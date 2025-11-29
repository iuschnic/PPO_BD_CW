using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using Domain.OutPorts;

namespace MessageSenderClient;

public class MessageSenderHttpClientArgs(string baseUrl)
{
    public string BaseUrl = baseUrl;
}

public class MessageSenderHttpClient: IMessageSenderClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public MessageSenderHttpClient(MessageSenderHttpClientArgs args)
    {
        _baseUrl = args.BaseUrl.TrimEnd('/');
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<bool> CheckUserExistsAsync(string taskTrackerLogin)
    {
        if (string.IsNullOrEmpty(taskTrackerLogin))
            throw new ArgumentException("Task tracker login cannot be null or empty", nameof(taskTrackerLogin));

        try
        {
            var url = $"{_baseUrl}/check_exists?login={Uri.EscapeDataString(taskTrackerLogin)}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return bool.Parse(content);
            }

            throw new HttpRequestException($"Request failed with status code: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error checking user existence for login '{taskTrackerLogin}': {ex.Message}", ex);
        }
    }

    public async Task DeleteUserAccountAsync(string taskTrackerLogin)
    {
        if (string.IsNullOrEmpty(taskTrackerLogin))
            throw new ArgumentException("Task tracker login cannot be null or empty", nameof(taskTrackerLogin));

        try
        {
            var url = $"{_baseUrl}/delete_account";
            var content = new StringContent(taskTrackerLogin, Encoding.UTF8, "text/plain");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Request failed with status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting user account for login '{taskTrackerLogin}': {ex.Message}", ex);
        }
    }

    public async Task SendTwoFactorMessageAsync(string taskTrackerLogin, string message)
    {
        if (string.IsNullOrEmpty(taskTrackerLogin))
            throw new ArgumentException("Task tracker login cannot be null or empty", nameof(taskTrackerLogin));

        if (string.IsNullOrEmpty(message))
            throw new ArgumentException("Message cannot be null or empty", nameof(message));

        try
        {
            var url = $"{_baseUrl}/send_two_factor";
            var requestData = new
            {
                login = taskTrackerLogin,
                message = message
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Request failed with status code: {response.StatusCode}. Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error sending two-factor message to user '{taskTrackerLogin}': {ex.Message}", ex);
        }
    }
}