namespace MessageSenderDomain.Models;

public class Subscriber(long chatId, string taskTrackerLogin, string password, string username, DateTime subscriptionDate)
{
    public long Id { get; set; } = chatId;
    public string TaskTrackerLogin { get; set; } = taskTrackerLogin;
    public string Password { get; set; } = password;
    public string Username { get; set; } = username;
    public DateTime SubscriptionDate { get; set; } = subscriptionDate;
}
