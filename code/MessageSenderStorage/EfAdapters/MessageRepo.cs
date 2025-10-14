using MessageSenderDomain.Models;
using MessageSenderDomain.OutPorts;
using MessageSenderStorage.Models;
namespace MessageSenderStorage.EfAdapters;

public class EfMessageRepo(MessageSenderDBContext dbContext) : IMessageRepo
{
    public class HabitDueSoonResult
    {
        public string UserName { get; set; }
        public string HabitName { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
    private MessageSenderDBContext _dbContext { get; } = dbContext;
    /*private List<HabitDueSoonResult> GetHabitsDueSoon()
    {
        var currentTime = TimeOnly.FromDateTime(DateTime.Now);
        var currentDayOfWeek = DateTime.Now.DayOfWeek;
        var timePlus30Min = currentTime.AddMinutes(30);
        var crossesMidnight = timePlus30Min < currentTime;

        var habits = _dbContext.ActualTimes
            .Include(at => at.DBHabit)
            .ThenInclude(h => h.DBUser)
            .Include(at => at.DBHabit)
            .ThenInclude(h => h.DBUser.Settings)
            .ThenInclude(s => s.ForbiddenTimings)
            .Where(at =>
                (at.Day == currentDayOfWeek ||
                 (crossesMidnight && at.Day == GetTomorrowDayOfWeek(currentDayOfWeek))) &&
                IsTimeInRange(at.Start, currentTime, timePlus30Min, crossesMidnight) &&
                !IsInForbiddenTime(at.DBHabit.DBUser, currentTime))
            .Select(at => new HabitDueSoonResult
            {
                UserName = at.DBHabit.DBUser.NameID,
                HabitName = at.DBHabit.Name,
                StartTime = at.Start,
                EndTime = at.End
            })
            .ToList();

        return habits;
    }

    private bool IsTimeInRange(TimeOnly time, TimeOnly currentTime, TimeOnly timePlus30Min, bool crossesMidnight)
    {
        if (!crossesMidnight)
            return time >= currentTime && time <= timePlus30Min;
        else
            return time >= currentTime || time <= timePlus30Min;
    }

    private bool IsInForbiddenTime(DBUser user, TimeOnly currentTime)
    {
        return user.Settings?.ForbiddenTimings?
            .Any(ft => currentTime >= ft.Start && currentTime <= ft.End) ?? false;
    }
    private DayOfWeek GetTomorrowDayOfWeek(DayOfWeek today)
    {
        return today == DayOfWeek.Saturday ? DayOfWeek.Sunday : today + 1;
    }*/

    /*public bool TryCreateMessages(List<Message> users_messages)
    {
        lock (_locker)
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
    }*/

    /*public bool TryCreateMessages(List<Message> messages)
    {
        lock (_locker)
        {
            bool ret = true;
            var subs = _dbContext.Subscribers.ToList();
            if (subs == null)
                return false;
            List<DBSubscriberMessage> dbSubsMessages = [];
            List<DBMessage> dbMessages = [];
            foreach (var message in messages)
            {
                var sub = subs.Find(s => s.TaskTrackerLogin == message.TaskTrackerLogin);
                if (sub == null)
                    ret = false;
                else
                {
                    var g = Guid.NewGuid();
                    var dbm = new DBMessage(g, message.TaskTrackerLogin);
                    dbSubsMessages.Add(new DBSubscriberMessage(sub.Id, g,
                        false, message.TimeOutdated, null));
                    dbMessages.Add(dbm);
                }
            }
            _dbContext.Messages.AddRange(dbMessages);
            _dbContext.SubscriberMessage.AddRange(dbSubsMessages);
            _dbContext.SaveChanges();
            return ret;
        }
    }*/

    public bool TryCreateMessages(List<Message> messages)
    {
        if (messages.Count == 0)
            return true;

        var taskTrackerLogins = messages.Select(m => m.TaskTrackerLogin).Distinct().ToList();
        var subs = _dbContext.Subscribers
            .Where(s => taskTrackerLogins.Contains(s.TaskTrackerLogin))
            .ToDictionary(s => s.TaskTrackerLogin);
        if (subs.Count == 0)
            return false;
        var dbMessages = new List<DBMessage>();
        var dbSubsMessages = new List<DBSubscriberMessage>();
        bool allSubscribersFound = true;
        foreach (var message in messages)
        {
            if (!subs.TryGetValue(message.TaskTrackerLogin, out var sub))
            {
                allSubscribersFound = false;
                continue;
            }
            var messageId = Guid.NewGuid();
            dbMessages.Add(new DBMessage(messageId, message.TaskTrackerLogin));
            dbSubsMessages.Add(new DBSubscriberMessage(sub.Id, messageId, false, message.TimeOutdated, null));
        }
        if (dbMessages.Count > 0)
        {
            _dbContext.Messages.AddRange(dbMessages);
            _dbContext.SubscriberMessage.AddRange(dbSubsMessages);
            _dbContext.SaveChanges();
        }
        return allSubscribersFound;
    }

    public List<Message>? TryGetMessagesToSend()
    {
        var currentTime = DateTime.Now;

        return _dbContext.SubscriberMessage
            .Where(dbum => dbum.TimeOutdated > currentTime && !dbum.WasSent)
            .Select(dbum => new Message(
                dbum.MessageID,
                dbum.DBMessage.Text,
                null,
                dbum.TimeOutdated,
                dbum.WasSent,
                dbum.DBSubscriber.TaskTrackerLogin,
                dbum.SubscriberID
            ))
            .ToList();
    }

    /*public List<Message>? TryGetMessagesToSend()
    {
        lock (_locker)
        {
            var messages = _dbContext.SubscriberMessage.Where(dbum => dbum.TimeOutdated > DateTime.Now && dbum.WasSent == false)
                .Include(dbum => dbum.DBMessage)
                .Include(dbum => dbum.DBSubscriber)
                .ToList();
            if (messages == null)
                return null;
            List<Message> toSend = [];
            foreach (var message in messages)
                toSend.Add(new Message(message.MessageID, message.DBMessage.Text,
                    null, message.TimeOutdated, message.WasSent, message.DBSubscriber.TaskTrackerLogin, message.SubscriberID));
            return toSend;
        }
    }*/

    public bool MarkMessagesSent(List<Message> messages)
    {
        if (messages.Count == 0) return true;
        var messageIds = messages.Select(m => m.Id).ToList();
        var dbSubsMessages = _dbContext.SubscriberMessage
            .Where(sm => messageIds.Contains(sm.MessageID))
            .ToDictionary(sm => sm.MessageID, sm => sm);
        foreach (var message in messages)
        {
            if (dbSubsMessages.TryGetValue(message.Id, out var dbsm))
            {
                dbsm.WasSent = message.WasSent;
                dbsm.TimeSent = message.TimeSent;
            }
        }
        return _dbContext.SaveChanges() > 0;
    }

    /*public bool MarkMessagesSent(List<Message> messages)
    {
        lock (_locker)
        {
            var dbSubsMessages = _dbContext.SubscriberMessage.ToList();
            if (dbSubsMessages == null) return false;
            foreach (var message in messages)
            {
                var dbsm = dbSubsMessages.Find(d => d.MessageID == message.Id);
                if (dbsm != null)
                {
                    dbsm.WasSent = message.WasSent;
                    dbsm.TimeSent = message.TimeSent;
                }
            }
            _dbContext.SaveChanges();
            return true;
        }
    }*/

    /*public List<UserHabitInfo> GetUsersToNotify()
    {
        lock (_locker)
        {
            List<UserHabitInfo> users_habits = [];
            //var conn = _dbContext.Database.GetDbConnection();
            //var result = conn.Query("select * from get_habits_due_soon()");
            var result = GetHabitsDueSoon();
            foreach (var r in result)
                users_habits.Add(new UserHabitInfo(r.UserName, r.HabitName, TimeOnly.Parse(r.StartTime.ToString()), TimeOnly.Parse(r.EndTime.ToString())));
            return users_habits;
        }
    }*/
}