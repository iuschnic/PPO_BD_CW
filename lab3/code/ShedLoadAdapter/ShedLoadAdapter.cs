using Domain;
using Domain.OutPorts;
using Types;

namespace LoadAdapters;

public class DummyShedAdapter: IShedLoad
{
    public List<Event> LoadShedule(Guid user_id)
    {
        List<Event> events = new();
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), new WeekDay("Monday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), new WeekDay("Monday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), new WeekDay("Monday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), new WeekDay("Monday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), new WeekDay("Tuesday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), new WeekDay("Tuesday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), new WeekDay("Tuesday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), new WeekDay("Tuesday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), new WeekDay("Wednesday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), new WeekDay("Thursday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), new WeekDay("Friday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), new WeekDay("Saturday"), user_id));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), new WeekDay("Sunday"), user_id)); 
        return events;
    }
}