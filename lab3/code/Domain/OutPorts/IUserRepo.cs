namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface IUserRepo
{
    User? TryGet(Guid id);
    User? TryGet(string username);
    bool TryCreate(User user);
    void Update(User user);
    void Delete(Guid id);
    void Save();
}
