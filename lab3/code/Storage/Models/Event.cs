namespace Storage.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Types;

[Table("events")]
public class DBEvent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [Column("e_start")]
    public TimeOnly Start { get; set; }
    [Column("e_end")]
    public TimeOnly End { get; set; }
    [Column("day")]
    public DayOfWeek Day { get; set; }
    [Column("e_option")]
    public EventOption Option { get; set; }
    [Column("e_date")]
    public DateOnly? EDate { get; set; }
    [Column("user_name")]
    public string DBUserNameID { get; set; }
    public DBUser? DBUser { get; set; }
    public DBEvent(Guid id, string name, TimeOnly start, TimeOnly end, string dBUserNameID, EventOption option, DayOfWeek day, DateOnly? eDate)
    {
        Id = id;
        Name = name;
        Start = start;
        End = end;
        Option = option;
        Day = day;
        DBUserNameID = dBUserNameID;
        EDate = eDate;
    }
}
