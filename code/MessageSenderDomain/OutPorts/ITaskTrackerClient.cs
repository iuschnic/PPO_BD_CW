using MessageSenderDomain.Models;

namespace MessageSenderDomain.OutPorts;

public interface ITaskTrackerClient
{
    public Task<List<UserHabitInfo>?> GetUsersToNotifyAsync();
    public Task<bool> TryLogInAsync(string taskTrackerLogin, string password);
}
