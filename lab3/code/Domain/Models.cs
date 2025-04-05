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

    public void Print()
    {
        Console.WriteLine("ACTUAL TIME: Weekday: {0}, Start: {1}, End: {2}", Day.StringDay, Start, End);
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

    public void Print()
    {
        Console.WriteLine("PREFFERED OR FIXED TIME:\n" +
            "Start: {1}, End: {2}",  Start, End);
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
    
    public void Print()
    {
        Console.WriteLine("\nHABIT: Name = {0}, Mins to complete = {1}, NDays = {2}, TimeOpt = {3}",
            Name, MinsToComplete, NDays, Option.StringTimeOption);
        foreach (var t in PrefFixedTimings) { t.Print(); }
        if (ActualTimings.Count == 0)
            Console.WriteLine("NOT DISTRIBUTED");
        else
            foreach (var t in ActualTimings) { t.Print(); }
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
    public void Print()
    {
        Console.WriteLine("\nBANNED SETTINGS TIME: Start = {0}, End = {1}", Start, End);
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
    public void Print()
    {
        Console.WriteLine("\nSETTINGS: notifyon = {0}", NotifyOn);
        foreach (var time in SettingsTimes)
        {
            time.Print();
        }
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
    public void Print()
    {
        Console.WriteLine("\nEVENT: Name = {0}, Day = {1}, Start = {2}, End = {3}", Name, Day.StringDay, Start, End);
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

    public void Print()
    {
        Console.WriteLine("\nUSER: Name = {0}, Password = {1}, Number = {2}", Name, PasswordHash, Number.StringNumber);
        if (Habits == null)
            Console.WriteLine("No habits");
        else
            foreach (var h in Habits) { h.Print(); }
        if (Events == null)
            Console.WriteLine("No events");
        else
            foreach (var e in Events) { e.Print(); }
        if (Settings == null)
            Console.WriteLine("No settings");
        else
            Settings.Print();
    }
}