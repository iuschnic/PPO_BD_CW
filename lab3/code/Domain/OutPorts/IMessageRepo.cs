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
    public bool TryCreateMessages(List<Message> users_messages);
    public List<Message> GetMessagesToSend();
    public bool MarkMessagesSent(List<Message> messages);
    public List<UserHabitInfo> GetUsersToNotify();
}
