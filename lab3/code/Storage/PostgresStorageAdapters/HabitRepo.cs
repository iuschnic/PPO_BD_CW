using Dapper;
using Domain.Models;
using Domain.OutPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Storage.Models;
using Types;

namespace Storage.PostgresStorageAdapters;

public class PostgresHabitRepo : IHabitRepo
{
    private PostgresDBContext _dbContext;
    public PostgresHabitRepo(PostgresDBContext dbContext)
    {
        _dbContext = dbContext;
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
        List<Habit> habits = new();
        foreach (var dbh in dbhabits)
        {
            List<ActualTime> actualTimes = new();
            List<PrefFixedTime> prefFixedTimes = new();
            foreach (var at in dbh.ActualTimings)
                actualTimes.Add(new ActualTime(at.Id, at.Start, at.End, at.Day, at.DBHabitID));
            foreach (var pf in dbh.PrefFixedTimings)
                prefFixedTimes.Add(new PrefFixedTime(pf.Id, pf.Start, pf.End, pf.DBHabitID));
            habits.Add(new Habit(dbh.Id, dbh.Name, dbh.MinsToComplete, dbh.Option, dbh.DBUserNameID,
                actualTimes, prefFixedTimes, dbh.NDays));
        }
        return habits;
    }
    /*public List<Habit>? TryRedistributeNMTimeHabitsDB(string user_name)
    {
        List<Habit> no_distributed = [];
        var conn = _dbContext.Database.GetDbConnection();
        var result = conn.Query("SELECT * FROM distribute_with_no_matter_time(@u_name)",
                                      new { u_name = user_name });
        _dbContext.SaveChanges();
        if (result == null)
            return null;
        foreach (var r in result)
        {
            Guid g = new Guid(r.habit_id.ToString());
            string name = r.habit_name.ToString();
            int mins_to_complete = Int32.Parse(r.mins_to_complete.ToString());
            TimeOption opt = (TimeOption)Int32.Parse(r.option.toString());
            string u_name = r.user_name.ToString();
            int ndays = Int32.Parse(r.ndays.toString());
            no_distributed.Add(new Habit(g, name, mins_to_complete, opt, u_name, [], [], ndays));
        }
        return no_distributed;
    }*/
    public List<Habit>? TryRedistributeNMTimeHabitsDB(string user_name)
    {
        var conn = _dbContext.Database.GetDbConnection();

        // Вызываем хранимую процедуру (она сама делает INSERT/DELETE)
        var result = conn.Query(
            "SELECT * FROM distribute_with_no_matter_time(@u_name)",
            new { u_name = user_name }
        ).ToList();

        // Отключаем отслеживание, чтобы EF Core не пытался сохранить изменения
        _dbContext.ChangeTracker.Clear(); // Важно!

        if (!result.Any())
            return null;

        var no_distributed = new List<Habit>();
        foreach (var r in result)
        {
            Guid g = new Guid(r.habit_id.ToString());
            string name = r.habit_name.ToString();
            int mins_to_complete = Int32.Parse(r.mins_to_complete.ToString());
            TimeOption opt = (TimeOption)Int32.Parse(r.option.toString());
            string u_name = r.user_name.ToString();
            int ndays = Int32.Parse(r.ndays.toString());
            no_distributed.Add(new Habit(g, name, mins_to_complete, opt, u_name, [], [], ndays));
        }
        return no_distributed;
    }

    public bool TryCreate(Habit h)
    {
        var test = _dbContext.Habits.Find(h.Id);
        if (test != null) 
            return false;
        List<DBActualTime> actualTimes = new();
        List<DBPrefFixedTime> prefFixedTimes = new();
        foreach (var at in h.ActualTimings)
        {
            DBActualTime dbat = new DBActualTime(at.Id, at.Start, at.End, at.Day, at.HabitID);
            actualTimes.Add(dbat);
        }
        foreach (var pf in h.PrefFixedTimings)
        {
            DBPrefFixedTime dbpf = new DBPrefFixedTime(pf.Id, pf.Start, pf.End, pf.HabitID);
            prefFixedTimes.Add(dbpf);
        }
        DBHabit dbh = new DBHabit(h.Id, h.Name, h.MinsToComplete, h.Option, h.UserNameID, h.CountInWeek);
        _dbContext.Habits.Add(dbh);
        _dbContext.ActualTimes.AddRange(actualTimes);
        _dbContext.PrefFixedTimes.AddRange(prefFixedTimes);
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryCreateMany(List<Habit> habits)
    {
        List<DBActualTime> actualTimes = new();
        List<DBPrefFixedTime> prefFixedTimes = new();
        List<DBHabit> dbhabits = new();
        foreach (var h in habits)
        {
            var test = _dbContext.Habits.Find(h.Id);
            if (test != null)
                return false;
            foreach (var at in h.ActualTimings)
            {
                DBActualTime dbat = new DBActualTime(at.Id, at.Start, at.End, at.Day, at.HabitID);
                actualTimes.Add(dbat);
            }
            foreach (var pf in h.PrefFixedTimings)
            {
                DBPrefFixedTime dbpf = new DBPrefFixedTime(pf.Id, pf.Start, pf.End, pf.HabitID);
                prefFixedTimes.Add(dbpf);
            }
            DBHabit dbh = new DBHabit(h.Id, h.Name, h.MinsToComplete, h.Option, h.UserNameID, h.CountInWeek);
            dbhabits.Add(dbh);
        }
        _dbContext.Habits.AddRange(dbhabits);
        _dbContext.ActualTimes.AddRange(actualTimes);
        _dbContext.PrefFixedTimes.AddRange(prefFixedTimes);
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryUpdate(Habit h)
    {
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

    //Вроде ОК но написать тесты
    public bool TryDeleteHabits(string user_name)
    {
        var dbu = _dbContext.Users.Find(user_name);
        if (dbu == null) return false;
        List<DBActualTime> actual_times = [];
        List<DBPrefFixedTime> pref_fixed_times = [];
        var habits = _dbContext.Habits.Include(h => h.ActualTimings).Include(h => h.PrefFixedTimings)
            .Where(h => h.DBUserNameID == user_name).ToList();
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

    public bool TryReplaceHabits(List<Habit> habits, string user_name)
    {
        if (!habits.TrueForAll(h => h.UserNameID == user_name))
            return false;
        if (!TryDeleteHabits(user_name)) return false;
        return TryCreateMany(habits);
    }
}
