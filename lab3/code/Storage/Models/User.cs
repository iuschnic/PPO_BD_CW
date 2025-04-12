using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Models;

public class DBUser
{
    public string NameID { get; set; }
    public string Number { get; set; }
    public string PasswordHash { get; set; }
    //public List<DBHabit> Habits { get; set; }
    //public List<DBEvent> Events { get; set; }
}
