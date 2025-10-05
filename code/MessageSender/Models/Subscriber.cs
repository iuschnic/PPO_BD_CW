namespace MessageSenderDomain.Models;

public class Subscriber
{
    public long Id { get; set; }
    public string TaskTrackerLogin { get; set; }
    public string Password { get; set; }
    public string Username { get; set; }
    public DateTime SubscriptionDate { get; set; }
    public Subscriber(long chatId, string taskTrackerLogin, string password, string username, DateTime subscriptionDate)
    {
        Id = chatId;
        TaskTrackerLogin = taskTrackerLogin;
        Password = password;
        Username = username;
        SubscriptionDate = subscriptionDate;
    }
}
