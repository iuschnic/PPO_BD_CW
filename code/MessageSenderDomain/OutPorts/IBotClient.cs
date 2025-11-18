namespace MessageSenderDomain.OutPorts;
public interface IBotClient
{
    Task SendMessageAsync(long chatId, string text);
    Task StartReceivingAsync(Func<IBotUpdate, Task> updateHandler, CancellationToken cancellationToken);
}

public interface IBotUpdate
{
    long ChatId { get; }
    string Text { get; }
    string Username { get; }
}

public interface IBotMessage
{
    long ChatId { get; }
    string Text { get; }
    string Username { get; }
}