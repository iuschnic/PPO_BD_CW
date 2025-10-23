using System.Text;
using System.Text.Json;
using TaskTrackerDtoModels;
namespace MessageSenderTaskTrackerClient;

public class WebPublicTaskTrackerClient
{
    private readonly HttpClient _httpClient;

    public WebPublicTaskTrackerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async 

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

