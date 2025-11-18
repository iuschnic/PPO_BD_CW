using MessageSenderDomain.OutPorts;
using System.Net;

namespace MessageSenderTelegramAdapter;

public class MockWebBotAdapterArgs(string baseUrl)
{
    public string BaseUrl = baseUrl;
}

public class MockWebApiBotAdapter : IBotClient
{
    private readonly string _baseUrl;
    private Func<IBotUpdate, Task>? _updateHandler;

    public MockWebApiBotAdapter(MockWebBotAdapterArgs args)
    {
        _baseUrl = args.BaseUrl;
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        // Логируем в консоль и можно отправлять куда-то еще
        Console.WriteLine($"[API] 🤖 → User{chatId}: {text}");

        // Можно добавить запрос к реальному API для тестирования
        using var client = new HttpClient();
        var payload = new { ChatId = chatId, Message = text, Timestamp = DateTime.Now };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);

        // await client.PostAsync($"{_baseUrl}/api/messages", 
        //     new StringContent(json, Encoding.UTF8, "application/json"));

        await Task.CompletedTask;
    }

    public Task StartReceivingAsync(Func<IBotUpdate, Task> updateHandler, CancellationToken cancellationToken)
    {
        _updateHandler = updateHandler;
        _ = Task.Run(async () => await StartMockServer(cancellationToken), cancellationToken);

        return Task.CompletedTask;
    }

    private async Task StartMockServer(CancellationToken cancellationToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"{_baseUrl}/");
        listener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            var context = await listener.GetContextAsync();
            _ = Task.Run(() => ProcessRequest(context));
        }
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        if (_updateHandler == null) return;
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/api/messages")
            {
                using var reader = new StreamReader(request.InputStream);
                var body = await reader.ReadToEndAsync();

                var messageData = System.Text.Json.JsonSerializer.Deserialize<MessageData>(body);
                if (messageData != null)
                {
                    var update = new MockBotUpdate(messageData.ChatId, messageData.Text, messageData.Username);
                    await _updateHandler(update);
                }

                response.StatusCode = 200;
            }
            else
            {
                response.StatusCode = 404;
            }
        }
        finally
        {
            response.Close();
        }
    }

    private class MessageData
    {
        public long ChatId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}

public class MockBotUpdate : IBotUpdate
{
    public long ChatId { get; }
    public string Text { get; }
    public string Username { get; }

    public MockBotUpdate(long chatId, string text, string username)
    {
        ChatId = chatId;
        Text = text;
        Username = username;
    }
}
