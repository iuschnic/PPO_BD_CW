using MessageSenderDomain.Models;
using Storage.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace MessageSenderStorage.Models;

[Table("messages")]
public class DBMessage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("data")]
    public string Text { get; set; }
    public DBMessage(Guid id, string text)
    {
        Id = id;
        Text = text;
    }
}

[Table("subscriber_message")]
public class DBSubscriberMessage
{
    [Column("subscriber_id")]
    public long SubscriberID { get; set; }
    [Column("message_id")]
    public Guid MessageID { get; set; }
    [Column("was_sent")]
    public bool WasSent { get; set; }
    [Column("time_outdated")]
    public DateTime TimeOutdated { get; set; }
    [Column("time_sent")]
    public DateTime? TimeSent { get; set; }
    public DBSubscriber? DBSubscriber { get; set; }
    public DBMessage? DBMessage { get; set; }
    public DBSubscriberMessage(long subscriberID, Guid messageID, bool wasSent,
        DateTime timeOutdated, DateTime? timeSent)
    {
        MessageID = messageID;
        SubscriberID = subscriberID;
        WasSent = wasSent;
        TimeOutdated = timeOutdated;
        TimeSent = timeSent;
    }
}
