using Domain.Models;
using Domain.OutPorts;
using Microsoft.EntityFrameworkCore;
using Storage.Models;

namespace Storage.EfAdapters;

public class EfHabitRepo(ITaskTrackerContext dbContext) : IHabitRepo
{
    private ITaskTrackerContext _dbContext = dbContext;

    public async Task<List<Habit>?> TryGetAsync(string user_name)
    {
        var test = await _dbContext.Users.FindAsync(user_name);
        if (test == null)
            return null;
        var dbhabits = await _dbContext.Habits.Include(h => h.ActualTimings).Include(h => h.PrefFixedTimings)
            .Where(h => h.DBUserNameID == user_name).ToListAsync();
        if (dbhabits == null)
            return [];
        List<Habit> habits = [];
        foreach (var dbh in dbhabits)
            habits.Add(dbh.ToModel(dbh.PrefFixedTimings, dbh.ActualTimings));
        return habits;
    }
    public List<Habit>? TryGet(string user_name)
    {
        var test = _dbContext.Users.Find(user_name);
        if (test == null)
            return null;
        var dbhabits = _dbContext.Habits.Include(h => h.ActualTimings).Include(h => h.PrefFixedTimings)
            .Where(h => h.DBUserNameID == user_name).ToList();
        if (dbhabits == null)
            return [];
        List<Habit> habits = [];
        foreach (var dbh in dbhabits)
            habits.Add(dbh.ToModel(dbh.PrefFixedTimings, dbh.ActualTimings));
        return habits;
    }
    public async Task<bool> TryCreateAsync(Habit h)
    {
        if (await _dbContext.Habits.FindAsync(h.Id) != null)
            return false;
        List<DBActualTime> actualTimes = new();
        List<DBPrefFixedTime> prefFixedTimes = new();
        foreach (var at in h.ActualTimings)
        {
            DBActualTime dbat = new DBActualTime(at);
            actualTimes.Add(dbat);
        }
        foreach (var pf in h.PrefFixedTimings)
        {
            DBPrefFixedTime dbpf = new DBPrefFixedTime(pf);
            prefFixedTimes.Add(dbpf);
        }
        DBHabit dbh = new DBHabit(h);
        _dbContext.Habits.Add(dbh);
        _dbContext.ActualTimes.AddRange(actualTimes);
        _dbContext.PrefFixedTimes.AddRange(prefFixedTimes);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public bool TryCreate(Habit h)
    {
        if (_dbContext.Habits.Find(h.Id) != null) 
            return false;
        List<DBActualTime> actualTimes = new();
        List<DBPrefFixedTime> prefFixedTimes = new();
        foreach (var at in h.ActualTimings)
        {
            DBActualTime dbat = new DBActualTime(at);
            actualTimes.Add(dbat);
        }
        foreach (var pf in h.PrefFixedTimings)
        {
            DBPrefFixedTime dbpf = new DBPrefFixedTime(pf);
            prefFixedTimes.Add(dbpf);
        }
        DBHabit dbh = new DBHabit(h);
        _dbContext.Habits.Add(dbh);
        _dbContext.ActualTimes.AddRange(actualTimes);
        _dbContext.PrefFixedTimes.AddRange(prefFixedTimes);
        _dbContext.SaveChanges();
        return true;
    }
    public async Task<bool> TryCreateManyAsync(List<Habit> habits)
    {
        List<DBActualTime> actualTimes = [];
        List<DBPrefFixedTime> prefFixedTimes = [];
        List<DBHabit> dbhabits = [];
        foreach (var h in habits)
        {
            var test = await _dbContext.Habits.FindAsync(h.Id);
            if (test != null)
                return false;
            foreach (var at in h.ActualTimings)
            {
                DBActualTime dbat = new DBActualTime(at);
                actualTimes.Add(dbat);
            }
            foreach (var pf in h.PrefFixedTimings)
            {
                DBPrefFixedTime dbpf = new DBPrefFixedTime(pf);
                prefFixedTimes.Add(dbpf);
            }
            DBHabit dbh = new DBHabit(h);
            dbhabits.Add(dbh);
        }
        _dbContext.Habits.AddRange(dbhabits);
        _dbContext.ActualTimes.AddRange(actualTimes);
        _dbContext.PrefFixedTimes.AddRange(prefFixedTimes);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public bool TryCreateMany(List<Habit> habits)
    {
        List<DBActualTime> actualTimes = [];
        List<DBPrefFixedTime> prefFixedTimes = [];
        List<DBHabit> dbhabits = [];
        foreach (var h in habits)
        {
            var test = _dbContext.Habits.Find(h.Id);
            if (test != null)
                return false;
            foreach (var at in h.ActualTimings)
            {
                DBActualTime dbat = new DBActualTime(at);
                actualTimes.Add(dbat);
            }
            foreach (var pf in h.PrefFixedTimings)
            {
                DBPrefFixedTime dbpf = new DBPrefFixedTime(pf);
                prefFixedTimes.Add(dbpf);
            }
            DBHabit dbh = new DBHabit(h);
            dbhabits.Add(dbh);
        }
        _dbContext.Habits.AddRange(dbhabits);
        _dbContext.ActualTimes.AddRange(actualTimes);
        _dbContext.PrefFixedTimes.AddRange(prefFixedTimes);
        _dbContext.SaveChanges();
        return true;
    }
    public async Task<bool> TryUpdateAsync(Habit h)
    {
        var habit = await _dbContext.Habits.Include(h => h.ActualTimings).Include(h => h.PrefFixedTimings)
            .FirstOrDefaultAsync(h => h.Id == h.Id);
        if (habit == null)
            return false;
        return true;
    }
    public bool TryUpdate(Habit h)
    {
        var habit = _dbContext.Habits.Include(h => h.ActualTimings).Include(h => h.PrefFixedTimings)
            .FirstOrDefault(h => h.Id == h.Id);
        if (habit == null)
            return false;
        return true;
    }
    public async Task<bool> TryDeleteAsync(Guid habit_id)
    {
        var habit = await _dbContext.Habits.Include(h => h.ActualTimings).Include(h => h.PrefFixedTimings)
            .FirstOrDefaultAsync(h => h.Id == habit_id);
        if (habit == null)
            return false;
        _dbContext.ActualTimes.RemoveRange(habit.ActualTimings);
        _dbContext.PrefFixedTimes.RemoveRange(habit.PrefFixedTimings);
        _dbContext.Habits.Remove(habit);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public bool TryDelete(Guid habit_id)
    {
        var habit = _dbContext.Habits.Include(h => h.ActualTimings).Include(h => h.PrefFixedTimings)
            .FirstOrDefault(h => h.Id == habit_id);
        if (habit == null)
            return false;
        _dbContext.ActualTimes.RemoveRange(habit.ActualTimings);
        _dbContext.PrefFixedTimes.RemoveRange(habit.PrefFixedTimings);
        _dbContext.Habits.Remove(habit);
        _dbContext.SaveChanges();
        return true;
    }
    public async Task<bool> TryDeleteHabitsAsync(string user_name)
    {
        var dbu = await _dbContext.Users.FindAsync(user_name);
        if (dbu == null) return false;
        List<DBActualTime> actual_times = [];
        List<DBPrefFixedTime> pref_fixed_times = [];
        var habits = await _dbContext.Habits.Include(h => h.ActualTimings).Include(h => h.PrefFixedTimings)
            .Where(h => h.DBUserNameID == user_name).ToListAsync();
        if (habits == null) return false;
        foreach (var habit in habits)
        {
            actual_times.AddRange(habit.ActualTimings);
            pref_fixed_times.AddRange(habit.PrefFixedTimings);
        }
        _dbContext.PrefFixedTimes.RemoveRange(pref_fixed_times);
        _dbContext.ActualTimes.RemoveRange(actual_times);
        _dbContext.Habits.RemoveRange(habits);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public bool TryDeleteHabits(string user_name)
    {
        var dbu = _dbContext.Users.Find(user_name);
        if (dbu == null) return false;
        List<DBActualTime> actual_times = [];
        List<DBPrefFixedTime> pref_fixed_times = [];
        var habits = _dbContext.Habits.Include(h => h.ActualTimings).Include(h => h.PrefFixedTimings)
            .Where(h => h.DBUserNameID == user_name).ToList();
        if (habits == null) return false;
        foreach (var habit in habits)
        {
            actual_times.AddRange(habit.ActualTimings);
            pref_fixed_times.AddRange(habit.PrefFixedTimings);
        }
        _dbContext.PrefFixedTimes.RemoveRange(pref_fixed_times);
        _dbContext.ActualTimes.RemoveRange(actual_times);
        _dbContext.Habits.RemoveRange(habits);
        _dbContext.SaveChanges();
        return true;
    }
    public async Task<bool> TryReplaceHabitsAsync(List<Habit> habits, string user_name)
    {
        if (!habits.TrueForAll(h => h.UserNameID == user_name))
            return false;
        if (!await TryDeleteHabitsAsync(user_name)) return false;
        return await TryCreateManyAsync(habits);
    }
    public bool TryReplaceHabits(List<Habit> habits, string user_name)
    {
        if (!habits.TrueForAll(h => h.UserNameID == user_name))
            return false;
        if (!TryDeleteHabits(user_name)) return false;
        return TryCreateMany(habits);
    }
    public async Task<List<UserHabitInfo>?> GetUsersToNotifyAsync()
    {
        var currentTime = TimeOnly.FromDateTime(DateTime.Now);
        var currentDayOfWeek = DateTime.Now.DayOfWeek;
        var timePlus30Min = currentTime.AddMinutes(30);
        var crossesMidnight = timePlus30Min < currentTime;
        var usersHabits = await _dbContext.ActualTimes
            .Include(at => at.DBHabit)
            .ThenInclude(h => h.DBUser)
            .Include(at => at.DBHabit)
            .ThenInclude(h => h.DBUser.Settings)
            .ThenInclude(s => s.ForbiddenTimings)
            .Where(at =>
                (at.Day == currentDayOfWeek ||
                 (crossesMidnight && at.Day == GetTomorrowDayOfWeek(currentDayOfWeek))) &&
                IsTimeInRange(at.Start, currentTime, timePlus30Min, crossesMidnight) &&
                !IsInForbiddenTime(at.DBHabit.DBUser, currentTime))
            .Select(at => new UserHabitInfo(at.DBHabit.DBUser.NameID, at.DBHabit.Name, at.Start, at.End))
            .ToListAsync();
        return usersHabits;
    }
    public List<UserHabitInfo>? GetUsersToNotify()
    {
        var currentTime = TimeOnly.FromDateTime(DateTime.Now);
        var currentDayOfWeek = DateTime.Now.DayOfWeek;
        var timePlus30Min = currentTime.AddMinutes(30);
        var crossesMidnight = timePlus30Min < currentTime;
        var usersHabits = _dbContext.ActualTimes
            .Include(at => at.DBHabit)
            .ThenInclude(h => h.DBUser)
            .Include(at => at.DBHabit)
            .ThenInclude(h => h.DBUser.Settings)
            .ThenInclude(s => s.ForbiddenTimings)
            .Where(at =>
                (at.Day == currentDayOfWeek ||
                 (crossesMidnight && at.Day == GetTomorrowDayOfWeek(currentDayOfWeek))) &&
                IsTimeInRange(at.Start, currentTime, timePlus30Min, crossesMidnight) &&
                !IsInForbiddenTime(at.DBHabit.DBUser, currentTime))
            .Select(at => new UserHabitInfo(at.DBHabit.DBUser.NameID, at.DBHabit.Name, at.Start, at.End))
            .ToList();
        return usersHabits;
    }

    private bool IsTimeInRange(TimeOnly time, TimeOnly currentTime, TimeOnly timePlus30Min, bool crossesMidnight)
    {
        if (!crossesMidnight)
            return time >= currentTime && time <= timePlus30Min;
        else
            return time >= currentTime || time <= timePlus30Min;
    }

    private bool IsInForbiddenTime(DBUser user, TimeOnly currentTime)
    {
        return user.Settings?.ForbiddenTimings?
            .Any(ft => currentTime >= ft.Start && currentTime <= ft.End) ?? false;
    }
    private DayOfWeek GetTomorrowDayOfWeek(DayOfWeek today)
    {
        return today == DayOfWeek.Saturday ? DayOfWeek.Sunday : today + 1;
    }

}
