using Domain.Models;
using Domain.OutPorts;
using Microsoft.EntityFrameworkCore;
using Storage.Models;
using System.Linq.Expressions;

namespace Storage.PostgresStorageAdapters;

public class PostgresEventRepo : IEventRepo
{
    private PostgresDBContext _dbContext { get; }
    public PostgresEventRepo(PostgresDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<Event>? TryGet(string user_name)
    {
        if (_dbContext.Users.Find(user_name) == null)
            return null;

        int delta1 = DayOfWeek.Sunday - DateTime.Now.DayOfWeek;
        DateOnly fir_day_of_week = DateOnly.FromDateTime(DateTime.Now.AddDays(delta1));
        DateOnly last_day_of_week = DateOnly.FromDateTime(DateTime.Now.AddDays(6 + delta1));

        var dbevents = _dbContext.Events.Where(
            ev => ev.DBUserNameID == user_name
            && (ev.Option == Types.EventOption.EveryWeek ||
              ((ev.Option == Types.EventOption.Once && ev.EDate != null)
                    && (fir_day_of_week <= ev.EDate) 
                    && (ev.EDate <= last_day_of_week))) ||
              ((ev.Option == Types.EventOption.EveryTwoWeeks)
                    && fir_day_of_week.AddDays((int) ev.Day).DayNumber - ((DateOnly)ev.EDate).DayNumber >= 0
                    && (((fir_day_of_week.AddDays((int)ev.Day).DayNumber - ((DateOnly)ev.EDate).DayNumber) / 7) % 2 == 0))
            ).ToList();

        if (dbevents == null)
            return [];
        List<Event> events = [];
        foreach (var dbe in dbevents)
            events.Add(new Event(dbe.Id, dbe.Name, dbe.Start, dbe.End, dbe.DBUserNameID, dbe.Option, dbe.Day, dbe.EDate));
        return events;
    }

    public bool TryCreate(Event e)
    {
        var test = _dbContext.Events.Find(e.Id);
        if (test != null) 
            return false;
        DBEvent dbe = new DBEvent(e.Id, e.Name, e.Start, e.End, e.UserNameID, e.Option, e.Day, e.EDate);
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
            DBEvent dbe = new DBEvent(e.Id, e.Name, e.Start, e.End, e.UserNameID, e.Option, e.Day, e.EDate);
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
        dbe.Day = e.Day;
        dbe.Option = e.Option;
        dbe.EDate = e.EDate;
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

    public bool TryReplaceEvents(List<Event> events, string user_name)
    {
        if (!events.TrueForAll(e => e.UserNameID == user_name))
            return false;
        if (!TryDeleteEvents(user_name)) return false;
        return TryCreateMany(events);
    }
}
