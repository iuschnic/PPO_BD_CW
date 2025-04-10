using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace Domain.Models;

public class User
{
    public Guid Id { get; }
    public string Name { get; }
    public string PasswordHash { get; }
    public PhoneNumber Number { get; }
    public List<Habit>? Habits { get; set; }
    public List<Event>? Events { get; set; }
    public UserSettings? Settings { get; set; }

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

    public override string ToString()
    {
        string ans = $"\nUSER: Name = {Name}, Password = {PasswordHash}, Number = {Number}\n";
        if (Habits == null || Habits.Count == 0)
            ans += "\nNO HABITS\n";
        else
        {
            ans += "\n";
            foreach (var h in Habits) { ans += h; }
        }
        if (Events == null || Events.Count == 0)
            ans += "\nNO EVENTS\n";
        else
        {
            ans += "\n";
            foreach (var e in Events) { ans += e; }
        }
        if (Settings == null)
            ans += "\nNo settings\n";
        else
        {
            ans += "\n";
            ans += Settings;
        }
        return ans + "\n";
    }
}
