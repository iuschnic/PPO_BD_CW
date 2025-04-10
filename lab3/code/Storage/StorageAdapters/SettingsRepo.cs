using Domain;
using Domain.OutPorts;
using Domain.Models;
using Storage.Models;
using System.Net;

namespace Storage.StorageAdapters;

public class SettingsRepo : ISettingsRepo
{
    //Словарь Id пользователя и настройки
    private Dictionary<Guid, DBUserSettings> Settings = new();
    //Словарь Id настроек и время
    private Dictionary<Guid, List<DBSTime>> STimes = new();
    public UserSettings? TryGet(Guid user_id)
    {
        var dbs = Settings.GetValueOrDefault(user_id);
        if (dbs == null)
            return null;
        List<SettingsTime> settingsTimes = new();
        if (STimes.ContainsKey(dbs.Id))
            foreach (DBSTime time in STimes[dbs.Id])
            {
                SettingsTime st = new SettingsTime(time.Id, time.Start, time.End, time.DBSettingsID);
                settingsTimes.Add(st);
            }
        UserSettings s = new UserSettings(dbs.Id, dbs.NotifyOn, dbs.DBUserID, settingsTimes);
        return s;
    }

    public bool TryCreate (UserSettings us)
    {
        if (Settings.ContainsKey(us.UserID))
        {
            return false;
        }
        DBUserSettings dbus = new()
        {
            Id = us.Id,
            NotifyOn = us.NotifyOn,
            DBUserID = us.UserID
        };
        foreach (var time in us.SettingsTimes)
        {
            DBSTime st = new()
            {
                Id = time.Id,
                Start = time.Start,
                End = time.End,
                DBSettingsID = time.SettingsID
            };
            STimes[us.Id].Add(st);
        }
        Settings[dbus.DBUserID] = dbus;
        return true;
    }

    public void Update(UserSettings us)
    {
        DBUserSettings dbus = new()
        {
            Id = us.Id,
            NotifyOn = us.NotifyOn,
            DBUserID = us.UserID
        };
        if (STimes.ContainsKey(us.Id))
        {
            STimes[us.Id].Clear();
            foreach (var time in us.SettingsTimes)
            {
                DBSTime st = new()
                {
                    Id = time.Id,
                    Start = time.Start,
                    End = time.End,
                    DBSettingsID = time.SettingsID
                };
                STimes[us.Id].Add(st);
            }
        }
        Settings[dbus.DBUserID] = dbus;
        return;
    }

    public void Delete(Guid user_id)
    {
        if (Settings.ContainsKey(user_id))
        {
            STimes.Remove(Settings[user_id].Id);
        }
        Settings.Remove(user_id);
    }

    public void Save()
    {
        return;
    }
}
