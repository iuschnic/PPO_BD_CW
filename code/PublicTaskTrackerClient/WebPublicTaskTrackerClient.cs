using Domain.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskTrackerDtoModels;
using Types;
namespace MessageSenderTaskTrackerClient;

public interface IPublicTaskTrackerClient
{
    Task<User> CreateUserAsync(string username, PhoneNumber phone_number, string password);
    Task<User> LogInAsync(string username, string password);
    Task<Tuple<User, List<Habit>>> ImportNewScheduleAsync(string user_name, Stream stream, string extension);
    Task<Tuple<User, List<Habit>>> AddHabitAsync(Habit habit);
    Task<Tuple<User, List<Habit>>> DeleteHabitAsync(string user_name, string name);
    Task<Tuple<User, List<Habit>>> DeleteHabitsAsync(string name);
    Task<User> ChangeSettingsAsync(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string user_name);
    Task DeleteUserAsync(string username);
}

public class WebPublicTaskTrackerClient : IPublicTaskTrackerClient
{
    private readonly HttpClient _httpClient;

    public WebPublicTaskTrackerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<User> CreateUserAsync(string username, PhoneNumber phone_number, string password)
    {
        var request = new RegisterRequestDto
        {
            UserName = username,
            PhoneNumber = phone_number.StringNumber,
            Password = password
        };

        var jsonContent = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/v1/auth/register", content);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Ошибка создания пользорвателя");

        var userDto = await response.Content.ReadFromJsonAsync<UserDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (userDto == null)
            throw new Exception("Ошибка создания пользорвателя");
        return DtoMapper.MapToDomain(userDto);
    }

    public async Task<User> LogInAsync(string username, string password)
    {
        var request = new LoginRequestDto
        {
            UserName = username,
            Password = password
        };

        var jsonContent = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.PostAsync("/api/v1/auth/login", content, cts.Token);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Ошибка входа");

            var userDto = await response.Content.ReadFromJsonAsync<UserDto>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (userDto == null)
                throw new Exception("Ошибка входа");
            return DtoMapper.MapToDomain(userDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    public async Task<Tuple<User, List<Habit>>> AddHabitAsync(Habit habit)
    {
        var habitDataDto = DtoMapper.MapToDto(habit);

        var jsonContent = JsonSerializer.Serialize(habitDataDto);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/v1/users/{habit.UserNameID}/habits", content);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Ошибка создания привычки");

        var resultDto = await response.Content.ReadFromJsonAsync<DistributionResultDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (resultDto == null)
            throw new Exception("Ошибка создания привычки");

        var user = DtoMapper.MapToDomain(resultDto.User);

        var habits = resultDto.NonDistributedHabits.Select(DtoMapper.MapToDomain).ToList();

        return new Tuple<User, List<Habit>>(user, habits);
    }

    public async Task<Tuple<User, List<Habit>>> DeleteHabitAsync(string user_name, string name)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/users/{user_name}/habits/{name}");

        if (!response.IsSuccessStatusCode)
            throw new Exception("Ошибка удаления привычки");

        var resultDto = await response.Content.ReadFromJsonAsync<DistributionResultDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (resultDto == null)
            throw new Exception("Ошибка удаления привычки");

        var user = DtoMapper.MapToDomain(resultDto.User);
        var habits = resultDto.NonDistributedHabits.Select(DtoMapper.MapToDomain).ToList();

        return new Tuple<User, List<Habit>>(user, habits);
    }

    public async Task<Tuple<User, List<Habit>>> DeleteHabitsAsync(string user_name)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/users/{user_name}/habits");

        if (!response.IsSuccessStatusCode)
            throw new Exception("Ошибка удаления привычек");

        var resultDto = await response.Content.ReadFromJsonAsync<DistributionResultDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (resultDto == null)
            throw new Exception("Ошибка удаления привычек");

        var user = DtoMapper.MapToDomain(resultDto.User);
        var habits = resultDto.NonDistributedHabits.Select(DtoMapper.MapToDomain).ToList();

        return new Tuple<User, List<Habit>>(user, habits);
    }

    public async Task<User> ChangeSettingsAsync(List<Tuple<TimeOnly, TimeOnly>>? newTimings,
        bool? notifyOn, string username)
    { 
        var notificationSettings = new NotificationSettingsDto
        {
            NewTimings = newTimings?.Select(t => new NotificationTimingDto
            {
                Start = t.Item1.ToTimeSpan(),
                End = t.Item2.ToTimeSpan()
            }).ToList(),
            NotifyOn = notifyOn
        };

        var jsonContent = JsonSerializer.Serialize(notificationSettings);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PatchAsync($"/api/v1/users/{username}/notifications/change_settings", content);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Ошибка обновления настроек");

        var userDto = await response.Content.ReadFromJsonAsync<UserDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (userDto == null)
            throw new Exception("Ошибка обновления настроек");

        return DtoMapper.MapToDomain(userDto);
    }

    public async Task<Tuple<User, List<Habit>>> ImportNewScheduleAsync(string user_name, Stream stream, string extension)
    {
        var formData = new MultipartFormDataContent
        {
            { new StreamContent(stream), "file", $"schedule{extension}" }
        };

        var response = await _httpClient.PostAsync($"/api/v1/users/{user_name}/schedule/import", formData);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Ошибка импорта расписания");

        var resultDto = await response.Content.ReadFromJsonAsync<DistributionResultDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (resultDto == null)
            throw new Exception("Ошибка импорта расписания");

        var user = DtoMapper.MapToDomain(resultDto.User);
        var habits = resultDto.NonDistributedHabits.Select(DtoMapper.MapToDomain).ToList();

        return new Tuple<User, List<Habit>>(user, habits);
    }

    public async Task DeleteUserAsync(string username)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/users/{username}");
        if (!response.IsSuccessStatusCode)
            throw new Exception("Ошибка удаления пользователя");
    }
}

