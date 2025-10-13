using Domain.Models;
using Domain.OutPorts;
using Microsoft.EntityFrameworkCore;
using Storage.Models;
using Types;

namespace Storage.EfAdapters;

public class EfUserRepo(ITaskTrackerContext dbContext) : IUserRepo
{
    ITaskTrackerContext _dbContext = dbContext;

    public async Task<User?> TryGetAsync(string username)
    {
        var dbuser = await _dbContext.Users
            .Include(u => u.Settings).ThenInclude(s => s.ForbiddenTimings)
            .FirstOrDefaultAsync(u => u.NameID == username);
        if (dbuser == null)
            return null;
        if (dbuser.Settings == null)
            return null;
        UserSettings s = dbuser.Settings.ToModel(dbuser.Settings.ForbiddenTimings);
        return new User(dbuser.NameID, dbuser.PasswordHash, new PhoneNumber(dbuser.Number), s);
    }
    public User? TryGet(string username)
    {
        var dbuser = _dbContext.Users
            .Include(u => u.Settings).ThenInclude(s => s.ForbiddenTimings)
            .FirstOrDefault(u => u.NameID == username);
        if (dbuser == null)
            return null;
        if (dbuser.Settings == null)
            return null;
        UserSettings s = dbuser.Settings.ToModel(dbuser.Settings.ForbiddenTimings);
        return new User(dbuser.NameID, dbuser.PasswordHash, new PhoneNumber(dbuser.Number), s);
    }
    public async Task<User?> TryFullGetAsync(string username)
    {
        var dbuser = await _dbContext.Users
            .Include(u => u.Habits).ThenInclude(h => h.ActualTimings)
            .Include(u => u.Habits).ThenInclude(h => h.PrefFixedTimings)
            .Include(u => u.Settings).ThenInclude(s => s.ForbiddenTimings)
            .FirstOrDefaultAsync(u => u.NameID == username);
        if (dbuser == null)
            return null;
        if (dbuser.Settings == null)
            return null;
        UserSettings s = dbuser.Settings.ToModel(dbuser.Settings.ForbiddenTimings);
        List<Event> events = [];
        List<Habit> habits = [];
        foreach (var h in dbuser.Habits)
            habits.Add(h.ToModel(h.PrefFixedTimings, h.ActualTimings));
        return new User(dbuser.NameID, dbuser.PasswordHash, new PhoneNumber(dbuser.Number), s, habits, events);
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
        if (dbuser.Settings == null)
            return null;
        UserSettings s = dbuser.Settings.ToModel(dbuser.Settings.ForbiddenTimings);
        List<Event> events = [];
        List<Habit> habits = [];
        foreach (var h in dbuser.Habits)
            habits.Add(h.ToModel(h.PrefFixedTimings, h.ActualTimings));
        return new User(dbuser.NameID, dbuser.PasswordHash, new PhoneNumber(dbuser.Number), s, habits, events);
    }
    public async Task<bool> TryCreateAsync(User u)
    {
        if (await _dbContext.Users.FindAsync(u.NameID) != null)
            return false;
        if (await _dbContext.USettings.FindAsync(u.Settings.Id) != null)
            return false;
        DBUser dbuser = new(u.NameID, u.Number.StringNumber, u.PasswordHash);
        DBUserSettings dbus = new(u.Settings);
        List<DBSTime> times = [];
        foreach (var time in u.Settings.SettingsTimes)
        {
            DBSTime st = new(time);
            times.Add(st);
        }
        _dbContext.Users.Add(dbuser);
        _dbContext.USettings.Add(dbus);
        _dbContext.SettingsTimes.AddRange(times);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public bool TryCreate(User u)
    {
        if (_dbContext.Users.Find(u.NameID) != null)
            return false;
        if (_dbContext.USettings.Find(u.Settings.Id) != null)
            return false;
        DBUser dbuser = new(u.NameID, u.Number.StringNumber, u.PasswordHash);
        DBUserSettings dbus = new(u.Settings);
        List<DBSTime> times = [];
        foreach (var time in u.Settings.SettingsTimes)
        {
            DBSTime st = new(time);
            times.Add(st);
        }
        _dbContext.Users.Add(dbuser);
        _dbContext.USettings.Add(dbus);
        _dbContext.SettingsTimes.AddRange(times);
        _dbContext.SaveChanges();
        return true;
    }
    public async Task<bool> TryUpdateUserAsync(User u)
    {
        var dbu = await _dbContext.Users.FindAsync(u.NameID);
        if (dbu == null)
            return false;
        dbu.Number = u.Number.StringNumber;
        dbu.PasswordHash = u.PasswordHash;
        await _dbContext.SaveChangesAsync();
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
    public async Task<bool> TryUpdateSettingsAsync(UserSettings us)
    {
        var dbs = await _dbContext.USettings.FindAsync(us.Id);
        if (dbs == null)
            return false;
        List<DBSTime> times = [];
        foreach (var time in us.SettingsTimes)
        {
            DBSTime st = new(time.Id, time.Start, time.End, dbs.Id);
            times.Add(st);
        }
        var prev_times = await _dbContext.SettingsTimes.Where(el => el.DBUserSettingsID == us.Id).ToListAsync();
        _dbContext.SettingsTimes.RemoveRange(prev_times);
        _dbContext.SettingsTimes.AddRange(times);
        dbs.NotifyOn = us.NotifyOn;
        await _dbContext.SaveChangesAsync();
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
            DBSTime st = new(time.Id, time.Start, time.End, dbs.Id);
            times.Add(st);
        }
        var prev_times = _dbContext.SettingsTimes.Where(el => el.DBUserSettingsID == us.Id).ToList();
        _dbContext.SettingsTimes.RemoveRange(prev_times);
        _dbContext.SettingsTimes.AddRange(times);
        dbs.NotifyOn = us.NotifyOn;
        _dbContext.SaveChanges();
        return true;
    }
    public async Task<bool> TryUpdateSettingsAsync(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string user_name)
    {
        if (newTimings == null && notifyOn == null)
            return false;
        var dbuser = await _dbContext.Users
            .Include(u => u.Settings).ThenInclude(s => s.ForbiddenTimings)
            .FirstOrDefaultAsync(u => u.NameID == user_name);
        if (dbuser == null)
            return false;
        if (dbuser.Settings == null)
            return false;
        if (newTimings != null)
        {
            List<DBSTime> times = [];
            foreach (var time in newTimings)
            {
                DBSTime st = new(Guid.NewGuid(), time.Item1, time.Item2, dbuser.Settings.Id);
                times.Add(st);
            }
            _dbContext.SettingsTimes.RemoveRange(dbuser.Settings.ForbiddenTimings);
            _dbContext.SettingsTimes.AddRange(times);
        }
        if (notifyOn != null)
            dbuser.Settings.NotifyOn = notifyOn.Value;
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public bool TryUpdateSettings(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string user_name)
    {
        if (newTimings == null && notifyOn == null)
            return false;
        var dbuser = _dbContext.Users
            .Include(u => u.Settings).ThenInclude(s => s.ForbiddenTimings)
            .FirstOrDefault(u => u.NameID == user_name);
        if (dbuser == null)
            return false;
        if (dbuser.Settings == null)
            return false;
        if (newTimings != null)
        {
            List<DBSTime> times = [];
            foreach (var time in newTimings)
            {
                DBSTime st = new(Guid.NewGuid(), time.Item1, time.Item2, dbuser.Settings.Id);
                times.Add(st);
            }
            _dbContext.SettingsTimes.RemoveRange(dbuser.Settings.ForbiddenTimings);
            _dbContext.SettingsTimes.AddRange(times);
        }
        if (notifyOn != null)
            dbuser.Settings.NotifyOn = notifyOn.Value;
        _dbContext.SaveChanges();
        return true;
    }
    public async Task<bool> TryUpdateNotificationTimingsAsync(List<Tuple<TimeOnly, TimeOnly>> newTimings, string user_name)
    {
        var dbuser = await _dbContext.Users
            .Include(u => u.Settings).ThenInclude(s => s.ForbiddenTimings)
            .FirstOrDefaultAsync(u => u.NameID == user_name);
        if (dbuser == null)
            return false;
        if (dbuser.Settings == null)
            return false;

        List<DBSTime> times = [];
        foreach (var time in newTimings)
        {
            DBSTime st = new(Guid.NewGuid(), time.Item1, time.Item2, dbuser.Settings.Id);
            times.Add(st);
        }
        _dbContext.SettingsTimes.RemoveRange(dbuser.Settings.ForbiddenTimings);
        _dbContext.SettingsTimes.AddRange(times);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public bool TryUpdateNotificationTimings(List<Tuple<TimeOnly, TimeOnly>> newTimings, string user_name)
    {
        var dbuser = _dbContext.Users
            .Include(u => u.Settings).ThenInclude(s => s.ForbiddenTimings)
            .FirstOrDefault(u => u.NameID == user_name);
        if (dbuser == null)
            return false;
        if (dbuser.Settings == null)
            return false;

        List<DBSTime> times = [];
        foreach (var time in newTimings)
        {
            DBSTime st = new(Guid.NewGuid(), time.Item1, time.Item2, dbuser.Settings.Id);
            times.Add(st);
        }
        _dbContext.SettingsTimes.RemoveRange(dbuser.Settings.ForbiddenTimings);
        _dbContext.SettingsTimes.AddRange(times);
        _dbContext.SaveChanges();
        return true;
    }

    public async Task<bool> TryNotificationsOnAsync(string user_name)
    {
        var dbuser = await _dbContext.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.NameID == user_name);
        if (dbuser == null)
            return false;
        if (dbuser.Settings == null)
            return false;
        dbuser.Settings.NotifyOn = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public bool TryNotificationsOn(string user_name)
    {
        var dbuser = _dbContext.Users
            .Include(u => u.Settings)
            .FirstOrDefault(u => u.NameID == user_name);
        if (dbuser == null)
            return false;
        if (dbuser.Settings == null)
            return false;
        dbuser.Settings.NotifyOn = true;
        _dbContext.SaveChanges();
        return true;
    }
    public async Task<bool> TryNotificationsOffAsync(string user_name)
    {
        var dbuser = await _dbContext.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.NameID == user_name);
        if (dbuser == null)
            return false;
        if (dbuser.Settings == null)
            return false;
        dbuser.Settings.NotifyOn = false;
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public bool TryNotificationsOff(string user_name)
    {
        var dbuser = _dbContext.Users
            .Include(u => u.Settings)
            .FirstOrDefault(u => u.NameID == user_name);
        if (dbuser == null)
            return false;
        if (dbuser.Settings == null)
            return false;
        dbuser.Settings.NotifyOn = false;
        _dbContext.SaveChanges();
        return true;
    }

    public async Task<bool> TryDeleteAsync(string user_name)
    {
        var dbu = await _dbContext.Users.FindAsync(user_name);
        if (dbu == null)
            return false;
        var s = await _dbContext.USettings.FirstOrDefaultAsync(s => s.DBUserID == user_name);
        if (s == null)
            return false;
        var stimes = await _dbContext.SettingsTimes.Where(t => t.DBUserSettingsID == s.Id).ToListAsync();
        _dbContext.SettingsTimes.RemoveRange(stimes);
        _dbContext.USettings.Remove(s);
        _dbContext.Users.Remove(dbu);
        await _dbContext.SaveChangesAsync();

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
        var stimes = _dbContext.SettingsTimes.Where(t => t.DBUserSettingsID == s.Id).ToList();
        _dbContext.SettingsTimes.RemoveRange(stimes);
        _dbContext.USettings.Remove(s);
        _dbContext.Users.Remove(dbu);
        _dbContext.SaveChanges();

        return true;
    }
    public async Task<bool> TryCheckLogInAsync(string login, string password)
    {
        var dbu = await _dbContext.Users.FindAsync(login);
        return dbu != null && dbu.PasswordHash == password;
    }
    public bool TryCheckLogIn(string login, string password)
    {
        var dbu = _dbContext.Users.Find(login);
        return dbu != null && dbu.PasswordHash == password;
    }
}
