using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Table("Messages")]
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

[Table("User_Message")]
public class DBUserMessage
{
    [Column("user_name")]
    public string? DBUserNameID { get; set; }
    [Column("message_id")]
    public Guid? DBMessageID { get; set; }
    public DBUserMessage(string dbusernameid, Guid dbmessageid) 
    {
        DBMessageID = dbmessageid;
        DBUserNameID = dbusernameid;
    }
}
