using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Table("users")]
public class DBUser
{
    [Key]
    [Column("name")]
    public string NameID { get; set; }
    [Column("number")]
    public string Number { get; set; }
    [Column("password_hash")]
    public string PasswordHash { get; set; }
    //Навигационные свойства
    public List<DBHabit> Habits { get; set; } = [];
    public List<DBEvent> Events { get; set; } = [];
    public DBUserSettings Settings { get; set; }
    public DBUser(string nameID, string number, string passwordHash)
    {
        NameID = nameID;
        Number = number;
        PasswordHash = passwordHash;
    }
}
