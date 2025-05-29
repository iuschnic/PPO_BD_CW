namespace Storage.Models;

using Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("settingstime")]
public class DBSTime
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("s_start")]
    public TimeOnly Start { get; set; }
    [Column("s_end")]
    public TimeOnly End { get; set; }
    [Column("settings_id")]
    public Guid DBUserSettingsID { get; set; }
    public DBUserSettings? DBUserSettings { get; set; }
    public DBSTime(Guid id, TimeOnly start, TimeOnly end, Guid dBUserSettingsID)
    {
        Id = id;
        Start = start;
        End = end;
        DBUserSettingsID = dBUserSettingsID;
    }
    public DBSTime(SettingsTime t)
    {
        Id = t.Id;
        Start = t.Start;
        End = t.End;
        DBUserSettingsID = t.SettingsID;
    }
    public SettingsTime ToModel()
    {
        return new SettingsTime(Id, Start, End, DBUserSettingsID);
    }
}


[Table("settings")]
public class DBUserSettings
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("notify_on")]
    public bool NotifyOn { get; set; }
    [Column("user_name")]
    public string DBUserID { get; set; }
    public DBUser? DBUser { get; set; }
    public List<DBSTime> ForbiddenTimings { get; set; } = [];
    public DBUserSettings(Guid id, bool notifyOn, string dBUserID)
    {
        Id = id;
        NotifyOn = notifyOn;
        DBUserID = dBUserID;
    }
    public DBUserSettings(UserSettings userSettings)
    {
        Id = userSettings.Id;
        NotifyOn = userSettings.NotifyOn;
        DBUserID = userSettings.UserNameID;
    }
    public UserSettings ToModel(List<DBSTime> forbiddenTimings)
    {
        List<SettingsTime> times = [];
        foreach (var t in forbiddenTimings)
            times.Add(t.ToModel());
        return new UserSettings(Id, NotifyOn, DBUserID, times);
    }
}
