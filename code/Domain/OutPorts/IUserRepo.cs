using Domain.Models;
namespace Domain.OutPorts;
public interface IUserRepo
{
    Task<User?> TryGetAsync(string username);
    Task<User?> TryFullGetAsync(string username);
    Task<bool> TryCreateAsync(User user);
    Task<bool> TryUpdateSettingsAsync(UserSettings user_settings);
    Task<bool> TryUpdateSettingsAsync(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string user_name);
    Task<bool> TryDeleteAsync(string username);
    Task<bool> TryCheckLogInAsync(string login, string password);
    Task<bool> TryChangeTwoFactorAsync(string username, bool state);
    Task<bool> TryCheckPasswordAttemptAsync(string username, int maxAttempts, int secondsBlocked);
    Task<bool> TryResetPasswordAttemptsAsync(string username);
    Task<bool> TryUpdateTwoFactorAsync(string username, string newTwoFactorCode, int twoFactorValidSeconds);
    Task<bool> TryChangePasswordAsync(string username, string new_password);

    User? TryGet(string username);
    User? TryFullGet(string username);
    bool TryCreate(User user);
    bool TryUpdateSettings(UserSettings user_settings);
    bool TryUpdateSettings(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string user_name);
    bool TryDelete(string username);
    bool TryCheckLogIn(string login, string password);
}
