using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Models;

//[Table("Event")]
public class DBEvent
{
    public Guid Id { get; }
    public string Name { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public string Day { get; }
    public string DBUserNameID { get; }
    public DBEvent(Guid id, string name, TimeOnly start, TimeOnly end, DayOfWeek day, string user_name)
    {
        Id = id;
        Name = name;
        Start = start;
        End = end;
        Day = day.ToString();
        DBUserNameID = user_name;
    }
}
