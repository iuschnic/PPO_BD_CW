namespace Storage.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Types;

[Table("actual_time")]
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
    //public string Day { get; set; }
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
}

[Table("pref_fixed_time")]
public class DBPrefFixedTime
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("a_start")]
    public TimeOnly Start { get; set; }
    [Column("a_end")]
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
    //public string Option { get; set; }
    public TimeOption Option { get; set; }
    [Column("user_name")]
    //Внешний ключ
    public string? DBUserNameID { get; set; }
    public DBUser? DBUser { get; set; }
    [Column("ndays")]
    public int NDays { get; set; }
    public DBHabit(Guid id, string name, int minsToComplete, TimeOption option, string dBUserNameID, int nDays)
    {
        Id = id;
        Name = name;
        MinsToComplete = minsToComplete;
        Option = option;
        DBUserNameID = dBUserNameID;
        NDays = nDays;
    }
}
