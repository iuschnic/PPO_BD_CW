using Domain.Models;
namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface ISettingsRepo
{
    UserSettings? TryGet(string user_name);
    bool TryCreate(UserSettings user_settings);
    bool TryUpdate(UserSettings user_settings); 
    bool TryDelete(string user_name);
}
