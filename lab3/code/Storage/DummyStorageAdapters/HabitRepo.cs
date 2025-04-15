using Domain;
using Domain.OutPorts;
using Domain.Models;
using Storage.Models;
using Types;

namespace Storage.StorageAdapters;

public class DummyHabitRepo : IHabitRepo
{
    //Моделирует таблицу DBHabits
    private Dictionary<string, List<DBHabit>> UserHabits = new();
    //Моделирует таблицу DBActualTime
    private Dictionary<Guid, List<DBActualTime>> ATimes = new();
    //Моделирует таблицу DBPrefFixedTime
    private Dictionary<Guid, List<DBPrefFixedTime>> PfTimes = new();
    public List<Habit>? Get(string user_name)
    {
        DayOfWeek day;
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
            }
            if (PfTimes.ContainsKey(dbh.Id))
            {
                foreach (var pf in PfTimes[dbh.Id])
                {
                    prefFixedTimes.Add(new PrefFixedTime(pf.Id, pf.Start, pf.End, pf.DBHabitID));
                }
            }
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

    public void Create(Habit h)
    {
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
        if (!UserHabits.ContainsKey(dbh.DBUserNameID))
            UserHabits[dbh.DBUserNameID] = [];
        UserHabits[dbh.DBUserNameID].Add(dbh);
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

    public void DeleteHabits(string user_name)
    {
        if (UserHabits.ContainsKey(user_name))
            foreach(var habit in UserHabits[user_name])
            {
                ATimes.Remove(habit.Id);
                PfTimes.Remove(habit.Id);
            }
        UserHabits.Remove(user_name);
    }
}
