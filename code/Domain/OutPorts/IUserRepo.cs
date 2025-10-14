using Domain.Models;
namespace Domain.OutPorts;
public interface IUserRepo
{
    Task<User?> TryGetAsync(string username);
    Task<User?> TryFullGetAsync(string username);
    Task<bool> TryCreateAsync(User user);
    Task<bool> TryUpdateUserAsync(User user);
    Task<bool> TryUpdateSettingsAsync(UserSettings user_settings);
    Task<bool> TryUpdateSettingsAsync(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string user_name);
    Task<bool> TryNotificationsOffAsync(string username);
    Task<bool> TryNotificationsOnAsync(string username);
    Task<bool> TryUpdateNotificationTimingsAsync(List<Tuple<TimeOnly, TimeOnly>> newTimings, string user_name);
    Task<bool> TryDeleteAsync(string username);
    Task<bool> TryCheckLogInAsync(string login, string password);

    User? TryGet(string username);
    User? TryFullGet(string username);
    bool TryCreate(User user);
    bool TryUpdateUser(User user);
    bool TryUpdateSettings(UserSettings user_settings);
    bool TryUpdateSettings(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string user_name);
    bool TryNotificationsOff(string username);
    bool TryNotificationsOn(string username);
    bool TryUpdateNotificationTimings(List<Tuple<TimeOnly, TimeOnly>> newTimings, string user_name);
    bool TryDelete(string username);
    bool TryCheckLogIn(string login, string password);
}
