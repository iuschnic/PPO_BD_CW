using Domain;
using Domain.OutPorts;
using Domain.Models;
using Storage.Models;
using Types;

namespace Storage.StorageAdapters;

public class EventRepo : IEventRepo
{
    //Моделирует таблицу DBEvent
    private Dictionary<string, List<DBEvent>> UserEvents = new();

    public List<Event> Get(string user_name)
    {
        var dbevents =  UserEvents.GetValueOrDefault(user_name);
        if (dbevents == null)
            return [];
        List<Event> events = [];
        DayOfWeek day;
        foreach(var dbe in dbevents)
        {
            if (dbe.Day == "Monday")
                day = DayOfWeek.Monday;
            else if (dbe.Day == "Tuesday")
                day = DayOfWeek.Tuesday;
            else if (dbe.Day == "Wednesday")
                day = DayOfWeek.Wednesday;
            else if (dbe.Day == "Thursday")
                day = DayOfWeek.Thursday;
            else if (dbe.Day == "Friday")
                day = DayOfWeek.Friday;
            else if (dbe.Day == "Saturday")
                day = DayOfWeek.Saturday;
            else
                day = DayOfWeek.Sunday;
            events.Add(new Event(dbe.Id, dbe.Name, dbe.Start, dbe.End, day, dbe.DBUserNameID));
        }
        return events;
    }

    public void Create(Event e)
    {
        DBEvent dbe = new DBEvent(e.Id, e.Name, e.Start, e.End, e.Day, e.UserNameID);
        if (!UserEvents.ContainsKey(dbe.DBUserNameID))
            UserEvents[dbe.DBUserNameID] = [];
        UserEvents[dbe.DBUserNameID].Add(dbe);
    }

    public void CreateMany(List<Event> events)
    {
        foreach(var e in events)
            Create(e);
    }

    public void Update(Event e)
    {
        return;
    }

    public void DeleteEvents(string user_name)
    {
        UserEvents.Remove(user_name);
    }

    public void Save()
    {
        return;
    }
}
