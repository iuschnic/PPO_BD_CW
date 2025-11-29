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
    public bool NotifyOn { get; }
    public List<SettingsTime> SettingsTimes { get; }
    public string UserNameID { get; }
    public bool TwoFactorEnabled { get; }
    public string? TwoFactorCurrentCode { get; }
    public DateTime? TwoFactorValidUntil { get; }
    public int? PasswordAttempts { get; }
    public DateTime? BlockedUntil { get; }
    public DateTime? PasswordLastChanged { get; }

    public UserSettings(Guid id, bool notifyOn, string userName, List<SettingsTime> settingsTimes, bool twoFactorEnabled = false,
        string? twoFactorCurrentCode = null, DateTime? twoFactorValidUntil = null, int? passwordAttempts = null,
        DateTime? blockedUntil = null, DateTime? passwordLastChanged = null)
    {
        Id = id;
        SettingsTimes = settingsTimes;
        NotifyOn = notifyOn;
        UserNameID = userName;
        TwoFactorEnabled = twoFactorEnabled;
        TwoFactorCurrentCode = twoFactorCurrentCode;
        TwoFactorValidUntil = twoFactorValidUntil;
        PasswordAttempts = passwordAttempts;
        BlockedUntil = blockedUntil;
        PasswordLastChanged = passwordLastChanged;
    }
    public override string ToString()
    {
        string ans = $"SETTINGS: notifyon = {NotifyOn}\n";
        foreach (var time in SettingsTimes)
            ans += time;
        return ans;
    }
}