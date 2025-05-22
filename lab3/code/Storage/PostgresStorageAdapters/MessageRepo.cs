using Domain.OutPorts;
using Domain.Models;
using Storage.Models;
using Storage.PostgresStorageAdapters;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Storage.StorageAdapters;

public class PostgresMessageRepo : IMessageRepo
{
    private PostgresDBContext _dbContext { get; }
    public PostgresMessageRepo(PostgresDBContext dbContext)
    {
        _dbContext = dbContext;
    }
    public bool TryCreateMessages(List<Message> users_messages)
    {
        bool ret = true;
        var dbusers = _dbContext.Users.ToList();
        if (dbusers == null)
            return false;

        List<DBUserMessage> dbuser_messages = [];
        List<DBMessage> dbmessages = [];
        foreach (var user_message in users_messages)
        {
            if (!dbusers.Any(dbu => dbu.NameID == user_message.UserNameID))
                ret = false;
            else
            {
                var g = Guid.NewGuid();
                DBMessage dbm = new DBMessage(user_message.Id, user_message.Text);
                dbuser_messages.Add(new DBUserMessage(user_message.UserNameID, user_message.Id, false, user_message.TimeOutdated, null));
                dbmessages.Add(dbm);
            }
        }
        _dbContext.Messages.AddRange(dbmessages);
        _dbContext.UserMessages.AddRange(dbuser_messages);
        _dbContext.SaveChanges();
        return ret;
    }

    public List<Message> GetMessagesToSend()
    {
        var messages = _dbContext.UserMessages.Where(dbum => dbum.TimeOutdated > DateTime.Now && dbum.WasSent == false)
            .Include(dbum => dbum.DBMessage)
            .Include(dbum => dbum.DBUser)
            .ToList();
        List<Message> to_send = [];
        foreach (var message in messages)
            to_send.Add(new Message(message.DBMessageID, message.DBMessage.Text, null, message.TimeOutdated, message.WasSent, message.DBUserID));
        return to_send;
    }

    public bool MarkMessagesSent(List<Message> messages)
    {
        var dbusermessages = _dbContext.UserMessages.ToList();
        if (dbusermessages == null) return false;
        foreach (var message in messages)
        {
            var dbum = dbusermessages.Find(d => d.DBMessageID == message.Id);
            if (dbum != null)
            {
                dbum.WasSent = message.WasSent;
                dbum.TimeSent = message.TimeSent;
            }
        }
        _dbContext.SaveChanges();
        return true;
    }

    public List<UserHabitInfo> GetUsersToNotify()
    {
        List<UserHabitInfo> users_habits = [];
        var conn = _dbContext.Database.GetDbConnection();
        var result = conn.Query("select * from get_habits_due_soon()");
        foreach (var r in result)
            users_habits.Add(new UserHabitInfo(r.user_name, r.habit_name, TimeOnly.Parse(r.start_time.ToString()), TimeOnly.Parse(r.end_time.ToString())));
        return users_habits;
    }
}
