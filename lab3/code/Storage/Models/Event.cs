namespace Storage.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public string Day { get; set; }
    [Column("user_name")]
    public string? DBUserNameID { get; set; }
    public DBUser? DBUser { get; set; }
    public DBEvent(Guid id, string name, TimeOnly start, TimeOnly end, string day, string dBUserNameID)
    {
        Id = id;
        Name = name;
        Start = start;
        End = end;
        Day = day;
        DBUserNameID = dBUserNameID;
    }
}
