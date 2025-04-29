using Domain;
using Domain.OutPorts;
using Domain.Models;
using Storage.Models;
using Types;

namespace Storage.StorageAdapters;

public class DummyEventRepo : IEventRepo
{
    //Моделирует таблицу DBEvent
    private Dictionary<string, List<DBEvent>> UserEvents = new();


    public List<Event>? TryGet(string user_name)
    {
        var dbevents =  UserEvents.GetValueOrDefault(user_name);
        if (dbevents == null)
            return [];
        List<Event> events = [];
        DayOfWeek day;
        foreach(var dbe in dbevents)
        {
            /*if (dbe.Day == "Monday")
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
            events.Add(new Event(dbe.Id, dbe.Name, dbe.Start, dbe.End, day, dbe.DBUserNameID));*/
            events.Add(new Event(dbe.Id, dbe.Name, dbe.Start, dbe.End, dbe.Day, dbe.DBUserNameID));
        }
        return events;
    }

    public bool TryCreate(Event e)
    {
        //DBEvent dbe = new DBEvent(e.Id, e.Name, e.Start, e.End, e.Day.ToString(), e.UserNameID);
        DBEvent dbe = new DBEvent(e.Id, e.Name, e.Start, e.End, e.Day, e.UserNameID);
        if (!UserEvents.ContainsKey(dbe.DBUserNameID))
            UserEvents[dbe.DBUserNameID] = [];
        UserEvents[dbe.DBUserNameID].Add(dbe);
        return true;
    }

    public bool TryCreateMany(List<Event> events)
    {
        foreach(var e in events)
            TryCreate(e);
        return true;
    }

    public bool TryUpdate(Event e)
    {
        return true;
    }

    public bool TryDelete(Guid event_id)
    {
        return true;
    }

    public bool TryDeleteEvents(string user_name)
    {
        UserEvents.Remove(user_name);
        return true;
    }
}
