using MessageSenderDomain.Models;

namespace MessageSenderDomain.OutPorts;

public interface ITaskTrackerClient
{
    public Task<List<UserHabitInfo>> GetUsersToNotifyAsync();
    public Task<UserInfo> TryGetUserInfoAsync(string taskTrackerLogin);
}
