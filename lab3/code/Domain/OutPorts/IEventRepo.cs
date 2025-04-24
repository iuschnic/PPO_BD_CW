using Domain.Models;
namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface IEventRepo
{
    //Здесь будет IEnumerable
    List<Event>? TryGet(string user_name);
    bool TryCreate(Event e);
    bool TryCreateMany(List<Event> events);
    bool TryUpdate(Event e);
    bool TryDelete(Guid event_id);
    bool TryDeleteEvents(string user_name);
}
