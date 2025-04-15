using Domain;
using Domain.OutPorts;
using Domain.Models;
using Storage.Models;
using System.Net;

namespace Storage.StorageAdapters;

public class DummySettingsRepo : ISettingsRepo
{
    //Словарь NameId пользователя и настройки
    private Dictionary<string, DBUserSettings> Settings = new();
    //Словарь Id настроек и время
    private Dictionary<Guid, List<DBSTime>> STimes = new();
    public UserSettings? TryGet(string user_name)
    {
        var dbs = Settings.GetValueOrDefault(user_name);
        if (dbs == null)
            return null;
        List<SettingsTime> settingsTimes = new();
        if (STimes.ContainsKey(dbs.Id))
            foreach (DBSTime time in STimes[dbs.Id])
            {
                SettingsTime st = new SettingsTime(time.Id, time.Start, time.End, time.DBSettingsID);
                settingsTimes.Add(st);
            }
        UserSettings s = new UserSettings(dbs.Id, dbs.NotifyOn, dbs.DBUserNameID, settingsTimes);
        return s;
    }

    public bool TryCreate (UserSettings us)
    {
        if (Settings.ContainsKey(us.UserNameID))
        {
            return false;
        }
        DBUserSettings dbus = new DBUserSettings(us.Id, us.NotifyOn, us.UserNameID);
        foreach (var time in us.SettingsTimes)
        {
            DBSTime st = new DBSTime(time.Id, time.Start, time.End, time.SettingsID);
            STimes[us.Id].Add(st);
        }
        Settings[dbus.DBUserNameID] = dbus;
        return true;
    }

    public void Update(UserSettings us)
    {
        DBUserSettings dbus = new DBUserSettings(us.Id, us.NotifyOn, us.UserNameID);
        if (STimes.ContainsKey(us.Id))
        {
            STimes[us.Id].Clear();
            foreach (var time in us.SettingsTimes)
            {
                DBSTime st = new DBSTime(time.Id, time.Start, time.End, time.SettingsID);
                STimes[us.Id].Add(st);
            }
        }
        Settings[dbus.DBUserNameID] = dbus;
        return;
    }

    public void Delete(string user_name)
    {
        if (Settings.ContainsKey(user_name))
        {
            STimes.Remove(Settings[user_name].Id);
        }
        Settings.Remove(user_name);
    }
}
