namespace Storage.Models;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Table("ActualTime")]
public class DBActualTime
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("a_start")]
    public TimeOnly Start { get; set; }
    [Column("a_end")]
    public TimeOnly End { get; set; }
    [Column("day")]
    public string Day { get; set; }

    //Внешний ключ
    [Column("habit_id")]
    public Guid DBHabitID { get; set; }
    //public DBHabit? DBHabit { get; set; }

    public DBActualTime(Guid id, TimeOnly start, TimeOnly end, string day, Guid dbhabitid)
    {
        Id = id;
        Start = start;
        End = end;
        Day = day;
        DBHabitID = dbhabitid;
    }
}

[Table("PrefFixedTime")]
public class DBPrefFixedTime
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("a_start")]
    public TimeOnly Start { get; set; }
    [Column("a_end")]
    public TimeOnly End { get; set; }

    //Внешний ключ
    [Column("habit_id")]
    public Guid DBHabitID { get; set; }
    //public DBHabit? DBHabit { get; set; }
    public DBPrefFixedTime(Guid id, TimeOnly start, TimeOnly end, Guid dbhabitid)
    {
        Id = id;
        Start = start;
        End = end;
        DBHabitID = dbhabitid;
    }
}

[Table("Habits")]
public class DBHabit
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [Column("mins_to_complete")]
    public int MinsToComplete { get; set; }
    //public List<DBActualTime> ActualTimings { get; set; }
    //public List<DBPrefFixedTime> PrefFixedTimings { get; set; }
    [Column("option")]
    public string Option { get; set; }
    [Column("user_name")]
    public string DBUserNameID { get; set; }
    [Column("ndays")]
    public int NDays { get; set; }
    public DBHabit(Guid id, string name, int minsToComplete, string option, string dBUserNameID, int nDays)
    {
        Id = id;
        Name = name;
        MinsToComplete = minsToComplete;
        Option = option;
        DBUserNameID = dBUserNameID;
        NDays = nDays;
    }
}
