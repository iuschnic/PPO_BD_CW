using Domain.Models;
namespace Domain.OutPorts;

public class UserHabitInfo
{
    public string UserName;
    public string HabitName;
    public TimeOnly Start;
    public TimeOnly End;
    public UserHabitInfo(string user_name, string habit_name, TimeOnly start, TimeOnly end) 
    { 
        UserName = user_name;
        HabitName = habit_name;
        Start = start;
        End = end;
    }
}
public interface IMessageRepo
{
    bool TryCreateMessage(Message message, List<string> users);
    public bool TryCreateMessages(List<Tuple<string, string>> users_messages);
    List<UserHabitInfo> GetUsersToNotify();
}
