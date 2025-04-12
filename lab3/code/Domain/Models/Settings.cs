using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models;

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
    public override string ToString()
    {
        return $"BANNED SETTINGS TIME: Start = {Start}, End = {End}\n";
    }
}

public class UserSettings
{
    public Guid Id { get; }
    public bool NotifyOn { get; set; }
    public List<SettingsTime> SettingsTimes { get; }
    public string UserNameID { get; }

    public UserSettings(Guid id, bool notify_on, string user_name, List<SettingsTime> settings_times)
    {
        Id = id;
        SettingsTimes = settings_times;
        NotifyOn = notify_on;
        UserNameID = user_name;
    }
    public override string ToString()
    {
        string ans = $"SETTINGS: notifyon = {NotifyOn}\n";
        foreach (var time in SettingsTimes)
            ans += time;
        return ans;
    }
}
