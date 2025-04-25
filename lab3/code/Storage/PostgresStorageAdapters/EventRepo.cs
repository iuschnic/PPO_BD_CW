using Domain.Models;
using Domain.OutPorts;
using Microsoft.EntityFrameworkCore;
using Storage.Models;

namespace Storage.PostgresStorageAdapters;

public class PostgresEventRepo : IEventRepo
{
    private PostgresDBContext _dbContext { get; }

    public List<Event>? TryGet(string user_name)
    {
        if (_dbContext.Users.Find(user_name) == null)
            return null;
        var dbevents = _dbContext.Events.Where(ev => ev.DBUserNameID == user_name).ToList();
        if (dbevents == null)
            return [];
        List<Event> events = [];
        DayOfWeek day;
        foreach (var dbe in dbevents)
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

    public bool TryCreate(Event e)
    {
        var test = _dbContext.Events.Find(e.Id);
        if (test != null) 
            return false;
        DBEvent dbe = new DBEvent(e.Id, e.Name, e.Start, e.End, e.Day.ToString(), e.UserNameID);
        _dbContext.Events.Add(dbe);
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryCreateMany(List<Event> events)
    {
        List<DBEvent> dbevents = [];
        foreach (var e in events)
        {
            var test = _dbContext.Events.Find(e.Id);
            if (test != null)
                return false;
            DBEvent dbe = new DBEvent(e.Id, e.Name, e.Start, e.End, e.Day.ToString(), e.UserNameID);
            dbevents.Add(dbe);
        }
        _dbContext.Events.AddRange(dbevents);
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryUpdate(Event e)
    {
        var dbe = _dbContext.Events.Find(e.Id);
        if (dbe == null)
            return false;
        dbe.Name = e.Name;
        dbe.Start = e.Start;
        dbe.End = e.End;
        dbe.Day = e.Day.ToString();
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryDelete(Guid event_id)
    {
        var dbe = _dbContext.Events.Find(event_id);
        if (dbe == null) return false;
        _dbContext.Remove(dbe);
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryDeleteEvents(string user_name)
    {
        var dbu = _dbContext.Users.Find(user_name);
        if (dbu == null)
            return false;
        var events = _dbContext.Events.Where(e => e.DBUserNameID == user_name).ToList();
        _dbContext.Events.RemoveRange(events);
        _dbContext.SaveChanges();
        return true;
    }

    public PostgresEventRepo(PostgresDBContext dbContext)
    {
        _dbContext = dbContext;
    }
}
