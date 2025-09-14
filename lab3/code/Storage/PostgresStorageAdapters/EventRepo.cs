using Domain.Models;
using Domain.OutPorts;
using Microsoft.EntityFrameworkCore;
using Storage.Models;

namespace Storage.PostgresStorageAdapters;

public class PostgresEventRepo : IEventRepo
{
    private ITaskTrackerContext _dbContext { get; }
    public PostgresEventRepo(ITaskTrackerContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<List<Event>?> TryGetAsync(string user_name)
    {
        if (await _dbContext.Users.FindAsync(user_name) == null)
            return null;

        int delta1 = DayOfWeek.Sunday - DateTime.Now.DayOfWeek;
        DateOnly fir_day_of_week = DateOnly.FromDateTime(DateTime.Now.AddDays(delta1));
        DateOnly last_day_of_week = DateOnly.FromDateTime(DateTime.Now.AddDays(6 + delta1));

        var dbevents = await _dbContext.Events.Where(
            ev => ev.DBUserNameID == user_name
            && (ev.Option == Types.EventOption.EveryWeek ||
              ((ev.Option == Types.EventOption.Once && ev.EDate != null)
                    && (fir_day_of_week <= ev.EDate) 
                    && (ev.EDate <= last_day_of_week)) ||
              ((ev.Option == Types.EventOption.EveryTwoWeeks)
                    && fir_day_of_week.AddDays((int) ev.Day).DayNumber - ((DateOnly)ev.EDate).DayNumber >= 0
                    && (((fir_day_of_week.AddDays((int)ev.Day).DayNumber - ((DateOnly)ev.EDate).DayNumber) / 7) % 2 == 0)))
            ).ToListAsync();

        if (dbevents == null)
            return [];
        List<Event> events = [];
        foreach (var dbe in dbevents)
            events.Add(dbe.ToModel());
        return events;
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
                    && (ev.EDate <= last_day_of_week)) ||
              ((ev.Option == Types.EventOption.EveryTwoWeeks)
                    && fir_day_of_week.AddDays((int)ev.Day).DayNumber - ((DateOnly)ev.EDate).DayNumber >= 0
                    && (((fir_day_of_week.AddDays((int)ev.Day).DayNumber - ((DateOnly)ev.EDate).DayNumber) / 7) % 2 == 0)))
            ).ToList();

        if (dbevents == null)
            return [];
        List<Event> events = [];
        foreach (var dbe in dbevents)
            events.Add(dbe.ToModel());
        return events;
    }
    public async Task<bool> TryCreateAsync(Event e)
    {
        var test = await _dbContext.Events.FindAsync(e.Id);
        if (test != null) 
            return false;
        var dbe = new DBEvent(e);
        _dbContext.Events.Add(dbe);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public bool TryCreate(Event e)
    {
        var test = _dbContext.Events.Find(e.Id);
        if (test != null)
            return false;
        DBEvent dbe = new DBEvent(e);
        _dbContext.Events.Add(dbe);
        _dbContext.SaveChanges();
        return true;
    }
    public async Task<bool> TryCreateManyAsync(List<Event> events)
    {
        List<DBEvent> dbevents = [];
        foreach (var e in events)
        {
            var test = await _dbContext.Events.FindAsync(e.Id);
            if (test != null)
                return false;
            DBEvent dbe = new DBEvent(e);
            dbevents.Add(dbe);
        }
        _dbContext.Events.AddRange(dbevents);
        await _dbContext.SaveChangesAsync();
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
            DBEvent dbe = new DBEvent(e);
            dbevents.Add(dbe);
        }
        _dbContext.Events.AddRange(dbevents);
        _dbContext.SaveChanges();
        return true;
    }
    public async Task<bool> TryUpdateAsync(Event e)
    {
        var dbe = await _dbContext.Events.FindAsync(e.Id);
        if (dbe == null)
            return false;
        dbe.Name = e.Name;
        dbe.Start = e.Start;
        dbe.End = e.End;
        dbe.Day = e.Day;
        dbe.Option = e.Option;
        dbe.EDate = e.EDate;
        await _dbContext.SaveChangesAsync();
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
    public async Task<bool> TryDeleteAsync(Guid event_id)
    {
        var dbe = await _dbContext.Events.FindAsync(event_id);
        if (dbe == null) return false;
        _dbContext.Events.Remove(dbe);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public bool TryDelete(Guid event_id)
    {
        var dbe = _dbContext.Events.Find(event_id);
        if (dbe == null) return false;
        _dbContext.Events.Remove(dbe);
        _dbContext.SaveChanges();
        return true;
    }
    public async Task<bool> TryDeleteEventsAsync(string user_name)
    {
        var dbu = await _dbContext.Users.FindAsync(user_name);
        if (dbu == null)
            return false;
        var events = await _dbContext.Events.Where(e => e.DBUserNameID == user_name).ToListAsync();
        _dbContext.Events.RemoveRange(events);
        await _dbContext.SaveChangesAsync();
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
    public async Task<bool> TryReplaceEventsAsync(List<Event> events, string user_name)
    {
        if (!events.TrueForAll(e => e.UserNameID == user_name))
            return false;
        if (!await TryDeleteEventsAsync(user_name)) return false;
        return await TryCreateManyAsync(events);
    }
    public bool TryReplaceEvents(List<Event> events, string user_name)
    {
        if (!events.TrueForAll(e => e.UserNameID == user_name))
            return false;
        if (!TryDeleteEvents(user_name)) return false;
        return TryCreateMany(events);
    }
}
