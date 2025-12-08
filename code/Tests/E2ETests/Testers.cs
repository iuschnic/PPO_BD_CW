using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using MessageSenderBotAdapters;

namespace Tests.E2ETests;

public class TesterTelegram
{
    private readonly TelegramBotClient _botClient;
    private readonly long _chatId;
    private readonly List<string?> _receivedMessages = new();
    private CancellationTokenSource _cts = new CancellationTokenSource();

    public TesterTelegram(string botToken, long chatId)
    {
        _botClient = new TelegramBotClient(botToken);
        _chatId = chatId;
    }

    public List<string?> GetMessages()
    {
        return _receivedMessages;
    }

    public string GetLastMessage()
    {
        return _receivedMessages.Last();
    }

    public async Task StartListeningAsync()
    {
        await _botClient.DropPendingUpdates();
        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            cancellationToken: _cts.Token
        );
        Console.WriteLine("TesterBot started listening...");
    }

    public async Task StopListeningAsync()
    {
        _cts?.Cancel();
        await Task.Delay(1000);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message != null)
        {
            _receivedMessages.Add(update.Message.Text);
            Console.WriteLine($"TesterBot recieved: {update.Message.Text}");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error TesterBot: {exception.Message}");
        return Task.CompletedTask;
    }

    public async Task SendAndWaitResponseAsync(string message)
    {
        var cnt = _receivedMessages.Count;
        await _botClient.SendMessage(_chatId, message);
        Console.WriteLine($"TesterBot sent: {message}");
        for (int i = 0; i < 100; i++)
        {
            if (_receivedMessages.Count > cnt)
                return;
            await Task.Delay(100);
        }
        Console.WriteLine("Timeout waiting for response");
    }

    public async Task WaitResponseAsync()
    {
        var cnt = _receivedMessages.Count;
        for (int i = 0; i < 100; i++)
        {
            if (_receivedMessages.Count > cnt)
                return;
            await Task.Delay(100);
        }
        Console.WriteLine("Timeout waiting for response");
    }

    public void ClearMessages()
    {
        _receivedMessages.Clear();
    }
}

public class TesterMock
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly long _chatId;
    private readonly string _userName;
    private readonly List<string?> _receivedMessages = new();
    private CancellationTokenSource _cts = new CancellationTokenSource();

    public TesterMock(string baseUrl, long chatId, string userName)
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl.TrimEnd('/') + "/";
        _chatId = chatId;
        _userName = userName;
    }

    public List<string?> GetMessages()
    {
        return _receivedMessages;
    }

    public async Task StartListeningAsync()
    {
        Console.WriteLine("TesterMock started listening...");
        await Task.CompletedTask;
    }

    public async Task StopListeningAsync()
    {
        _cts?.Cancel();
        await Task.Delay(1000);
    }

    public void ClearMessages()
    {
        _receivedMessages.Clear();
    }

    public async Task SendAndWaitResponseAsync(string message)
    {
        try
        {
            var request = new SendMessageRequest
            {
                ChatId = _chatId,
                Text = message,
                UserName = _userName
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"Sending message: {message}");

            var response = await _httpClient.PostAsync($"{_baseUrl}send", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SendMessageResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (result != null)
                {
                    if (!string.IsNullOrEmpty(result.BotResponse))
                    {
                        Console.WriteLine($"Bot response: {result.BotResponse}");
                        _receivedMessages.Add(result.BotResponse);
                    }
                    else
                    {
                        Console.WriteLine($"Status: {result.Status}");
                    }
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception while sending message: {ex.Message}");
            throw;
        }
    }

    public async Task CheckStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}status");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status: {content}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking status: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _httpClient?.Dispose();
    }
}

public class SendMessageResponse
{
    public string Status { get; set; } = string.Empty;
    public string? BotResponse { get; set; }
    public string? Note { get; set; }
    public string? Error { get; set; }
}