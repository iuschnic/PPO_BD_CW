using Domain.Models;
using Types;

namespace TaskTrackerDtoModels;

public class RegisterRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class PhoneNumberDto
{
    public string StringNumber { get; set; } = string.Empty;
}

public class UserDto
{
    public string NameID { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public PhoneNumberDto Number { get; set; } = new();
    public List<HabitDto> Habits { get; set; } = new();
    public List<EventDto> Events { get; set; } = new();
    public UserSettingsDto Settings { get; set; } = new();
}

public class HabitDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinsToComplete { get; set; }
    public List<ActualTimeDto> ActualTimings { get; set; } = new();
    public List<PrefFixedTimeDto> PrefFixedTimings { get; set; } = new();
    public TimeOption Option { get; set; }
    public string UserNameID { get; set; } = string.Empty;
    public int CountInWeek { get; set; }
}

public class HabitDataDto
{
    public string Name { get; set; } = string.Empty;
    public int MinsToComplete { get; set; }
    public List<PrefFixedTimeDataDto> PrefFixedTimings { get; set; } = new();
    public TimeOption Option { get; set; }
    public string UserNameID { get; set; } = string.Empty;
    public int CountInWeek { get; set; }
}

public class ActualTimeDto
{
    public Guid Id { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public DayOfWeek Day { get; set; }
    public Guid HabitID { get; set; }
}

public class PrefFixedTimeDto
{
    public Guid Id { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public Guid HabitID { get; set; }
}

public class PrefFixedTimeDataDto
{
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
}

public class EventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public DayOfWeek? Day { get; set; }
    public string UserNameID { get; set; } = string.Empty;
    public EventOption Option { get; set; }
    public DateOnly? EDate { get; set; }
}

public class EventDataDto
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public DayOfWeek? Day { get; set; }
    public string UserNameID { get; set; } = string.Empty;
    public EventOption Option { get; set; }
    public DateOnly? EDate { get; set; }
}

public class UserSettingsDto
{
    public Guid Id { get; set; }
    public bool NotifyOn { get; set; }
    public List<SettingsTimeDto> SettingsTimes { get; set; } = new();
    public string UserNameID { get; set; } = string.Empty;
}

public class SettingsTimeDto
{
    public Guid Id { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public Guid SettingsID { get; set; }
}

public class SettingsTimeDataDto
{
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
}

public class NotificationTimingDto
{
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
}

public class NotificationSettingsDto
{
    public List<NotificationTimingDto>? NewTimings { get; set; }
    public bool? NotifyOn { get; set; }
}

public class DistributionResultDto
{
    public UserDto User { get; set; } = new();
    public List<HabitDto> NonDistributedHabits { get; set; } = new();
}

public class UserHabitInfoDto
{
    public string UserName { get; set; } = string.Empty;
    public string HabitName { get; set; } = string.Empty;
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
}

public class ErrorResponseDto
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Mapper classes
public static class DtoMapper
{
    // PhoneNumber mappings
    public static PhoneNumberDto MapToDto(PhoneNumber domain)
    {
        return new PhoneNumberDto { StringNumber = domain.StringNumber };
    }

    public static PhoneNumber MapToDomain(PhoneNumberDto dto)
    {
        return new PhoneNumber(dto.StringNumber);
    }

    public static PhoneNumber MapToDomain(string phoneNumber)
    {
        return new PhoneNumber(phoneNumber);
    }

    // Habit mappings
    public static HabitDto MapToDto(Habit domain)
    {
        return new HabitDto
        {
            Id = domain.Id,
            Name = domain.Name,
            MinsToComplete = domain.MinsToComplete,
            ActualTimings = domain.ActualTimings.Select(MapToDto).ToList(),
            PrefFixedTimings = domain.PrefFixedTimings.Select(MapToDto).ToList(),
            Option = domain.Option,
            UserNameID = domain.UserNameID,
            CountInWeek = domain.CountInWeek
        };
    }

