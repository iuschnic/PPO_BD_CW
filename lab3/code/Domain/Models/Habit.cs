using Types;

namespace Domain.Models;

public class ActualTime
{
    public Guid Id { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public DayOfWeek Day { get; }
    public Guid HabitID { get; }

    public ActualTime(Guid id, TimeOnly start, TimeOnly end, DayOfWeek week_day, Guid habitID)
    {
        Id = id;
        Start = start;
        End = end;
        Day = week_day;
        HabitID = habitID;
    }

    public override string ToString()
    {
        return $"ACTUAL TIME: Weekday: {Day}, Start: {Start}, End: {End}\n";
    }
}

public class PrefFixedTime
{
    public Guid Id { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public Guid HabitID { get; }

    public PrefFixedTime(Guid id, TimeOnly start, TimeOnly end, Guid habit_id)
    {
        Id = id;
        Start = start;
        End = end;
        HabitID = habit_id;
    }
    public override string ToString()
    {
        return $"PREFFERED OR FIXED TIME: Start: {Start}, End: {End}\n";
    }
}

public class Habit
{
    public Guid Id { get; }
    public string Name { get; }
    public int MinsToComplete { get; }
    public List<ActualTime> ActualTimings { get; }
    public List<PrefFixedTime> PrefFixedTimings { get; }
    public TimeOption Option { get; }
    public Guid UserID { get; }
    public int NDays { get; set; } //сколько дней в неделю нужно выполнять

    public Habit(Guid id, string name, int mins_to_complete,
        TimeOption option, Guid user_id, List<ActualTime> actual_timings, List<PrefFixedTime> pref_fixed_timings, int nDays)
    {
        Id = id;
        Name = name;
        MinsToComplete = mins_to_complete;
        ActualTimings = actual_timings;
        PrefFixedTimings = pref_fixed_timings;
        Option = option;
        UserID = user_id;
        NDays = nDays;
    }
    public override string ToString()
    {
        string ans = $"HABIT: Name = {Name}, Mins to complete = {MinsToComplete}, NDays = {NDays}, TimeOpt = {Option}\n";
        foreach (var t in PrefFixedTimings) { ans += "    " + t; }
        if (ActualTimings.Count == 0)
            ans += "    NOT DISTRIBUTED\n";
        else
            foreach (var t in ActualTimings) { ans += "    " + t; }
        return ans;
    }
}
