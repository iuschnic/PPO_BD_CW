using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.ComponentModel.DataAnnotations.Schema;
using Types;

namespace Storage;

//[Table("ActualTime")]
public class DBActualTime
{
    public Guid Id { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public string Day { get; set; }
    
    //Внешний ключ
    public Guid DBHabitID { get; set; }
    //public DBHabit? DBHabit { get; set; }
}

//[Table("PrefFixedTime")]
public class DBPrefFixedTime
{
    public Guid Id { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }

    //Внешний ключ
    public Guid DBHabitID { get; set; }
    //public DBHabit? DBHabit { get; set; }
}

//[Table("Habit")]
public class DBHabit
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int MinsToComplete { get; set; }
    //public List<DBActualTime> ActualTimings { get; set; }
    //public List<DBPrefFixedTime> PrefFixedTimings { get; set; }
    public string Option { get; set; }
    public Guid DBUserID { get; set; }
}

//[Table("STime")]
public class DBSTime
{
    public Guid Id { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public Guid DBSettingsID { get; set; }
}

//[Table("Settings")]
public class DBUserSettings
{
    public Guid Id { get; set; }
    public bool NotifyOn {  get; set; }
    public Guid DBUserID { get; set; }
    //public List<DBSTime> ForbiddenTimings { get; set; }
}

//[Table("Event")]
public class DBEvent
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public string Day { get; set; }
    public Guid DBUserID { get; set; }
}

//[Table("Message")]
public class DBMessage
{
    public Guid Id { get; set; }
    public string Text { get; set; }
    public DateOnly DateSent { get; set; }
}

//[Table("UserMessage")]
public class DBUserMessage
{
    public Guid DBUserID { get; set; }
    public Guid DBMessageID { get; set; }
}

//[Table("User")]
public class DBUser
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Number { get; set; }
    public string PasswordHash { get; set; }
    //public List<DBHabit> Habits { get; set; }
    //public List<DBEvent> Events { get; set; }
}



