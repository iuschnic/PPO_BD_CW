namespace MessageSenderDomain.Models;

public class Message(Guid id, string text, DateTime? timeSent,
    DateTime timeOutdated, bool wasSent, string taskTrackerLogin, long? subscriberID)
{
    public Guid Id { get; } = id;
    public string Text { get; } = text;
    public DateTime? TimeSent { get; } = timeSent;
    public DateTime TimeOutdated { get; } = timeOutdated;
    public bool WasSent { get; } = wasSent;
    public string TaskTrackerLogin { get; } = taskTrackerLogin;
    public long? SubscriberID { get; } = subscriberID;
}