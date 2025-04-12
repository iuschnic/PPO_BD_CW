using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Models;

//[Table("STime")]
public class DBSTime
{
    public Guid Id { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public Guid DBSettingsID { get; set; }
}

//[Table("Settings")]
public class DBUserSettings
{
    public Guid Id { get; set; }
    public bool NotifyOn { get; set; }
    public string DBUserNameID { get; set; }
    //public List<DBSTime> ForbiddenTimings { get; set; }
}
