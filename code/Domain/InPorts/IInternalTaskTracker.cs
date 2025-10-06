using Domain.Models;

namespace Domain.InPorts;

public interface IMessageSenderProvider
{
    public Task<List<UserHabitInfo>> GetUsersToNotifyAsync();
    public Task<UserInfo> TryGetUserInfoAsync(string taskTrackerLogin);
}
