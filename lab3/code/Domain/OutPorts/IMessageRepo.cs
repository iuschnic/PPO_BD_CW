using Domain.Models;
namespace Domain.OutPorts;

public interface IMessageRepo
{
    bool TryCreateMessage(Message message, List<string> users);
    public bool TryNotify(List<Tuple<string, string>> users_messages);
    List<Tuple<string, string>> GetUsersToNotify();
}
