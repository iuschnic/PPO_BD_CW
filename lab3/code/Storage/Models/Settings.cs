using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Models;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Table("SettingsTime")]
public class DBSTime
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("s_start")]
    public TimeOnly Start { get; set; }
    [Column("s_end")]
    public TimeOnly End { get; set; }
    [Column("settings_id")]
    public Guid DBSettingsID { get; set; }
    public DBSTime(Guid id, TimeOnly start, TimeOnly end, Guid dbsettingsid) 
    {
        Id = id;
        Start = start;
        End = end;
        DBSettingsID = dbsettingsid;
    }
}

[Table("Settings")]
public class DBUserSettings
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("notify_on")]
    public bool NotifyOn { get; set; }
    [Column("user_name")]
    public string DBUserNameID { get; set; }
    //public List<DBSTime> ForbiddenTimings { get; set; }
    public DBUserSettings(Guid id, bool notifyon, string dBUserNameID)
    {
        Id = id;
        NotifyOn = notifyon;
        DBUserNameID = dBUserNameID;
    }
}
