namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface IMessageRepo
{
    void Create(Message m, List<Guid> users);
    void Save();
}
