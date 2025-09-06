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
        UserSettings s = dbuser.Settings.ToModel(dbuser.Settings.ForbiddenTimings);
        return new User(dbuser.NameID, dbuser.PasswordHash, new PhoneNumber(dbuser.Number), s);
    }
    //Не возвращаются event из за более сложной логики получения, добавляются в TaskTracker
    //В одном запросе трудно отфильтровать привычки с разными опциями, поэтому используется отдельный запрос в EventRepo
    public User? TryFullGet(string username)
    {
        var dbuser = _dbContext.Users
            .Include(u => u.Habits).ThenInclude(h => h.ActualTimings)
            .Include(u => u.Habits).ThenInclude(h => h.PrefFixedTimings)
            .Include(u => u.Settings).ThenInclude(s => s.ForbiddenTimings)
            .FirstOrDefault(u => u.NameID == username);
        if (dbuser == null)
            return null;
        UserSettings s = dbuser.Settings.ToModel(dbuser.Settings.ForbiddenTimings);
        List<Event> events = [];
        List<Habit> habits = [];
        foreach (var h in dbuser.Habits)
            habits.Add(h.ToModel(h.PrefFixedTimings, h.ActualTimings));
        return new User(dbuser.NameID, dbuser.PasswordHash, new PhoneNumber(dbuser.Number), s, habits, events);
    }

    public bool TryCreate(User u)
    {
        if (_dbContext.Users.Find(u.NameID) != null)
            return false;
        if (_dbContext.USettings.Find(u.Settings.Id) != null)
            return false;
        DBUser dbuser = new DBUser(u.NameID, u.Number.StringNumber, u.PasswordHash);
        DBUserSettings dbus = new DBUserSettings(u.Settings);
        List<DBSTime> times = [];
        foreach (var time in u.Settings.SettingsTimes)
        {
            DBSTime st = new DBSTime(time);
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
