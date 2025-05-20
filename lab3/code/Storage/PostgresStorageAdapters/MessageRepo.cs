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
    public bool TryCreateMessage(Message message, List<string> users)
    {
        bool ret = true;
        var dbusers = _dbContext.Users.ToList();
        if (dbusers == null)
            return false;

        DBMessage dbm = new DBMessage(message.Id, message.Text, message.TimeSent.ToUniversalTime(), message.TimeOutdated);
        List<DBUserMessage> user_message = [];
        foreach (var user in users)
        {
            //сообщение рассылается всем пользователям, но если по какой то причине пользователя нет, возвращаем false
            if (!dbusers.Any(dbu => dbu.NameID == user))
                ret = false;
            else
                user_message.Add(new DBUserMessage(user, dbm.Id, false));
        }
        _dbContext.Messages.Add(dbm);
        _dbContext.UserMessages.AddRange(user_message);
        _dbContext.SaveChanges();
        return ret;
    }
    public bool TryCreateMessages(List<Tuple<string, string>> users_messages)
    {
        bool ret = true;
        var dbusers = _dbContext.Users.ToList();
        if (dbusers == null)
            return false;

        List<DBUserMessage> user_message = [];
        List<DBMessage> messages = [];
        foreach (var user in users_messages)
        {
            //сообщение рассылается всем пользователям, но если по какой то причине пользователя нет, возвращаем false
            if (!dbusers.Any(dbu => dbu.NameID == user.Item1))
                ret = false;
            else
            {
                var g = Guid.NewGuid();
                DBMessage dbm = new DBMessage(g, user.Item2, DateTime.Now, DateTime.Now);
                user_message.Add(new DBUserMessage(user.Item1, g, false));
                messages.Add(dbm);
            }
        }
        _dbContext.Messages.AddRange(messages);
        _dbContext.UserMessages.AddRange(user_message);
        try
        {
            _dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        
        return ret;
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
