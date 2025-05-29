namespace Storage.Models;

using Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;
using Types;

[Table("actualtime")]
public class DBActualTime
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("a_start")]
    public TimeOnly Start { get; set; }
    [Column("a_end")]
    public TimeOnly End { get; set; }
    [Column("day")]
    public DayOfWeek Day { get; set; }

    //Внешний ключ
    [Column("habit_id")]
    public Guid DBHabitID { get; set; }
    public DBHabit? DBHabit { get; set; }

    public DBActualTime(Guid id, TimeOnly start, TimeOnly end, DayOfWeek day, Guid dBHabitID)
    {
        Id = id;
        Start = start;
        End = end;
        Day = day;
        DBHabitID = dBHabitID;
    }
    public DBActualTime(ActualTime actual)
    {
        Id = actual.Id;
        Start = actual.Start;
        End = actual.End;
        Day = actual.Day;
        DBHabitID = actual.HabitID;
    }
    public ActualTime ToModel()
    {
        return new ActualTime(Id, Start, End, Day, DBHabitID);
    }
}

[Table("preffixedtime")]
public class DBPrefFixedTime
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("pf_start")]
    public TimeOnly Start { get; set; }
    [Column("pf_end")]
    public TimeOnly End { get; set; }

    //Внешний ключ
    [Column("habit_id")]
    public Guid DBHabitID { get; set; }
    public DBHabit? DBHabit { get; set; }
    public DBPrefFixedTime(Guid id, TimeOnly start, TimeOnly end, Guid dBHabitID)
    {
        Id = id;
        Start = start;
        End = end;
        DBHabitID = dBHabitID;
    }
    public DBPrefFixedTime(PrefFixedTime pref)
    {
        Id = pref.Id;
        Start = pref.Start;
        End = pref.End;
        DBHabitID = pref.HabitID;
    }
    public PrefFixedTime ToModel()
    {
        return new PrefFixedTime(Id, Start, End, DBHabitID);
    }
}

[Table("habits")]
public class DBHabit
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [Column("mins_to_complete")]
    public int MinsToComplete { get; set; }
    //Навигационные свойства
    public List<DBActualTime> ActualTimings { get; set; } = [];
    public List<DBPrefFixedTime> PrefFixedTimings { get; set; } = [];
    [Column("option")]
    public TimeOption Option { get; set; }
    //Внешний ключ
    [Column("user_name")]
    public string DBUserNameID { get; set; }
    public DBUser? DBUser { get; set; }
    [Column("ndays")]
    public int CountInWeek { get; set; }
    public DBHabit(Guid id, string name, int minsToComplete, TimeOption option, string dBUserNameID, int countInWeek)
    {
        Id = id;
        Name = name;
        MinsToComplete = minsToComplete;
        Option = option;
        DBUserNameID = dBUserNameID;
        CountInWeek = countInWeek;
    }
    public DBHabit(Habit habit)
    {
        Id = habit.Id;
        Name = habit.Name;
        MinsToComplete = habit.MinsToComplete;
        Option = habit.Option;
        DBUserNameID = habit.UserNameID;
        CountInWeek = habit.CountInWeek;
    }
    public Habit ToModel(List<DBPrefFixedTime> dbpref, List<DBActualTime> dbactual)
    {
        List<PrefFixedTime> pref = [];
        List<ActualTime> actual = [];
        foreach (var at in dbactual)
            actual.Add(at.ToModel());
        foreach (var pf in dbpref)
            pref.Add(pf.ToModel());
        return new Habit(Id, Name, MinsToComplete, Option, DBUserNameID, actual, pref, CountInWeek);
    }
}
