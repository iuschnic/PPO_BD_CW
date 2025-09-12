using Domain.Models;
namespace Domain.OutPorts;
public interface IUserRepo
{
    Task<User?> TryGetAsync(string username);
    Task<User?> TryFullGetAsync(string username);
    Task<bool> TryCreateAsync(User user);
    Task<bool> TryUpdateUserAsync(User user);
    Task<bool> TryUpdateSettingsAsync(UserSettings user_settings);
    Task<bool> TryDeleteAsync(string username);
    User? TryGet(string username);
    User? TryFullGet(string username);
    bool TryCreate(User user);
    bool TryUpdateUser(User user);
    bool TryUpdateSettings(UserSettings user_settings);
    bool TryDelete(string username);
}
