using Domain.Models;
namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface IEventRepo
{
    //Здесь будет IEnumerable
    List<Event> Get(Guid user_id);
    void Create(Event e);
    void CreateMany(List<Event> events);
    void Update(Event e);
    void DeleteEvents(Guid user_id);
    void Save();
}
