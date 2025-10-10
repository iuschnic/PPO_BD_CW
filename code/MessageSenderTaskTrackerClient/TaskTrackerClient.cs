using MessageSenderDomain.OutPorts;
using MessageSenderDomain.Models;
using System.Text;
using System.Text.Json;
namespace MessageSenderTaskTrackerClient;

public class TaskTrackerClient : ITaskTrackerClient
{
    private readonly HttpClient _httpClient;

    public TaskTrackerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<UserHabitInfo>?> GetUsersToNotifyAsync()
    {
        var response = await _httpClient.GetAsync("/api/v1/internal/get-users-to-notify");

        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        var userHabitInfoDtos = JsonSerializer.Deserialize<List<UserHabitInfoDto>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return userHabitInfoDtos?.Select(MapToDomain).ToList() ?? new List<UserHabitInfo>();
    }

    public async Task<bool> TryLogInAsync(string taskTrackerLogin, string password)
    {
        var loginRequest = new LoginRequestDto
        {
            UserName = taskTrackerLogin,
            Password = password
        };

        var jsonContent = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync("/api/v1/internal/check-log-in", content);

        var isValid = response.IsSuccessStatusCode;

        return isValid;
    }

    private UserHabitInfo MapToDomain(UserHabitInfoDto dto)
    {
        return new UserHabitInfo(
            dto.UserName,
            dto.HabitName,
            TimeOnly.FromTimeSpan(dto.Start),
            TimeOnly.FromTimeSpan(dto.End)
        );
    }
}

public class UserHabitInfoDto
{
    public string UserName { get; set; } = string.Empty;
    public string HabitName { get; set; } = string.Empty;
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
}

public class LoginRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
