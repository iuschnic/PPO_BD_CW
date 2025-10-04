namespace MessageSenderDomain.Models;

public class Message
{
    public Guid Id { get; }
    public string Text { get; }
    public DateTime? TimeSent { get; }
    public DateTime TimeOutdated { get; }
    public bool WasSent { get; }
    public string UserNameID { get; }
    public Message(Guid id, string text, DateTime? timeSent, DateTime timeOutdated, bool wasSent, string userNameID)
    {
        Id = id;
        Text = text;
        TimeSent = timeSent;
        TimeOutdated = timeOutdated;
        WasSent = wasSent;
        UserNameID = userNameID;
    }
}