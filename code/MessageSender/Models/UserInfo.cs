namespace MessageSenderDomain.Models;

public class UserInfo(string userName, string password)
{
    public string UserName { get; } = userName;
    public string Password { get; } = password;
}
