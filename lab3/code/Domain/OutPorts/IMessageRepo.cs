using Domain.Models;
namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface IMessageRepo
{
    void Create(Message m, List<string> users);
    void Save();
}
