using Domain.Models;
namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface ISettingsRepo
{
    UserSettings? TryGet(string user_name);
    bool TryCreate(UserSettings user_settings);
    void Update(UserSettings user_settings); 
    void Delete(string user_name);
    void Save();
}
