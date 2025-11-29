namespace Domain.OutPorts;

public interface IMessageSenderClient
{
    Task<bool> CheckUserExistsAsync(string taskTrackerLogin);
    Task DeleteUserAccountAsync(string taskTrackerLogin);
    Task SendTwoFactorMessageAsync(string taskTrackerLogin, string message);
}
