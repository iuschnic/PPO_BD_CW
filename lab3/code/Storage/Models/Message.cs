namespace Storage.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("message")]
public class DBMessage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("data")]
    public string Text { get; set; }
    [Column("date_sent")]
    public DateOnly DateSent { get; set; }
    public DBMessage(Guid id, string text, DateOnly dateSent)
    {
        Id = id;
        Text = text;
        DateSent = dateSent;
    }
}

[Table("user_message")]
public class DBUserMessage
{
    [Column("user_name")]
    public string? DBUserID { get; set; }
    [Column("message_id")]
    public Guid DBMessageID { get; set; }
    public DBUserMessage(string dBUserID, Guid dBMessageID) 
    {
        DBMessageID = dBMessageID;
        DBUserID = dBUserID;
    }
}
