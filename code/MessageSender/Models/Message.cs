namespace MessageSenderDomain.Models;

public class Message
{
    public Guid Id { get; }
    public string Text { get; }
    public DateTime? TimeSent { get; }
    public DateTime TimeOutdated { get; }
    public bool WasSent { get; }
    public long SubscriberID { get; }
    public string TaskTrackerUserID { get; }
    public Message(Guid id, string text, DateTime? timeSent, DateTime timeOutdated, bool wasSent, long subscriberID)
    {
        Id = id;
        Text = text;
        TimeSent = timeSent;
        TimeOutdated = timeOutdated;
        WasSent = wasSent;
        SubscriberID = subscriberID;
    }
}