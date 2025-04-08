namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface ISettingsRepo
{
    UserSettings? TryGet(Guid user_id);
    bool TryCreate(UserSettings user_settings);
    void Update(UserSettings user_settings); 
    void Delete(Guid user_id);
    void Save();
}
