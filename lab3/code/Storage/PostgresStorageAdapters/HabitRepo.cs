using Domain.Models;
using Domain.OutPorts;
using Storage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace Storage.PostgresStorageAdapters;

public class PostgresHabitRepo : IHabitRepo
{
    private PostgresDBContext _dbContext;

    public List<Habit>? TryGet(string user_name)
    {
        var test = _dbContext.Users.Find(user_name);
        if (test == null)
            return null;
        DayOfWeek day;
        var dbhabits = _dbContext.Habits.Where(h => h.DBUserNameID == user_name).ToList();
        if (dbhabits == null)
            return [];
        List<Habit> habits = new();
        foreach (var dbh in dbhabits)
        {
            List<ActualTime> actualTimes = new();
            List<PrefFixedTime> prefFixedTimes = new();
            var atimes = _dbContext.ActualTimes.Where(t => t.DBHabitID == dbh.Id).ToList();
            foreach (var at in atimes)
            {
                if (at.Day == "Monday")
                    day = DayOfWeek.Monday;
                else if (at.Day == "Tuesday")
                    day = DayOfWeek.Tuesday;
                else if (at.Day == "Wednesday")
                    day = DayOfWeek.Wednesday;
                else if (at.Day == "Thursday")
                    day = DayOfWeek.Thursday;
                else if (at.Day == "Friday")
                    day = DayOfWeek.Friday;
                else if (at.Day == "Saturday")
                    day = DayOfWeek.Saturday;
                else
                    day = DayOfWeek.Sunday;
                actualTimes.Add(new ActualTime(at.Id, at.Start, at.End, day, at.DBHabitID));
            }
            var pftimes = _dbContext.PrefFixedTimes.Where(t => t.DBHabitID == dbh.Id).ToList();
            foreach (var pf in pftimes)
                prefFixedTimes.Add(new PrefFixedTime(pf.Id, pf.Start, pf.End, pf.DBHabitID));
            TimeOption op;
            if (dbh.Option == "Fixed")
                op = TimeOption.Fixed;
            else if (dbh.Option == "Preffered")
                op = TimeOption.Preffered;
            else
                op = TimeOption.NoMatter;
            habits.Add(new Habit(dbh.Id, dbh.Name, dbh.MinsToComplete, op, dbh.DBUserNameID,
                actualTimes, prefFixedTimes, dbh.NDays));
        }
        return habits;
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
            DBActualTime dbat = new DBActualTime(at.Id, at.Start, at.End, at.Day.ToString(), at.HabitID);
            actualTimes.Add(dbat);
        }
        foreach (var pf in h.PrefFixedTimings)
        {
            DBPrefFixedTime dbpf = new DBPrefFixedTime(pf.Id, pf.Start, pf.End, pf.HabitID);
            prefFixedTimes.Add(dbpf);
        }
        string op;
        if (h.Option == TimeOption.Fixed)
            op = "Fixed";
        else if (h.Option == TimeOption.Preffered)
            op = "Preffered";
        else
            op = "NoMatter";
        DBHabit dbh = new DBHabit(h.Id, h.Name, h.MinsToComplete, op, h.UserNameID, h.NDays);
        _dbContext.Habits.Add(dbh);
        _dbContext.ActualTimes.AddRange(actualTimes);
        _dbContext.PrefFixedTimes.AddRange(prefFixedTimes);
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryCreateMany(List<Habit> habits)
    {
        foreach (var h in habits)
        {
            var test = _dbContext.Habits.Find(h.Id);
            if (test != null)
                return false;
            List<DBActualTime> actualTimes = new();
            List<DBPrefFixedTime> prefFixedTimes = new();
            foreach (var at in h.ActualTimings)
            {
                DBActualTime dbat = new DBActualTime(at.Id, at.Start, at.End, at.Day.ToString(), at.HabitID);
                actualTimes.Add(dbat);
            }
            foreach (var pf in h.PrefFixedTimings)
            {
                DBPrefFixedTime dbpf = new DBPrefFixedTime(pf.Id, pf.Start, pf.End, pf.HabitID);
                prefFixedTimes.Add(dbpf);
            }
            string op;
            if (h.Option == TimeOption.Fixed)
                op = "Fixed";
            else if (h.Option == TimeOption.Preffered)
                op = "Preffered";
            else
                op = "NoMatter";
            DBHabit dbh = new DBHabit(h.Id, h.Name, h.MinsToComplete, op, h.UserNameID, h.NDays);
            _dbContext.Habits.Add(dbh);
            _dbContext.ActualTimes.AddRange(actualTimes);
            _dbContext.PrefFixedTimes.AddRange(prefFixedTimes);
        }
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryUpdate(Habit h)
    {
        return true;
    }

    public bool TryDelete(Guid habit_id)
    {
        var habit = _dbContext.Habits.Find(habit_id);
        if (habit == null)
            return false;
        var atimes = _dbContext.ActualTimes.Where(t => t.DBHabitID == habit_id).ToList();
        var ptimes = _dbContext.PrefFixedTimes.Where(t => t.DBHabitID == habit_id).ToList();
        _dbContext.ActualTimes.RemoveRange(atimes);
        _dbContext.PrefFixedTimes.RemoveRange(ptimes);
        _dbContext.Habits.Remove(habit);
        _dbContext.SaveChanges();
        return true;
    }

    //Вроде ОК но написать тесты
    public bool TryDeleteHabits(string user_name)
    {
        var dbu = _dbContext.Users.Find(user_name);
        if (dbu == null) return false;
        var habits = _dbContext.Habits.Where(h => h.DBUserNameID == user_name).ToList();
        foreach (var habit in habits)
        {
            var atimes = _dbContext.ActualTimes.Where(t => t.DBHabitID == habit.Id).ToList();
            var ptimes = _dbContext.PrefFixedTimes.Where(t => t.DBHabitID == habit.Id).ToList();
            _dbContext.ActualTimes.RemoveRange(atimes);
            _dbContext.PrefFixedTimes.RemoveRange(ptimes);
        }
        _dbContext.Habits.RemoveRange(habits);
        _dbContext.SaveChanges();
        return true;
    }

    public PostgresHabitRepo(PostgresDBContext dbContext)
    {
        _dbContext = dbContext;
    }
}
