using Domain;
using Domain.OutPorts;
using Domain.Models;
using Types;

namespace LoadAdapters;

public class DummyShedAdapter: IShedLoad
{
    public List<Event> LoadShedule(Guid user_id, string path)
    {
        List<Event> events = new();
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Monday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), DayOfWeek.Monday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Monday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Monday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Tuesday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(9, 0, 0), DayOfWeek.Tuesday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Tuesday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Tuesday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Wednesday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Thursday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Saturday, user_id));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Sunday, user_id)); 
        return events;
    }
}