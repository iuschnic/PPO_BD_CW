using Domain.Models;
using Domain.OutPorts;
using Microsoft.EntityFrameworkCore;
using Storage.Models;
using Types;

namespace Storage.PostgresStorageAdapters;

public class PostgresUserRepo : IUserRepo
{
    PostgresDBContext _dbContext;

    public PostgresUserRepo(PostgresDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public User? TryGet(string username)
    {
        var dbuser = _dbContext.Users
            .Include(u => u.Settings).ThenInclude(s => s.ForbiddenTimings)
            .FirstOrDefault(u => u.NameID == username);
        if (dbuser == null)
            return null;
        List<SettingsTime> settingsTimes = [];
        foreach (DBSTime time in dbuser.Settings.ForbiddenTimings)
        {
            SettingsTime st = new SettingsTime(time.Id, time.Start, time.End, time.DBUserSettingsID);
            settingsTimes.Add(st);
        }
        UserSettings s = new UserSettings(dbuser.Settings.Id, dbuser.Settings.NotifyOn, dbuser.Settings.DBUserID, settingsTimes);
        return new User(dbuser.NameID, dbuser.PasswordHash, new PhoneNumber(dbuser.Number), s);
    }

    public User? TryFullGet(string username)
    {
        var dbuser = _dbContext.Users.Include(u => u.Events)
            .Include(u => u.Events)
            .Include(u => u.Habits).ThenInclude(h => h.ActualTimings)
            .Include(u => u.Habits).ThenInclude(h => h.PrefFixedTimings)
            .Include(u => u.Settings).ThenInclude(s => s.ForbiddenTimings)
            .FirstOrDefault(u => u.NameID == username);
        if (dbuser == null)
            return null;
        List<SettingsTime> settingsTimes = [];
        foreach (DBSTime time in dbuser.Settings.ForbiddenTimings)
        {
            SettingsTime st = new SettingsTime(time.Id, time.Start, time.End, time.DBUserSettingsID);
            settingsTimes.Add(st);
        }
        UserSettings s = new UserSettings(dbuser.Settings.Id, dbuser.Settings.NotifyOn, dbuser.Settings.DBUserID, settingsTimes);
        List<Event> events = [];
        foreach (var ev in dbuser.Events)
            events.Add(new Event(ev.Id, ev.Name, ev.Start, ev.End, ev.Day, ev.DBUserNameID));
        List<Habit> habits = [];
        foreach (var h in dbuser.Habits)
        {
            List<ActualTime> actualTimes = [];
            List<PrefFixedTime> preffixedTimes = [];
            foreach (var pf in h.PrefFixedTimings)
                preffixedTimes.Add(new PrefFixedTime(pf.Id, pf.Start, pf.End, pf.DBHabitID));
            foreach (var a in h.ActualTimings)
                actualTimes.Add(new ActualTime(a.DBHabitID, a.Start, a.End, a.Day, a.DBHabitID));
            habits.Add(new Habit(h.Id, h.Name, h.MinsToComplete, h.Option, h.DBUserNameID, actualTimes, preffixedTimes, h.NDays));
        }
        return new User(dbuser.NameID, dbuser.PasswordHash, new PhoneNumber(dbuser.Number), s, habits, events);
    }

    public bool TryCreate(User u)
    {
        if (_dbContext.Users.Find(u.NameID) != null)
            return false;
        if (_dbContext.USettings.Find(u.Settings.Id) != null)
            return false;
        DBUser dbuser = new DBUser(u.NameID, u.Number.StringNumber, u.PasswordHash);
        DBUserSettings dbus = new DBUserSettings(u.Settings.Id, u.Settings.NotifyOn, u.Settings.UserNameID);
        List<DBSTime> times = [];
        foreach (var time in u.Settings.SettingsTimes)
        {
            DBSTime st = new DBSTime(time.Id, time.Start, time.End, time.SettingsID);
            times.Add(st);
        }
        _dbContext.Users.Add(dbuser);
        _dbContext.USettings.Add(dbus);
        _dbContext.SettingsTimes.AddRange(times);
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryUpdateUser(User u)
    {
        var dbu = _dbContext.Users.Find(u.NameID);
        if (dbu == null)
            return false;
        dbu.Number = u.Number.StringNumber;
        dbu.PasswordHash = u.PasswordHash;
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryUpdateSettings(UserSettings us)
    {
        var dbs = _dbContext.USettings.Find(us.Id);
        if (dbs == null)
            return false;
        List<DBSTime> times = [];
        foreach (var time in us.SettingsTimes)
        {
            DBSTime st = new DBSTime(time.Id, time.Start, time.End, dbs.Id);
            times.Add(st);
        }
        var prev_times = _dbContext.SettingsTimes.Where(el => el.DBUserSettingsID == us.Id);
        _dbContext.SettingsTimes.RemoveRange(prev_times);
        _dbContext.SettingsTimes.AddRange(times);
        dbs.NotifyOn = us.NotifyOn;
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryDelete(string user_name)
    {
        var dbu = _dbContext.Users.Find(user_name);
        if (dbu == null)
            return false;
        var s = _dbContext.USettings.FirstOrDefault(s => s.DBUserID == user_name);
        if (s == null)
            return false;
        var stimes = _dbContext.SettingsTimes.Where(t => t.DBUserSettingsID == s.Id);
        _dbContext.SettingsTimes.RemoveRange(stimes);
        _dbContext.USettings.Remove(s);
        _dbContext.Users.Remove(dbu);
        _dbContext.SaveChanges();

        return true;
    }
}
