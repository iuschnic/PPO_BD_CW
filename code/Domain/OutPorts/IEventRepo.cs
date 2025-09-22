using Domain.Models;
namespace Domain.OutPorts;

public interface IEventRepo
{
    List<Event>? TryGet(string user_name);
    bool TryCreate(Event e);
    bool TryCreateMany(List<Event> events);
    bool TryUpdate(Event e);
    bool TryDelete(Guid event_id);
    bool TryDeleteEvents(string user_name);
    bool TryReplaceEvents(List<Event> events, string user_name);
    Task<List<Event>?> TryGetAsync(string user_name);
    Task<bool> TryCreateAsync(Event e);
    Task<bool> TryCreateManyAsync(List<Event> events);
    Task<bool> TryUpdateAsync(Event e);
    Task<bool> TryDeleteAsync(Guid event_id);
    Task<bool> TryDeleteEventsAsync(string user_name);
    Task<bool> TryReplaceEventsAsync(List<Event> events, string user_name);
}
