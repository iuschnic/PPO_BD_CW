using Domain;
using Domain.OutPorts;
using Types;

namespace Storage.StorageAdapters;

public class HabitRepo : IHabitRepo
{
    //Моделирует таблицу DBHabits
    private Dictionary<Guid, List<DBHabit>> UserHabits = new();
    //Моделирует таблицу DBActualTime
    private Dictionary<Guid, List<DBActualTime>> ATimes = new();
    //Моделирует таблицу DBPrefFixedTime
    private Dictionary<Guid, List<DBPrefFixedTime>> PfTimes = new();
    public List<Habit>? Get(Guid user_id)
    {
        var dbhabits = UserHabits.GetValueOrDefault(user_id);
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
                {
                    actualTimes.Add(new ActualTime(at.Id, at.Start, at.End, new WeekDay(at.Day), at.DBHabitID));
                }
            }
            if (PfTimes.ContainsKey(dbh.Id))
            {
                foreach (var pf in PfTimes[dbh.Id])
                {
                    prefFixedTimes.Add(new PrefFixedTime(pf.Id, pf.Start, pf.End, pf.DBHabitID));
                }
            }
            habits.Add(new Habit(dbh.Id, dbh.Name, dbh.MinsToComplete, new TimeOption(dbh.Option), dbh.DBUserID,
                actualTimes, prefFixedTimes, dbh.NDays));
        }
        return habits;
    }

    public void Create(Habit h)
    {
        List<DBActualTime> actualTimes = new();
        List<DBPrefFixedTime> prefFixedTimes = new();
        foreach (var at in h.ActualTimings)
        {
            DBActualTime dbat = new()
            {
                Id = at.Id,
                Start = at.Start,
                End = at.End,
                Day = at.Day.StringDay,
                DBHabitID = at.HabitID,
            };
            actualTimes.Add(dbat);
        }
        foreach (var pf in h.PrefFixedTimings)
        {
            DBPrefFixedTime dbpf = new()
            {
                Id = pf.Id,
                Start = pf.Start,
                End = pf.End,
                DBHabitID = pf.HabitID
            };
            prefFixedTimes.Add(dbpf);
        }
        DBHabit dbh = new()
        {
            Id = h.Id,
            Name = h.Name,
            MinsToComplete = h.MinsToComplete,
            Option = h.Option.StringTimeOption,
            DBUserID = h.UserID,
            NDays = h.NDays
        };
        if (!UserHabits.ContainsKey(dbh.DBUserID))
            UserHabits[dbh.DBUserID] = [];
        UserHabits[dbh.DBUserID].Add(dbh);
        ATimes[h.Id] = actualTimes;
        PfTimes[h.Id] = prefFixedTimes;
    }

    public void CreateMany(List<Habit> habits)
    {
        foreach (var h in habits) 
            Create(h);
    }

    public void Update(Habit h)
    {
        return;
    }

    public void Delete(Guid habit_id)
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
    }

    public void DeleteHabits(Guid user_id)
    {
        if (UserHabits.ContainsKey(user_id))
            foreach(var habit in UserHabits[user_id])
            {
                ATimes.Remove(habit.Id);
                PfTimes.Remove(habit.Id);
            }
        UserHabits.Remove(user_id);
    }

    public void Save()
    {
        return;
    }

}
