namespace Domain;

using System;
using System.Text.RegularExpressions;
using Types;

public class ActualTime
{
    public Guid Id { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public WeekDay Day { get; }
    public Guid HabitID { get; }

    public ActualTime(Guid id, TimeOnly start, TimeOnly end, WeekDay week_day, Guid habitID)
    {
        Id = id;
        Start = start;
        End = end;
        Day = week_day;
        HabitID = habitID;
    }
}

public class PrefFixedTime
{
    public Guid Id { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public Guid HabitID{ get; }
    
    public PrefFixedTime(Guid id, TimeOnly start, TimeOnly end, Guid habit_id)
    {
        Id = id; 
        Start = start; 
        End = end;
        HabitID = habit_id;
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
    public int NDays { get; } //сколько дней в неделю нужно выполнять

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
}

public class SettingsTime
{
    public Guid Id { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public Guid SettingsID { get; }

    public SettingsTime(Guid id, TimeOnly start, TimeOnly end, Guid settings_id) 
    { 
        Id = id; 
        Start = start; 
        End = end; 
        SettingsID = settings_id;
    }
}

public class UserSettings
{
    public Guid Id { get; }
    public bool NotifyOn { get; set;  }
    public List<SettingsTime> SettingsTimes { get; }
    public Guid UserID { get; }

    public UserSettings(Guid id, bool notify_on, Guid user_id, List<SettingsTime> settings_times)
    {
        Id = id;
        SettingsTimes = settings_times;
        NotifyOn = notify_on;
        UserID = user_id;
    }
}

public class Event
{
    public Guid Id { get; }
    public string Name { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public WeekDay Day { get; }
    public Guid UserID { get; }

    public Event(Guid id, string name, TimeOnly start, TimeOnly end, WeekDay day, Guid user_id)
    {
        Id = id;
        Name = name;
        Start = start;
        End = end;
        Day = day;
        UserID = user_id;
    }
}

public class Message
{
    public Guid Id { get; }
    public string Text { get; }
    public DateOnly DateSent { get; }

    public Message(Guid id, string text, DateOnly date_sent)
    {
        Id = id;
        Text = text;
        DateSent = date_sent;
    }
}

public class User
{
    public Guid Id { get; }
    public string Name { get; }
    public string PasswordHash { get; }
    public PhoneNumber Number { get; }
    public List<Habit> ?Habits { get; set; }
    public List<Event> ?Events { get; set; }
    public UserSettings ?Settings { get; set; }

    public User(Guid id, string name, string passwordHash, PhoneNumber number,
        UserSettings? settings = null, List<Habit>? habits = null, List<Event>? events = null)
    {
        Id = id;
        Name = name;
        PasswordHash = passwordHash;
        Habits = habits;
        Events = events;
        Settings = settings;
        Number = number;
    }
}