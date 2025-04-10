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
    public Guid DBUserID { get; }
    public DBEvent(Guid id, string name, TimeOnly start, TimeOnly end, DayOfWeek day, Guid user_id)
    {
        Id = id;
        Name = name;
        Start = start;
        End = end;
        Day = day.ToString();
        DBUserID = user_id;
    }
}
