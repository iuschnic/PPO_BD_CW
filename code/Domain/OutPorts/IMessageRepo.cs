using Domain.Models;
namespace Domain.OutPorts;

public interface IMessageRepo
{
    public bool TryCreateMessages(List<Message> users_messages);
    public List<Message> GetMessagesToSend();
    public bool MarkMessagesSent(List<Message> messages);
    public List<UserHabitInfo> GetUsersToNotify();
}