    public static Habit MapToDomain(HabitDataDto dto, Guid id, List<ActualTime> actualTimings, List<PrefFixedTime> prefFixedTimings)
    {
        return new Habit(
            id: id,
            name: dto.Name,
            mins_to_complete: dto.MinsToComplete,
            option: dto.Option,
            user_name: dto.UserNameID,
            actual_timings: actualTimings,
            pref_fixed_timings: prefFixedTimings,
            countInWeek: dto.CountInWeek
        );
    }
    public static Habit MapToDomain(HabitDto dto)
    {
        return new Habit(
            id: dto.Id,
            name: dto.Name,
            mins_to_complete: dto.MinsToComplete,
            option: dto.Option,
            user_name: dto.UserNameID,
            actual_timings: dto.ActualTimings.Select(MapToDomain).ToList(),
            pref_fixed_timings: dto.PrefFixedTimings.Select(MapToDomain).ToList(),
            countInWeek: dto.CountInWeek
        );
    }
    public static List<Habit> MapToDomain(List<HabitDto> dtos)
    {
        return dtos.Select(MapToDomain).ToList();
    }

    // ActualTime mappings
    public static ActualTimeDto MapToDto(ActualTime domain)
    {
        return new ActualTimeDto
        {
            Id = domain.Id,
            Start = domain.Start.ToTimeSpan(),
            End = domain.End.ToTimeSpan(),
            Day = domain.Day,
            HabitID = domain.HabitID
        };
    }

    public static ActualTime MapToDomain(ActualTimeDto dto)
    {
        return new ActualTime(
            id: dto.Id,
            start: TimeOnly.FromTimeSpan(dto.Start),
            end: TimeOnly.FromTimeSpan(dto.End),
            week_day: dto.Day,
            habitID: dto.HabitID
        );
    }

    // PrefFixedTime mappings
    public static PrefFixedTimeDto MapToDto(PrefFixedTime domain)
    {
        return new PrefFixedTimeDto
        {
            Id = domain.Id,
            Start = domain.Start.ToTimeSpan(),
            End = domain.End.ToTimeSpan(),
            HabitID = domain.HabitID
        };
    }

    public static PrefFixedTime MapToDomain(PrefFixedTimeDataDto dto, Guid id, Guid habitId)
    {
        return new PrefFixedTime(
            id: id,
            start: TimeOnly.FromTimeSpan(dto.Start),
            end: TimeOnly.FromTimeSpan(dto.End),
            habit_id: habitId
        );
    }

    public static PrefFixedTime MapToDomain(PrefFixedTimeDto dto)
    {
        return new PrefFixedTime(
            id: dto.Id,
            start: TimeOnly.FromTimeSpan(dto.Start),
            end: TimeOnly.FromTimeSpan(dto.End),
            habit_id: dto.HabitID
        );
    }

    // Event mappings
    public static EventDto MapToDto(Event domain)
    {
        return new EventDto
        {
            Id = domain.Id,
            Name = domain.Name,
            Start = domain.Start.ToTimeSpan(),
            End = domain.End.ToTimeSpan(),
            Day = domain.Day,
            UserNameID = domain.UserNameID,
            Option = domain.Option,
            EDate = domain.EDate
        };
    }

    public static Event MapToDomain(EventDataDto dto, Guid id)
    {
        return new Event(
            id: id,
            name: dto.Name,
            start: TimeOnly.FromTimeSpan(dto.Start),
            end: TimeOnly.FromTimeSpan(dto.End),
            userNameID: dto.UserNameID,
            option: dto.Option,
            day: dto.Day,
            eDate: dto.EDate
        );
    }
    public static Event MapToDomain(EventDto dto)
    {
        return new Event(
            id: dto.Id,
            name: dto.Name,
            start: TimeOnly.FromTimeSpan(dto.Start),
            end: TimeOnly.FromTimeSpan(dto.End),
            userNameID: dto.UserNameID,
            option: dto.Option,
            day: dto.Day,
            eDate: dto.EDate
        );
    }
    public static List<Event> MapToDomain(List<EventDto> dtos)
    {
        return dtos.Select(MapToDomain).ToList();
    }

    // UserSettings mappings
    public static UserSettingsDto MapToDto(UserSettings domain)
    {
        return new UserSettingsDto
        {
            Id = domain.Id,
            NotifyOn = domain.NotifyOn,
            SettingsTimes = domain.SettingsTimes.Select(MapToDto).ToList(),
            UserNameID = domain.UserNameID
        };
    }

