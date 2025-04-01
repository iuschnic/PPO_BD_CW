namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface IUserRepo
{
    User? Get(Guid id);
    User? Get(string username);
    int Create(User user);
    void Update(User user);
    void Delete(Guid id);
    void Save();
}
