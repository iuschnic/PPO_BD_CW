using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Models;

//[Table("ActualTime")]
public class DBActualTime
{
    public Guid Id { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public string Day { get; set; }

    //Внешний ключ
    public Guid DBHabitID { get; set; }
    //public DBHabit? DBHabit { get; set; }
}

//[Table("PrefFixedTime")]
public class DBPrefFixedTime
{
    public Guid Id { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }

    //Внешний ключ
    public Guid DBHabitID { get; set; }
    //public DBHabit? DBHabit { get; set; }
}

//[Table("Habit")]
public class DBHabit
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int MinsToComplete { get; set; }
    //public List<DBActualTime> ActualTimings { get; set; }
    //public List<DBPrefFixedTime> PrefFixedTimings { get; set; }
    public string Option { get; set; }
    public Guid DBUserID { get; set; }
    public int NDays { get; set; }
}
