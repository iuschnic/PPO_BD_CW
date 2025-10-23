using MessageSenderDomain.Models;
namespace MessageSenderDomain.OutPorts;

public interface IMessageRepo
{
    public bool TryCreateMessages(List<Message> users_messages);
    public List<Message>? TryGetMessagesToSend();
    public bool MarkMessagesSent(List<Message> messages);
}