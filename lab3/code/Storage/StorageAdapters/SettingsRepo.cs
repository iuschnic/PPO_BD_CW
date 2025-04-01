using Domain;
using Domain.OutPorts;
using System.Net;

namespace Storage.StorageAdapters;

public class SettingsRepo : ISettingsRepo
{
    private Dictionary<Guid, DBUserSettings> Settings = new();
    private Dictionary<Guid, List<DBSTime>> STimes = new();
    public UserSettings? Get(Guid user_id)
    {
        var dbs = Settings.GetValueOrDefault(user_id);
        if (dbs == null)
            return null;
        List<SettingsTime> settingsTimes = new();
        foreach (DBSTime time in STimes[dbs.Id])
        {
            SettingsTime st = new SettingsTime(time.Id, time.Start, time.End, time.DBSettingsID);
            settingsTimes.Add(st);
        }
        UserSettings s = new UserSettings(dbs.Id, dbs.NotifyOn, dbs.DBUserID, settingsTimes);
        return s;
    }

    public int Create (UserSettings us)
    {
        if (Settings.ContainsKey(us.Id))
        {
            return -1;
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
        Settings[dbus.Id] = dbus;
        return 0;
    }

    public void Update(UserSettings us)
    {
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
