using Domain;
using Domain.OutPorts;
using Domain.Models;
using Storage.Models;
using Types;

namespace Storage.StorageAdapters;

/*public class DummyHabitRepo : IHabitRepo
{
    //Моделирует таблицу DBHabits
    private Dictionary<string, List<DBHabit>> UserHabits = new();
    //Моделирует таблицу DBActualTime
    private Dictionary<Guid, List<DBActualTime>> ATimes = new();
    //Моделирует таблицу DBPrefFixedTime
    private Dictionary<Guid, List<DBPrefFixedTime>> PfTimes = new();
    public List<Habit>? TryGet(string user_name)
    {
        var dbhabits = UserHabits.GetValueOrDefault(user_name);
        if (dbhabits == null)
            return [];
        List<Habit> habits = new();
        foreach (var dbh in dbhabits)
        {
            List<ActualTime> actualTimes = new();
            List<PrefFixedTime> prefFixedTimes = new();
            if (ATimes.ContainsKey(dbh.Id))
            {
                foreach (var at in ATimes[dbh.Id])
                    actualTimes.Add(new ActualTime(at.Id, at.Start, at.End, at.Day, at.DBHabitID));
            }
            if (PfTimes.ContainsKey(dbh.Id))
            {
                foreach (var pf in PfTimes[dbh.Id])
                {
                    prefFixedTimes.Add(new PrefFixedTime(pf.Id, pf.Start, pf.End, pf.DBHabitID));
                }
            }
            habits.Add(new Habit(dbh.Id, dbh.Name, dbh.MinsToComplete, dbh.Option, dbh.DBUserNameID,
                actualTimes, prefFixedTimes, dbh.NDays));
        }
        return habits;
    }

    public bool TryCreate(Habit h)
    {
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
        if (!UserHabits.ContainsKey(dbh.DBUserNameID))
            UserHabits[dbh.DBUserNameID] = [];
        UserHabits[dbh.DBUserNameID].Add(dbh);
        ATimes[h.Id] = actualTimes;
        PfTimes[h.Id] = prefFixedTimes;
        return true;
    }

    public bool TryCreateMany(List<Habit> habits)
    {
        foreach (var h in habits) 
            TryCreate(h);
        return true;
    }

    public bool TryUpdate(Habit h)
    {
        return true;
    }

    public bool TryDelete(Guid habit_id)
    {
        foreach(var habits in UserHabits)
        {
            bool flag = false;
            for (int i = 0; i < habits.Value.Count; i++) {
                if (habits.Value[i].Id == habit_id)
                {  
                    flag = true;
                    habits.Value.RemoveAt(i);
                    ATimes.Remove(habit_id);
                    PfTimes.Remove(habit_id);
                    break; 
                }
            }
            if (flag)
                break;
        }
        return true;
    }

    public bool TryDeleteHabits(string user_name)
    {
        if (UserHabits.ContainsKey(user_name))
            foreach(var habit in UserHabits[user_name])
            {
                ATimes.Remove(habit.Id);
                PfTimes.Remove(habit.Id);
            }
        UserHabits.Remove(user_name);
        return true;
    }
}
*/