    public static UserSettings MapToDomain(UserSettingsDto dto)
    {
        return new UserSettings(
            id: dto.Id,
            notify_on: dto.NotifyOn,
            user_name: dto.UserNameID,
            settings_times: dto.SettingsTimes.Select(MapToDomain).ToList()
        );
    }

    // SettingsTime mappings
    public static SettingsTimeDto MapToDto(SettingsTime domain)
    {
        return new SettingsTimeDto
        {
            Id = domain.Id,
            Start = domain.Start.ToTimeSpan(),
            End = domain.End.ToTimeSpan(),
            SettingsID = domain.SettingsID
        };
    }

    public static SettingsTime MapToDomain(SettingsTimeDto dto)
    {
        return new SettingsTime(
            id: dto.Id,
            start: TimeOnly.FromTimeSpan(dto.Start),
            end: TimeOnly.FromTimeSpan(dto.End),
            settings_id: dto.SettingsID
        );
    }

    public static SettingsTime MapToDomain(SettingsTimeDataDto dto, Guid id, Guid settingsId)
    {
        return new SettingsTime(
            id: id,
            start: TimeOnly.FromTimeSpan(dto.Start),
            end: TimeOnly.FromTimeSpan(dto.End),
            settings_id: settingsId
        );
    }

    // User mappings
    public static UserDto MapToDto(User domain)
    {
        return new UserDto
        {
            NameID = domain.NameID,
            PasswordHash = domain.PasswordHash,
            Number = MapToDto(domain.Number),
            Habits = domain.Habits?.Select(MapToDto).ToList() ?? new List<HabitDto>(),
            Events = domain.Events?.Select(MapToDto).ToList() ?? new List<EventDto>(),
            Settings = MapToDto(domain.Settings)
        };
    }
    public static User MapToDomain(UserDto dto)
    {
        return new User(dto.NameID, dto.PasswordHash, MapToDomain(dto.Number), MapToDomain(dto.Settings),
            MapToDomain(dto.Habits), MapToDomain(dto.Events));
    }

    // Notification timing mappings
    public static List<Tuple<TimeOnly, TimeOnly>> MapToTimeTuples(List<NotificationTimingDto> dtos)
    {
        return dtos.Select(d => new Tuple<TimeOnly, TimeOnly>(
            TimeOnly.FromTimeSpan(d.Start),
            TimeOnly.FromTimeSpan(d.End)
        )).ToList();
    }

    public static List<NotificationTimingDto> MapFromTimeTuples(List<Tuple<TimeOnly, TimeOnly>> tuples)
    {
        return tuples.Select(t => new NotificationTimingDto
        {
            Start = t.Item1.ToTimeSpan(),
            End = t.Item2.ToTimeSpan()
        }).ToList();
    }

    // UserHabitInfo mappings
    public static UserHabitInfoDto MapToDto(UserHabitInfo domain)
    {
        return new UserHabitInfoDto
        {
            UserName = domain.UserName,
            HabitName = domain.HabitName,
            Start = domain.Start.ToTimeSpan(),
            End = domain.End.ToTimeSpan()
        };
    }

    public static UserHabitInfo MapToDomain(UserHabitInfoDto dto)
    {
        return new UserHabitInfo(
            user_name: dto.UserName,
            habit_name: dto.HabitName,
            start: TimeOnly.FromTimeSpan(dto.Start),
            end: TimeOnly.FromTimeSpan(dto.End)
        );
    }

    // DistributionResult mappings
    public static DistributionResultDto MapToDto((User user, List<Habit> nonDistributedHabits) result)
    {
        return new DistributionResultDto
        {
            User = MapToDto(result.user),
            NonDistributedHabits = result.nonDistributedHabits.Select(MapToDto).ToList()
        };
    }
}

// Extension methods for conversion
public static class TimeExtensions
{
    public static TimeSpan ToTimeSpan(this TimeOnly timeOnly)
    {
        return new TimeSpan(timeOnly.Hour, timeOnly.Minute, timeOnly.Second);
    }

    public static TimeOnly ToTimeOnly(this TimeSpan timeSpan)
    {
        return new TimeOnly(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
    }
}
