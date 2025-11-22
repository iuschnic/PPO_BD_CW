using MessageSenderDomain.OutPorts;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class TelegramBotAdapterArgs(string botToken, long overridingChatId = 0)
{
    public string BotToken = botToken;
    public long OverridingChatId = overridingChatId;
}

/*
 Да простит меня Бог за этот костыль с _overridingChatId, он нужен для e2e тестирования бота с помощью другого обычного бота.
 Так как боты не могут напрямую читать сообщения друг друга (ограничения Telegram), создается канал с двумя ботами в нем, а также 
 привязывается чат к этому каналу, куда также добавляются боты. Боты пишут в канал по _overridingChatId, сообщения пересылаются в чат,
 откуда боты его читают и таким образом они общаются логически также как пользователь и бот, что позволяет провести тестирование.
*/
public class TelegramBotAdapter : IBotClient
{
    private readonly ITelegramBotClient _botClient;
    private readonly long _overridingChatId;

    public TelegramBotAdapter(TelegramBotAdapterArgs args)
    {
        _botClient = new TelegramBotClient(args.BotToken);
        _overridingChatId = args.OverridingChatId;
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        if (_overridingChatId != 0)
            await _botClient.SendMessage(_overridingChatId, text);
        else
            await _botClient.SendMessage(chatId, text);
    }

    public async Task StartReceivingAsync(Func<IBotUpdate, Task> updateHandler, CancellationToken cancellationToken)
    {
        await _botClient.DropPendingUpdates();
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

public class TelegramBotUpdate : IBotUpdate
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