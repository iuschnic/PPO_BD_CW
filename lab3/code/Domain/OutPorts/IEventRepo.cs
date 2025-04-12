using Domain.Models;
namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface IEventRepo
{
    //Здесь будет IEnumerable
    List<Event> Get(string user_name);
    void Create(Event e);
    void CreateMany(List<Event> events);
    void Update(Event e);
    void DeleteEvents(string user_name);
    void Save();
}
