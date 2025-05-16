namespace Storage.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("messages")]
public class DBMessage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("data")]
    public string Text { get; set; }
    [Column("time_sent")]
    public DateTime? TimeSent { get; set; }
    [Column("time_outdated")]
    public DateTime TimeOutdated { get; set; }
    public DBMessage(Guid id, string text, DateTime? timeSent, DateTime timeOutdated)
    {
        Id = id;
        Text = text;
        TimeSent = timeSent;
        TimeOutdated = timeOutdated;
    }
}

[Table("user_message")]
public class DBUserMessage
{
    [Column("user_name")]
    public string? DBUserID { get; set; }
    [Column("message_id")]
    public Guid DBMessageID { get; set; }
    [Column("was_sent")]
    public bool WasSent { get; set; }
    public DBUser? DBUser { get; set; }
    public DBMessage? DBMessage { get; set; }
    public DBUserMessage(string dBUserID, Guid dBMessageID, bool wasSent) 
    {
        DBMessageID = dBMessageID;
        DBUserID = dBUserID;
        WasSent = wasSent;
    }
}
