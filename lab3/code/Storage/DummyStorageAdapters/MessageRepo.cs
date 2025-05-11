using Domain;
using Domain.OutPorts;
using Domain.Models;
using Storage.Models;

namespace Storage.StorageAdapters;

public class DummyMessageRepo : IMessageRepo
{
    //Моделирует таблицу DBMessage
    private List<DBMessage> Messages = new();
    //Моделирует таблицу связку между сообщениями и пользователями
    private List<Tuple<string, Guid>> UserMessage = new();

    public bool TryCreateMessage(Message message, List<string> users)
    {
        DBMessage dbm = new DBMessage(message.Id, message.Text, message.TimeSent.ToUniversalTime());
        Messages.Add(dbm);
        foreach (var user in users)
        {
            UserMessage.Add(new Tuple<string, Guid>(user, message.Id));
        }
        return true;
    }
    public bool TryNotify(List<Tuple<string, string>> users_messages)
    {
        //List<Tuple<string, string>> users_habits = GetUsersToNotify();
        foreach (var user in users_messages)
        {
            var g = Guid.NewGuid();
            DBMessage dbm = new DBMessage(g, user.Item2, DateTime.Now.ToUniversalTime());
            UserMessage.Add(new Tuple<string, Guid>(user.Item1, g));
        }
        return true;
    }
    public List<UserHabitInfo> GetUsersToNotify()
    {
        return [];
    }
}
