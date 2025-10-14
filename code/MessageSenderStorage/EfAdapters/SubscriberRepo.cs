using MessageSenderDomain.Models;
using MessageSenderDomain.OutPorts;
using MessageSenderStorage.Models;
namespace MessageSenderStorage.EfAdapters;

public class EfSubscriberRepo(MessageSenderDBContext dbContext) : ISubscriberRepo
{
    private MessageSenderDBContext _dbContext { get; } = dbContext;
    public Subscriber? TryGetByChatID(long chatId)
    {
        var dbsub = _dbContext.Subscribers.Find(chatId);
        if (dbsub == null)
            return null;
        return dbsub.ToModel();
    }
    public Subscriber? TryGetByTaskTrackerLogin(string taskTrackerLogin)
    {
        var dbsub = _dbContext.Subscribers.FirstOrDefault(dbs => dbs.TaskTrackerLogin == taskTrackerLogin);
        if (dbsub == null)
            return null;
        return dbsub.ToModel();
    }
    public bool IfAnyChatID(long chatId)
    {
        return _dbContext.Subscribers.Any(s => s.Id == chatId);
    }
    public bool TryAdd(Subscriber subscriber)
    {
        var dbsub = _dbContext.Subscribers.Find(subscriber.Id);
        if (dbsub != null)
            return false;
        var toAdd = new DBSubscriber(subscriber);
        _dbContext.Subscribers.Add(toAdd);
        _dbContext.SaveChanges();
        return true;
    }
    public bool TryRemoveByChatID(long chatId)
    {
        var toRemove = _dbContext.Subscribers.Find(chatId);
        if (toRemove == null) return false;
        _dbContext.Subscribers.Remove(toRemove);
        _dbContext.SaveChanges();
        return true;
    }
}