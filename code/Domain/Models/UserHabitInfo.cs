namespace Domain.Models;
public class UserHabitInfo(string user_name, string habit_name, TimeOnly start, TimeOnly end)
{
    public string UserName = user_name;
    public string HabitName = habit_name;
    public TimeOnly Start = start;
    public TimeOnly End = end;
}