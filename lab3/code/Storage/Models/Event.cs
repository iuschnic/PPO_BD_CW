using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Models;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Table("Events")]
public class DBEvent
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [Column("e_start")]
    public TimeOnly Start { get; set; }
    [Column("e_end")]
    public TimeOnly End { get; set; }
    [Column("day")]
    public string Day { get; set; }
    [Column("user_name")]
    public string DBUserNameID { get; set; }
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
