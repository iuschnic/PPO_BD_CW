namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface ISettingsRepo
{
    UserSettings? Get(Guid user_id);
    int Create(UserSettings user_settings);
    void Update(UserSettings user_settings); 
    void Delete(Guid user_id);
    void Save();
}
