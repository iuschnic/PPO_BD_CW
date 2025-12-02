using Domain.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskTrackerDtoModels;
using Types;
namespace MessageSenderTaskTrackerClient;

public interface IPublicTaskTrackerClient
{
    Task<User> CreateUserAsync(string userName, PhoneNumber phoneNumber, string password);
    Task<User> LogInAsync(string userName, string password, string? twoFactorCode = null);
    Task ChangeTwoFactorAuthAsync(string userName, bool isEnabled);
    Task ChangePasswordAsync(string userName, string password, string newPassword, string? twoFactorCode = null);
    Task<Tuple<User, List<Habit>>> ImportNewScheduleAsync(string userName, Stream stream, string extension);
    Task<Tuple<User, List<Habit>>> AddHabitAsync(Habit habit);
    Task<Tuple<User, List<Habit>>> DeleteHabitAsync(string userName, string name);
    Task<Tuple<User, List<Habit>>> DeleteHabitsAsync(string name);
    Task<User> ChangeSettingsAsync(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string userName);
    Task DeleteUserAsync(string userName);
}

public class WebPublicTaskTrackerClient : IPublicTaskTrackerClient
{
    private readonly HttpClient _httpClient;

    public WebPublicTaskTrackerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<User> CreateUserAsync(string userName, PhoneNumber phoneNumber, string password)
    {
        var request = new RegisterRequestDto
        {
            UserName = userName,
            PhoneNumber = phoneNumber.StringNumber,
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

    public async Task<User> LogInAsync(string userName, string password, string? twoFactorCode = null)
    {
        var request = new LoginRequestDto
        {
            UserName = userName,
            Password = password,
            TwoFactorCode = twoFactorCode
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

    public async Task ChangeTwoFactorAuthAsync(string userName, bool isEnabled)
    {
        var request = new ChangeTwoFactorRequestDto
        {
            UserName = userName,
            IsEnabled = isEnabled
        };

        var jsonContent = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.PatchAsync("/api/v1/auth/two-factor", content, cts.Token);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Ошибка изменения состояния двухфакторной аутентификации");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
    public async Task ChangePasswordAsync(string userName, string password, 
        string newPassword, string? twoFactorCode = null)
    {
        var request = new ChangePasswordRequestDto
        {
            UserName = userName,
            Password = password,
            NewPassword = newPassword,
            TwoFactorCode = twoFactorCode
        };

        var jsonContent = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.PatchAsync("/api/v1/auth/change-password", content, cts.Token);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Ошибка изменения пароля");
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

    public async Task<Tuple<User, List<Habit>>> DeleteHabitAsync(string userName, string name)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/users/{userName}/habits/{name}");

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

    public async Task<Tuple<User, List<Habit>>> DeleteHabitsAsync(string userName)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/users/{userName}/habits");

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
        bool? notifyOn, string userName)
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

        var response = await _httpClient.PatchAsync($"/api/v1/users/{userName}/notifications/change-settings", content);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Ошибка обновления настроек");

        var userDto = await response.Content.ReadFromJsonAsync<UserDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (userDto == null)
            throw new Exception("Ошибка обновления настроек");

        return DtoMapper.MapToDomain(userDto);
    }

    public async Task<Tuple<User, List<Habit>>> ImportNewScheduleAsync(string userName, Stream stream, string extension)
    {
        var formData = new MultipartFormDataContent
        {
            { new StreamContent(stream), "file", $"schedule{extension}" }
        };

        var response = await _httpClient.PostAsync($"/api/v1/users/{userName}/schedule/import", formData);

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

    public async Task DeleteUserAsync(string userName)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/users/{userName}");
        if (!response.IsSuccessStatusCode)
            throw new Exception("Ошибка удаления пользователя");
    }
}

