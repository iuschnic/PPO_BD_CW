using Domain.Exceptions;
using Domain.InPorts;
using Domain.Models;
using Domain.OutPorts;
using Microsoft.Extensions.Logging;
using Types;

namespace Domain;

public class TaskTracker : ITaskTracker
{
    private readonly IEventRepo _eventRepo;
    private readonly IHabitRepo _habitRepo;
    private readonly IUserRepo _userRepo;
    private readonly ISheduleLoad _shedLoader;
    private readonly IHabitDistributor _distributer;
    private readonly ILogger<TaskTracker> _logger;

    public TaskTracker(IEventRepo eventRepo, IHabitRepo habitRepo,
        IUserRepo userRepo, ISheduleLoad shedLoader, IHabitDistributor distributer, ILogger<TaskTracker> logger)
    {
        _eventRepo = eventRepo;
        _habitRepo = habitRepo;
        _userRepo = userRepo;
        _shedLoader = shedLoader;
        _distributer = distributer;
        _logger = logger;
        _logger.LogInformation("TaskTracker был успешно инициализирован");
    }
    private async Task<User> GetUserAsync(string user_name)
    {
        var user = await _userRepo.TryFullGetAsync(user_name);
        if (user == null)
            throw new RepositoryOperationException("получения", "пользователя", user_name);
        var events = await _eventRepo.TryGetAsync(user_name);
        if (events == null)
            throw new EventsNotFoundException(user_name);
        user.Events.Clear();
        user.Events.AddRange(events);
        return user;
    }

