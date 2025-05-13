using Domain.Models;
namespace Domain.OutPorts;
public interface IUserRepo
{
    User? TryGet(string username);
    User? TryFullGet(string username);
    bool TryCreate(User user);
    bool TryUpdateUser(User user);
    bool TryUpdateSettings(UserSettings user_settings);
    bool TryDelete(string username);
}