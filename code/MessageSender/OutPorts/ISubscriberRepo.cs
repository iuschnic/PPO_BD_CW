using MessageSenderDomain.Models;
namespace MessageSenderDomain.OutPorts;

public interface ISubscribersRepo
{
    Subscriber? TryGetByChatID(long chat_id);
    Subscriber? TryGetByTaskTrackerLogin(string task_tracker_login);
    bool IfAnyChatID(long chat_id);
    bool TryAdd(Subscriber subscriber);
    bool TryRemoveByChatID(long chat_id);
}
