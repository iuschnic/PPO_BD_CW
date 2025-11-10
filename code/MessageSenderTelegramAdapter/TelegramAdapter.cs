using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MessageSenderDomain.OutPorts;

public class TelegramBotAdapterArgs(string botToken)
{
    public string BotToken = botToken;
}

public class TelegramBotAdapter : IBotClient
{
    private readonly ITelegramBotClient _botClient;

    public TelegramBotAdapter(TelegramBotAdapterArgs args)
    {
        _botClient = new TelegramBotClient(args.BotToken);
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        await _botClient.SendMessage(chatId, text);
    }

    public Task StartReceivingAsync(Func<IBotUpdate, Task> updateHandler, CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botClient.StartReceiving(
            updateHandler: (client, update, token) => HandleUpdateAsync(update, updateHandler),
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        );

        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(Update update, Func<IBotUpdate, Task> updateHandler)
    {
        if (update.Message is not { } message)
            return;

        var botUpdate = new TelegramBotUpdate(message);
        await updateHandler(botUpdate);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка Telegram Bot: {exception.Message}");
        return Task.CompletedTask;
    }
}

public class TelegramBotUpdate : IBotUpdate, IBotMessage
{
    private readonly Message _message;

    public long ChatId => _message.Chat.Id;
    public string Text => _message.Text?.Trim() ?? string.Empty;
    public string Username => _message.From?.Username ?? _message.From?.FirstName ?? "User";

    public TelegramBotUpdate(Message message)
    {
        _message = message;
    }
}