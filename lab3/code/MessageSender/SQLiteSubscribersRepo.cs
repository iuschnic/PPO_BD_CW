using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace StorageSubscribers;

public class DBSubscriber
{
    public long DBChatId { get; set; }
    public string DBTaskTrackerLogin { get; set; }
    public string DBPassword { get; set; }
    public string DBUsername { get; set; }
    public DateTime DBSubscriptionDate { get; set; }
    public DBSubscriber(long dBChatId, string dBTaskTrackerLogin, string dBPassword, string dBUsername, DateTime dBSubscriptionDate)
    {
        DBChatId = dBChatId;
        DBTaskTrackerLogin = dBTaskTrackerLogin;
        DBPassword = dBPassword;
        DBUsername = dBUsername;
        DBSubscriptionDate = dBSubscriptionDate;
    }
    public DBSubscriber(Subscriber subscriber)
    {
        DBChatId = subscriber.ChatId;
        DBTaskTrackerLogin = subscriber.TaskTrackerLogin;
        DBPassword = subscriber.Password;
        DBUsername = subscriber.Username;
        DBSubscriptionDate = subscriber.SubscriptionDate;
    }
}

public class SubscribersDBContext : DbContext
{
    public DbSet<DBSubscriber> Subscribers { get; set; }

    public SubscribersDBContext(DbContextOptions<SubscribersDBContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DBSubscriber>().HasKey(u => u.DBChatId);
    }
}
public class SQLiteSubscribersRepo: ISubscribersRepo
{
    private SubscribersDBContext _dbContext { get; }
    public SQLiteSubscribersRepo(SubscribersDBContext dbContext)
    {
        _dbContext = dbContext;
    }
    public Subscriber? TryGetByChatID(long chat_id)
    {
        var dbsub = _dbContext.Subscribers.Find(chat_id);
        if (dbsub == null)
            return null;
        return new Subscriber(dbsub.DBChatId, dbsub.DBTaskTrackerLogin, dbsub.DBPassword, dbsub.DBUsername, dbsub.DBSubscriptionDate);
    }
    public Subscriber? TryGetByTaskTrackerLogin(string task_tracker_login)
    {
        var dbsub = _dbContext.Subscribers.FirstOrDefault(dbs => dbs.DBTaskTrackerLogin == task_tracker_login);
        if (dbsub == null)
            return null;
        return new Subscriber(dbsub.DBChatId, dbsub.DBTaskTrackerLogin, dbsub.DBPassword, dbsub.DBUsername, dbsub.DBSubscriptionDate);
    }
    public bool IfAnyChatID(long chat_id)
    {
        return _dbContext.Subscribers.Any(s => s.DBChatId == chat_id);
    }
    public bool TryAdd(Subscriber subscriber)
    {
        var dbsub = _dbContext.Subscribers.Find(subscriber.ChatId);
        if (dbsub != null)
            return false;
        var to_add = new DBSubscriber(subscriber);
        _dbContext.Subscribers.Add(to_add);
        _dbContext.SaveChanges();
        return true;
    }
    public bool TryRemoveByChatID(long chat_id)
    {
        var to_remove = _dbContext.Subscribers.Find(chat_id);
        if (to_remove == null) return false;
        _dbContext.Subscribers.Remove(to_remove);
        _dbContext.SaveChanges();
        return true;
    }
}
