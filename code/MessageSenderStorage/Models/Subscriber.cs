using MessageSenderDomain.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MessageSenderStorage.Models;

[Table("subscribers")]
public class DBSubscriber
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    [Column("tasktracker_login")]
    public string TaskTrackerLogin { get; set; }
    [Column("password")]
    public string Password { get; set; }
    [Column("username")]
    public string Username { get; set; }
    [Column("subscription_date")]
    public DateTime SubscriptionDate { get; set; }
    public DBSubscriber(long chatId, string taskTrackerLogin, string password,
        string username, DateTime subscriptionDate)
    {
        Id = chatId;
        TaskTrackerLogin = taskTrackerLogin;
        Password = password;
        Username = username;
        SubscriptionDate = subscriptionDate;
    }
    public DBSubscriber(Subscriber subscriber)
    {
        Id = subscriber.ChatId;
        TaskTrackerLogin = subscriber.TaskTrackerLogin;
        Password = subscriber.Password;
        Username = subscriber.Username;
        SubscriptionDate = subscriber.SubscriptionDate;
    }
    public Subscriber ToModel()
    {
        return new Subscriber(Id, TaskTrackerLogin, Password, Username, SubscriptionDate);
    }
}
