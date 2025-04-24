using Domain.Models;
using Domain.OutPorts;
using Storage.Models;

namespace Storage.PostgresStorageAdapters;

public class PostgresSettingsRepo : ISettingsRepo
{
    //Словарь NameId пользователя и настройки
    //private Dictionary<string, DBUserSettings> Settings = new();
    //Словарь Id настроек и время
    //private Dictionary<Guid, List<DBSTime>> STimes = new();
    private PostgresDBContext _dbContext;
    public UserSettings? TryGet(string user_name)
    {
        //var dbs = Settings.GetValueOrDefault(user_name);
        var settings = _dbContext.USettings.ToList();
        Console.WriteLine(settings.Count);
        var dbs = _dbContext.USettings.FirstOrDefault(s => s.DBUserID == user_name);
        if (dbs == null)
            return null;

        List<SettingsTime> settingsTimes = [];
        var stimes = _dbContext.SettingsTimes.Where(t => t.DBUserSettingsID == dbs.Id).ToList();
        foreach (DBSTime time in stimes)
        {
            SettingsTime st = new SettingsTime(time.Id, time.Start, time.End, time.DBUserSettingsID);
            settingsTimes.Add(st);
        }
        UserSettings s = new UserSettings(dbs.Id, dbs.NotifyOn, dbs.DBUserID, settingsTimes);
        return s;
    }

    public bool TryCreate(UserSettings us)
    {
        if (_dbContext.USettings.Find(us.Id) != null)
            return false;
        DBUserSettings dbus = new DBUserSettings(us.Id, us.NotifyOn, us.UserNameID);
        List<DBSTime> times = [];
        foreach (var time in us.SettingsTimes)
        {
            DBSTime st = new DBSTime(time.Id, time.Start, time.End, time.SettingsID);
            times.Add(st);
        }
        _dbContext.USettings.Add(dbus);
        _dbContext.SettingsTimes.AddRange(times);
        _dbContext.SaveChanges();
        return true;
    }

    public bool TryUpdate(UserSettings us)
    {
        var dbs = _dbContext.USettings.Find(us.Id);
        if (dbs == null)
            return false;
        List<DBSTime> times = [];
        foreach (var time in us.SettingsTimes)
        {
            DBSTime st = new DBSTime(time.Id, time.Start, time.End, time.SettingsID);
            times.Add(st);
        }
        var prev_times = _dbContext.SettingsTimes.Where(el => el.Id == us.Id);
        _dbContext.SettingsTimes.RemoveRange(prev_times);
        _dbContext.SettingsTimes.AddRange(times);
        dbs.NotifyOn = us.NotifyOn;
        _dbContext.SaveChanges();
        Console.Write(dbs.NotifyOn);
        return true;
        /*
        DBUserSettings dbus = new DBUserSettings(us.Id, us.NotifyOn, us.UserNameID);
        List<DBSTime> times = [];
        foreach (var time in us.SettingsTimes)
        {
            DBSTime st = new DBSTime(time.Id, time.Start, time.End, time.SettingsID);
            times.Add(st);
        }
        _dbContext.USettings.Update(dbus);
        var prev_times = _dbContext.SettingsTimes.Where(el => el.Id == us.Id);
        _dbContext.SettingsTimes.RemoveRange(prev_times);
        _dbContext.SettingsTimes.AddRange(times);
        _dbContext.SaveChanges();
        return;*/
    }

    public bool TryDelete(string user_name)
    {
        var s = _dbContext.USettings.FirstOrDefault(s => s.DBUserID == user_name);
        if (s == null)
            return false;
        var stimes = _dbContext.SettingsTimes.Where(t => t.DBUserSettingsID == s.Id);
        _dbContext.SettingsTimes.RemoveRange(stimes);
        _dbContext.USettings.Remove(s);
        _dbContext.SaveChanges();
        return true;
    }

    public PostgresSettingsRepo(PostgresDBContext dbContext)
    {
        _dbContext = dbContext;
    }
}