    private User GetUser(string user_name)
    {
        var user = _userRepo.TryFullGet(user_name);
        if (user == null)
            throw new RepositoryOperationException("получения", "пользователя", user_name);
        var events = _eventRepo.TryGet(user_name);
        if (events == null)
            throw new EventsNotFoundException(user_name);
        user.Events.Clear();
        user.Events.AddRange(events);
        return user;
    }
    public async Task<User> CreateUserAsync(string user_name, PhoneNumber phone_number, string password)
    {
        _logger.LogInformation($"Пользователь запросил создание аккаунта с именем пользователя {user_name} и номером телефона {phone_number}");
        var s = new UserSettings(Guid.NewGuid(), true, user_name, []);
        var u = new User(user_name, password, phone_number, s);
        if (!await _userRepo.TryCreateAsync(u))
            throw new UserAlreadyExistsException(user_name);
        _logger.LogInformation($"Аккаунт {user_name} был успешно создан");
        return await GetUserAsync(u.NameID);
    }
    public User CreateUser(string user_name, PhoneNumber phone_number, string password)
    {
        _logger.LogInformation($"Пользователь запросил создание аккаунта с именем пользователя {user_name} и номером телефона {phone_number}");
        var s = new UserSettings(Guid.NewGuid(), true, user_name, []);
        var u = new User(user_name, password, phone_number, s);
        if (!_userRepo.TryCreate(u))
            throw new UserAlreadyExistsException(user_name);
        _logger.LogInformation($"Аккаунт {user_name} был успешно создан");
        return GetUser(u.NameID);
    }
    public async Task<User> LogInAsync(string user_name, string password)
    {
        _logger.LogInformation($"Пользователь запросил вход в аккаунт с именем {user_name}");
        var u = await _userRepo.TryGetAsync(user_name);
        if (u == null)
            throw new UserNotFoundException(user_name);
        if (u.PasswordHash != password)
            throw new InvalidCredentialsException(user_name);
        _logger.LogInformation($"Вход в аккаунт {user_name} был успешно выполнен");
        return await GetUserAsync(u.NameID);
    }
    public User LogIn(string user_name, string password)
    {
        _logger.LogInformation($"Пользователь запросил вход в аккаунт с именем {user_name}");
        var u = _userRepo.TryGet(user_name);
        if (u == null)
            throw new UserNotFoundException(user_name);
        if (u.PasswordHash != password)
            throw new InvalidCredentialsException(user_name);
        _logger.LogInformation($"Вход в аккаунт {user_name} был успешно выполнен");
        return GetUser(u.NameID);
    }
    public async Task<Tuple<User, List<Habit>>> ImportNewSheduleAsync(string user_name, string path)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил импорт нового расписания из файла {path}");
        if (await _userRepo.TryGetAsync(user_name) == null)
            throw new UserNotFoundException(user_name);
        List<Event> events;
        try
        {
            events = _shedLoader.LoadShedule(user_name, path);
        }
        catch (Exception ex)
        {
            throw new ScheduleLoadException(user_name, path, ex.Message);
        }
        var habits = await _habitRepo.TryGetAsync(user_name);
        if (habits == null)
            throw new HabitsNotFoundException(user_name);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!await _eventRepo.TryReplaceEventsAsync(events, user_name))
            throw new RepositoryOperationException("перезаписи", "событий", user_name);
        if (!await _habitRepo.TryReplaceHabitsAsync(habits, user_name))
            throw new RepositoryOperationException("перезаписи", "привычек", user_name);
        _logger.LogInformation($"Импорт нового расписания для пользователя {user_name} из файла {path} произошел успешно");
        return new Tuple<User, List<Habit>>(await GetUserAsync(user_name), no_distributed);
    }
    public async Task<Tuple<User, List<Habit>>> ImportNewSheduleAsync(string user_name, Stream stream, string extension)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил импорт нового расписания из переданного потока");
        if (await _userRepo.TryGetAsync(user_name) == null)
            throw new UserNotFoundException(user_name);
        List<Event> events;
        try
        {
            events = _shedLoader.LoadShedule(user_name, stream, extension);
        }
        catch (Exception ex)
        {
            throw new ScheduleLoadException(user_name, stream, ex.Message);
        }
        var habits = await _habitRepo.TryGetAsync(user_name);
        if (habits == null)
            throw new HabitsNotFoundException(user_name);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!await _eventRepo.TryReplaceEventsAsync(events, user_name))
            throw new RepositoryOperationException("перезаписи", "событий", user_name);
        if (!await _habitRepo.TryReplaceHabitsAsync(habits, user_name))
            throw new RepositoryOperationException("перезаписи", "привычек", user_name);
        _logger.LogInformation($"Импорт нового расписания для пользователя {user_name} из переданного потока произошел успешно");
        return new Tuple<User, List<Habit>>(await GetUserAsync(user_name), no_distributed);
    }
    public Tuple<User, List<Habit>> ImportNewShedule(string user_name, string path)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил импорт нового расписания из файла {path}");
        if (_userRepo.TryGet(user_name) == null)
            throw new UserNotFoundException(user_name);
        List<Event> events;
        try
        {
            events = _shedLoader.LoadShedule(user_name, path);
        }
        catch (Exception ex)
        {
            throw new ScheduleLoadException(user_name, path, ex.Message);
        }
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null)
            throw new HabitsNotFoundException(user_name);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_eventRepo.TryReplaceEvents(events, user_name))
            throw new RepositoryOperationException("перезаписи", "событий", user_name);
        if (!_habitRepo.TryReplaceHabits(habits, user_name))
            throw new RepositoryOperationException("перезаписи", "привычек", user_name);
        _logger.LogInformation($"Импорт нового расписания для пользователя {user_name} из файла {path} произошел успешно");
        return new Tuple<User, List<Habit>>(GetUser(user_name), no_distributed);
    }
    public Tuple<User, List<Habit>> ImportNewShedule(string user_name, Stream stream, string extension)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил импорт нового расписания из переданного потока");
        if (_userRepo.TryGet(user_name) == null)
            throw new UserNotFoundException(user_name);
        List<Event> events;
        try
        {
            events = _shedLoader.LoadShedule(user_name, stream, extension);
        }
        catch (Exception ex)
        {
            throw new ScheduleLoadException(user_name, stream, ex.Message);
        }
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null)
            throw new HabitsNotFoundException(user_name);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_eventRepo.TryReplaceEvents(events, user_name))
            throw new RepositoryOperationException("перезаписи", "событий", user_name);
        if (!_habitRepo.TryReplaceHabits(habits, user_name))
            throw new RepositoryOperationException("перезаписи", "привычек", user_name);
        _logger.LogInformation($"Импорт нового расписания для пользователя {user_name} из переданного потока произошел успешно");
        return new Tuple<User, List<Habit>>(GetUser(user_name), no_distributed);
    }
    public async Task<Tuple<User, List<Habit>>> AddHabitAsync(Habit habit)
    {
        _logger.LogInformation($"Пользователь с именем {habit.UserNameID} запросил добавление привычки {habit.Name}");
        if (await _userRepo.TryGetAsync(habit.UserNameID) == null)
            throw new UserNotFoundException(habit.UserNameID);
        var events = await _eventRepo.TryGetAsync(habit.UserNameID);
        if (events == null)
            throw new EventsNotFoundException(habit.UserNameID);
        var habits = await _habitRepo.TryGetAsync(habit.UserNameID);
        if (habits == null)
            throw new HabitsNotFoundException(habit.UserNameID);
        habits.Add(habit);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!await _habitRepo.TryReplaceHabitsAsync(habits, habit.UserNameID))
            throw new RepositoryOperationException("перезаписи", "привычек", habit.UserNameID);
        _logger.LogInformation($"Привычка {habit.Name} была успешно добавлена для пользователя {habit.UserNameID}");
        return new Tuple<User, List<Habit>>(await GetUserAsync(habit.UserNameID), no_distributed);
    }
    public Tuple<User, List<Habit>> AddHabit(Habit habit)
    {
        _logger.LogInformation($"Пользователь с именем {habit.UserNameID} запросил добавление привычки {habit.Name}");
        if (_userRepo.TryGet(habit.UserNameID) == null)
            throw new UserNotFoundException(habit.UserNameID);
        var events = _eventRepo.TryGet(habit.UserNameID);
        if (events == null)
            throw new EventsNotFoundException(habit.UserNameID);
        var habits = _habitRepo.TryGet(habit.UserNameID);
        if (habits == null)
            throw new HabitsNotFoundException(habit.UserNameID);
        habits.Add(habit);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_habitRepo.TryReplaceHabits(habits, habit.UserNameID))
            throw new RepositoryOperationException("перезаписи", "привычек", habit.UserNameID);
        _logger.LogInformation($"Привычка {habit.Name} была успешно добавлена для пользователя {habit.UserNameID}");
        return new Tuple<User, List<Habit>>(GetUser(habit.UserNameID), no_distributed);
    }
    public async Task<Tuple<User, List<Habit>>> DeleteHabitAsync(string user_name, string name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление привычки {name}");
        if (await _userRepo.TryGetAsync(user_name) == null)
            throw new UserNotFoundException(user_name);

        var events = await _eventRepo.TryGetAsync(user_name);
        if (events == null)
            throw new EventsNotFoundException(user_name);
        var habits = await _habitRepo.TryGetAsync(user_name);
        if (habits == null)
            throw new HabitsNotFoundException(user_name);

        habits.RemoveAll(h => h.Name == name);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!await _habitRepo.TryReplaceHabitsAsync(habits, user_name))
            throw new RepositoryOperationException("перезаписи", "привычек", user_name);
        _logger.LogInformation($"Удаление привычки {name} для пользователя {user_name} произведено успешно");
        return new Tuple<User, List<Habit>>(await GetUserAsync(user_name), no_distributed);
    }
    public Tuple<User, List<Habit>> DeleteHabit(string user_name, string name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление привычки {name}");
        if (_userRepo.TryGet(user_name) == null)
            throw new UserNotFoundException(user_name);
        var events = _eventRepo.TryGet(user_name);
        if (events == null)
            throw new EventsNotFoundException(user_name);
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null)
            throw new HabitsNotFoundException(user_name);
        habits.RemoveAll(h => h.Name == name);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_habitRepo.TryReplaceHabits(habits, user_name))
            throw new RepositoryOperationException("перезаписи", "привычек", user_name);
        _logger.LogInformation($"Удаление привычки {name} для пользователя {user_name} произведено успешно");
        return new Tuple<User, List<Habit>>(GetUser(user_name), no_distributed);
    }
    public async Task<Tuple<User, List<Habit>>> DeleteHabitsAsync(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление всех своих привычек");
        if (await _userRepo.TryGetAsync(user_name) == null)
            throw new UserNotFoundException(user_name);
        var habits = await _habitRepo.TryGetAsync(user_name);
        if (habits == null)
            throw new HabitsNotFoundException(user_name);
        if (!await _habitRepo.TryDeleteHabitsAsync(user_name))
            throw new RepositoryOperationException("удаления", "привычек", user_name);
        _logger.LogInformation($"Удаление привычек для пользователя {user_name} произведено успешно");
        return new Tuple<User, List<Habit>>(await GetUserAsync(user_name), []);
    }
    public Tuple<User, List<Habit>> DeleteHabits(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление всех своих привычек");
        if (_userRepo.TryGet(user_name) == null)
            throw new UserNotFoundException(user_name);
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null)
            throw new HabitsNotFoundException(user_name);
        if (!_habitRepo.TryDeleteHabits(user_name))
            throw new RepositoryOperationException("удаления", "привычек", user_name);
        _logger.LogInformation($"Удаление привычек для пользователя {user_name} произведено успешно");
        return new Tuple<User, List<Habit>>(GetUser(user_name), []);
    }
    public async Task<User> ChangeSettingsAsync(UserSettings settings)
    {
        _logger.LogInformation($"Пользователь с именем {settings.UserNameID} запросил изменение своих настроек");
        if (await _userRepo.TryGetAsync(settings.UserNameID) == null)
            throw new UserNotFoundException(settings.UserNameID);
        if (!await _userRepo.TryUpdateSettingsAsync(settings))
            throw new RepositoryOperationException("обновления", "настроек", settings.UserNameID);
        _logger.LogInformation($"Изменение настроек для пользователя {settings.UserNameID} произведено успешно");
        return await GetUserAsync(settings.UserNameID);
    }
    public User ChangeSettings(UserSettings settings)
    {
        _logger.LogInformation($"Пользователь с именем {settings.UserNameID} запросил изменение своих настроек");
        if (_userRepo.TryGet(settings.UserNameID) == null)
            throw new UserNotFoundException(settings.UserNameID);
        if (!_userRepo.TryUpdateSettings(settings))
            throw new RepositoryOperationException("обновления", "настроек", settings.UserNameID);
        _logger.LogInformation($"Изменение настроек для пользователя {settings.UserNameID} произведено успешно");
        return GetUser(settings.UserNameID);
    }

    public async Task<User> ChangeSettingsAsync(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил изменение своих настроек");
        if (!await _userRepo.TryUpdateSettingsAsync(newTimings, notifyOn, user_name))
            throw new RepositoryOperationException("обновления", "настроек", user_name);
        _logger.LogInformation($"Изменение настроек для пользователя {user_name} произведено успешно");
        return await GetUserAsync(user_name);
    }
    public User ChangeSettings(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил изменение своих настроек");
        if (!_userRepo.TryUpdateSettings(newTimings, notifyOn, user_name))
            throw new RepositoryOperationException("обновления", "настроек", user_name);
        _logger.LogInformation($"Изменение настроек для пользователя {user_name} произведено успешно");
        return GetUser(user_name);
    }
    public async Task<User> NotificationsOnAsync(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил включение уведомлений");
        if (await _userRepo.TryGetAsync(user_name) == null)
            throw new UserNotFoundException(user_name);
        if (!await _userRepo.TryNotificationsOnAsync(user_name))
            throw new RepositoryOperationException("включения", "уведомлений", user_name);
        _logger.LogInformation($"Включение уведомлений для пользователя {user_name} произведено успешно");
        return await GetUserAsync(user_name);
    }
    public User NotificationsOn(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил включение уведомлений");
        if (_userRepo.TryGet(user_name) == null)
            throw new UserNotFoundException(user_name);
        if (!_userRepo.TryNotificationsOn(user_name))
            throw new RepositoryOperationException("включения", "уведомлений", user_name);
        _logger.LogInformation($"Включение уведомлений для пользователя {user_name} произведено успешно");
        return GetUser(user_name);
    }
    public async Task<User> NotificationsOffAsync(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил выключение уведомлений");
        if (await _userRepo.TryGetAsync(user_name) == null)
            throw new UserNotFoundException(user_name);
        if (!await _userRepo.TryNotificationsOffAsync(user_name))
            throw new RepositoryOperationException("выключения", "уведомлений", user_name);
        _logger.LogInformation($"Выключение уведомлений для пользователя {user_name} произведено успешно");
        return await GetUserAsync(user_name);
    }
    public User NotificationsOff(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил выключение уведомлений");
        if (_userRepo.TryGet(user_name) == null)
            throw new UserNotFoundException(user_name);
        if (!_userRepo.TryNotificationsOff(user_name))
            throw new RepositoryOperationException("выключения", "уведомлений", user_name);
        _logger.LogInformation($"Выключение уведомлений для пользователя {user_name} произведено успешно");
        return GetUser(user_name);
    }
    public async Task<User> UpdateNotificationTimingsAsync(List<Tuple<TimeOnly, TimeOnly>> newTimings, string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил обновление времени запрета уведомлений");
        if (await _userRepo.TryGetAsync(user_name) == null)
            throw new UserNotFoundException(user_name);
        if (!await _userRepo.TryUpdateNotificationTimingsAsync(newTimings, user_name))
            throw new RepositoryOperationException("обновления", "времени уведомлений", user_name);
        _logger.LogInformation($"Обновление времени запрета уведомлений для пользователя {user_name} произведено успешно");
        return await GetUserAsync(user_name);
    }
    public User UpdateNotificationTimings(List<Tuple<TimeOnly, TimeOnly>> newTimings, string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил обновление времени запрета уведомлений");
        if (_userRepo.TryGet(user_name) == null)
            throw new UserNotFoundException(user_name);
        if (!_userRepo.TryUpdateNotificationTimings(newTimings, user_name))
            throw new RepositoryOperationException("обновления", "времени уведомлений", user_name);
        _logger.LogInformation($"Обновление времени запрета уведомлений для пользователя {user_name} произведено успешно");
        return GetUser(user_name);
    }
    public async Task DeleteUserAsync(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление своей учетной записи");
        var ret = await _userRepo.TryDeleteAsync(user_name);
        if (!ret)
            throw new UserNotFoundException(user_name);
        _logger.LogInformation($"Удаление учетной записи пользователя {user_name} произведено успешно");
    }
    public void DeleteUser(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление своей учетной записи");
        var ret = _userRepo.TryDelete(user_name);
        if (!ret)
        {
            throw new UserNotFoundException(user_name);
        }
        _logger.LogInformation($"Удаление учетной записи пользователя {user_name} произведено успешно");
    }
}