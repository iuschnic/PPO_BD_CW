using Domain.Models;

namespace Domain.InPorts;

public interface IMessageSenderProvider
{
    public Task<List<UserHabitInfo>> GetUsersToNotifyAsync();
    public Task<bool> CheckLogInAsync(string login, string password);
}
