using System.Collections.Concurrent;
using System.Net;
using MessageSenderDomain.OutPorts;

namespace MessageSenderBotAdapters;

public class MockWebBotAdapterArgs(string baseUrl)
{
    public string BaseUrl = baseUrl;
}

public class MockWebBotAdapter : IBotClient, IDisposable
{
    private HttpListener? _httpListener;
    private string _baseUrl;
    private readonly ConcurrentDictionary<long, HttpResponse> _pendingResponses = new();

    public MockWebBotAdapter(MockWebBotAdapterArgs args)
    {
        _baseUrl = args.BaseUrl;
    }

    public async Task StartReceivingAsync(Func<IBotUpdate, Task> updateHandler, CancellationToken cancellationToken)
    {
        await StartHttpServer(updateHandler, cancellationToken);
    }

    private async Task StartHttpServer(Func<IBotUpdate, Task> updateHandler, CancellationToken cancellationToken)
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(_baseUrl);

        try
        {
            _httpListener.Start();
            Console.WriteLine($"HTTP сервер запущен на {_baseUrl}");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => HandleHttpRequest(context, updateHandler));
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                    // Корректное завершение при отмене
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Сервер был disposed
                    break;
                }
                catch (Exception ex)
                {
                    break;
                }
            }
        }
        finally
        {
            StopHttpServer();
        }
    }

    private void StopHttpServer()
    {
        try
        {
            _httpListener?.Stop();
            _httpListener?.Close();
            Console.WriteLine("HTTP сервер остановлен");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при остановке HTTP сервера: {ex.Message}");
        }
    }

    public void Dispose()
    {
        StopHttpServer();
        _httpListener = null;
    }

    private async Task HandleHttpRequest(HttpListenerContext context, Func<IBotUpdate, Task> updateHandler)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url.AbsolutePath == "/send" && request.HttpMethod == "POST")
            {
                await HandleSendMessage(request, response, updateHandler);
            }
            else if (request.Url.AbsolutePath == "/status")
            {
                await WriteJsonResponse(response, new
                {
                    status = "Bot running",
                    time = DateTime.Now,
                    pendingResponses = _pendingResponses.Count
                });
            }
            else
            {
                response.StatusCode = 404;
                await WriteJsonResponse(response, new { error = "Not found" });
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await WriteJsonResponse(response, new { error = ex.Message });
        }
        finally
        {
            response.Close();
        }
    }

    private async Task HandleSendMessage(HttpListenerRequest request, HttpListenerResponse response, Func<IBotUpdate, Task> updateHandler)
    {
        using var reader = new StreamReader(request.InputStream);
        var json = await reader.ReadToEndAsync();
        var sendRequest = System.Text.Json.JsonSerializer.Deserialize<SendMessageRequest>(json);

        if (sendRequest != null)
        {
            var responseContext = new HttpResponse(response);
            _pendingResponses[sendRequest.ChatId] = responseContext;

            try
            {
                await updateHandler(new MockBotUpdate(sendRequest.ChatId, sendRequest.Text, sendRequest.UserName));

                var botResponse = await responseContext.WaitForResponseAsync(TimeSpan.FromSeconds(30));

                if (botResponse != null)
                {
                    await WriteJsonResponse(response, new
                    {
                        status = "Message processed",
                        botResponse = botResponse
                    });
                }
                else
                {
                    await WriteJsonResponse(response, new
                    {
                        status = "Message received but no response from bot",
                        note = "Bot processed message but didn't send response"
                    });
                }
            }
            finally
            {
                _pendingResponses.TryRemove(sendRequest.ChatId, out _);
            }
        }
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        if (_pendingResponses.TryGetValue(chatId, out var responseContext))
        {
            await responseContext.SetResponse(text);
        }
        else
        {
            Console.WriteLine($"Bot response for user {chatId}: {text}");
        }
    }

    private async Task WriteJsonResponse(HttpListenerResponse response, object data)
    {
        if (!response.OutputStream.CanWrite)
            return;

        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var buffer = System.Text.Encoding.UTF8.GetBytes(json);

        response.ContentType = "application/json";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }
}

public class HttpResponse
{
    private readonly HttpListenerResponse _response;
    private readonly TaskCompletionSource<string?> _responseTcs = new();

    public HttpResponse(HttpListenerResponse response)
    {
        _response = response;
    }

    public Task<string?> WaitForResponseAsync(TimeSpan timeout)
    {
        return _responseTcs.Task.WaitAsync(timeout);
    }

    public Task SetResponse(string responseText)
    {
        _responseTcs.TrySetResult(responseText);
        return Task.CompletedTask;
    }
}

public class SendMessageRequest
{
    public long ChatId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

public class MockBotUpdate : IBotUpdate
{
    public long ChatId { get; }
    public string Text { get; }
    public string Username { get; }

    public MockBotUpdate(long chatId, string text, string userName)
    {
        ChatId = chatId;
        Text = text;
        Username = userName;
    }
}