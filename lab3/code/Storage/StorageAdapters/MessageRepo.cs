using Domain;
using Domain.OutPorts;
using Domain.Models;
using Storage.Models;

namespace Storage.StorageAdapters;

public class MessageRepo : IMessageRepo
{
    //Моделирует таблицу DBMessage
    private List<DBMessage> Messages = new();
    //Моделирует таблицу связку между сообщениями и пользователями
    private List<Tuple<Guid, Guid>> UserMessage = new();
    public void Create(Message message, List<Guid> users)
    {
        DBMessage dbm = new()
        {
            Id = message.Id,
            Text = message.Text,
            DateSent = message.DateSent
        };
        Messages.Add(dbm);
        foreach (var user in users)
        {
            UserMessage.Add(new Tuple<Guid, Guid>(user, message.Id));
        }
        return;
    }

    public void Save()
    {
        return;
    }
}
