using Domain;
using Domain.OutPorts;
using Storage;
using Types;

namespace Storage.StorageAdapters;

public class EventRepo : IEventRepo
{
    //Моделирует таблицу DBEvent
    private Dictionary<Guid, List<DBEvent>> UserEvents = new();

    public List<Event> Get(Guid user_id)
    {
        var dbevents =  UserEvents.GetValueOrDefault(user_id);
        if (dbevents == null)
            return [];
        List<Event> events = [];
        foreach(var dbe in dbevents)
        {
            events.Add(new Event(dbe.Id, dbe.Name, dbe.Start, dbe.End, new WeekDay(dbe.Day), dbe.DBUserID));
        }
        return events;
    }

    public void Create(Event e)
    {
        DBEvent dbe = new()
        {
            Id = e.Id,
            Name = e.Name,
            Start = e.Start,
            End = e.End,
            Day = e.Day.StringDay,
            DBUserID = e.UserID
        };
        if (!UserEvents.ContainsKey(dbe.DBUserID))
            UserEvents[dbe.DBUserID] = [];
        UserEvents[dbe.DBUserID].Add(dbe);
    }

    public void Update(Event e)
    {
        return;
    }

    public void DeleteEvents(Guid user_id)
    {
        UserEvents.Remove(user_id);
    }

    public void Save()
    {
        return;
    }
}
