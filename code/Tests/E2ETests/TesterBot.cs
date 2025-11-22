using Telegram.Bot;
using Telegram.Bot.Types;

namespace Tests.E2ETests;

public class TesterBot
{
    private readonly TelegramBotClient _botClient;
    private readonly long _chatId;
    private readonly List<string?> _receivedMessages = new();
    private CancellationTokenSource _cts = new CancellationTokenSource();

    public TesterBot(string botToken, long chatId)
    {
        _botClient = new TelegramBotClient(botToken);
        _chatId = chatId;
    }

    public List<string?> GetMessages()
    {
        return _receivedMessages;
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

    public void ClearMessages()
    {
        _receivedMessages.Clear();
    }
}