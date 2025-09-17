using Domain.OutPorts;
using Domain.Models;
using Storage.Models;
using Storage.EfAdapters;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Storage.StorageAdapters;

public class EfMessageRepo(ITaskTrackerContext dbContext) : IMessageRepo
{
    public class HabitDueSoonResult
    {
        public string UserName { get; set; }
        public string HabitName { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
    private ITaskTrackerContext _dbContext { get; } = dbContext;
    private object _locker = new object();
    private List<HabitDueSoonResult> GetHabitsDueSoon()
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
        {
            return time >= currentTime && time <= timePlus30Min;
        }
        else
        {
            return time >= currentTime || time <= timePlus30Min;
        }
    }

    private bool IsInForbiddenTime(DBUser user, TimeOnly currentTime)
    {
        return user.Settings?.ForbiddenTimings?
            .Any(ft => currentTime >= ft.Start && currentTime <= ft.End) ?? false;
    }
    private DayOfWeek GetTomorrowDayOfWeek(DayOfWeek today)
    {
        return today == DayOfWeek.Saturday ? DayOfWeek.Sunday : today + 1;
    }

    public bool TryCreateMessages(List<Message> users_messages)
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
    }

    public List<Message> GetMessagesToSend()
    {
        lock (_locker)
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
    }

    public bool MarkMessagesSent(List<Message> messages)
    {
        lock (_locker)
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
    }

    public List<UserHabitInfo> GetUsersToNotify()
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
    }
}