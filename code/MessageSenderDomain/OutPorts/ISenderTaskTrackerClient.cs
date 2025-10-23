using MessageSenderDomain.Models;

namespace MessageSenderDomain.OutPorts;

public interface ISenderTaskTrackerClient
{
    public Task<List<UserHabitInfo>?> GetUsersToNotifyAsync();
    public Task<bool> TryLogInAsync(string taskTrackerLogin, string password);
}
