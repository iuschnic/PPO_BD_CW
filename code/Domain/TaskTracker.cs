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
        if (user == null) throw new Exception("Ошибка получения информации о пользователе");
        var events = await _eventRepo.TryGetAsync(user_name);
        if (events == null) throw new Exception("Ошибка получения информации о событиях пользователя");
        user.Events.Clear();
        user.Events.AddRange(events);
        return user;
    }
    /*Функция по username получает всю информацию о пользователе, его привычках, событиях расписания и настройках*/
    private User GetUser(string user_name)
    {
        var user = _userRepo.TryFullGet(user_name);
        if (user == null) throw new Exception("Ошибка получения информации о пользователе");
        var events = _eventRepo.TryGet(user_name);
        if (events == null) throw new Exception("Ошибка получения информации о событиях пользователя");
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
        {
            _logger.LogInformation($"Аккаунт не был создан так как уже существует аккаунт с именем пользователя {user_name}");
            throw new Exception($"Аккаунт не был создан так как уже существует аккаунт с именем пользователя {user_name}");
        }
        _logger.LogInformation($"Аккаунт {user_name} был успешно создан");
        return await GetUserAsync(u.NameID);
    }
    /*Функция создания пользователя по имени пользователя, номеру телефона и паролю
     Возвращает полную информацию о пользователе если такого пользователя в системе еще нет
     иначе возвращает null*/
    public User CreateUser(string user_name, PhoneNumber phone_number, string password)
    {
        _logger.LogInformation($"Пользователь запросил создание аккаунта с именем пользователя {user_name} и номером телефона {phone_number}");
        var s = new UserSettings(Guid.NewGuid(), true, user_name, []);
        var u = new User(user_name, password, phone_number, s);
        if (!_userRepo.TryCreate(u))
        {
            _logger.LogInformation($"Аккаунт не был создан так как уже существует аккаунт с именем пользователя {user_name}");
            throw new Exception($"Аккаунт не был создан так как уже существует аккаунт с именем пользователя {user_name}");
        }
        _logger.LogInformation($"Аккаунт {user_name} был успешно создан");
        return GetUser(u.NameID);
    }
    public async Task<User> LogInAsync(string user_name, string password)
    {
        _logger.LogInformation($"Пользователь запросил вход в аккаунт с именем {user_name}");
        var u = await _userRepo.TryGetAsync(user_name);
        if (u == null)
        {
            _logger.LogInformation($"Вход в аккаунт {user_name} не был выполнен так как такого пользователя не существует");
            throw new Exception($"Вход в аккаунт {user_name} не был выполнен так как такого пользователя не существует");
        }
        if (u.PasswordHash != password)
        {
            _logger.LogInformation($"Вход в аккаунт {user_name} не был выполнен так как пользователь ввел неправильный пароль");
            throw new Exception($"Вход в аккаунт {user_name} не был выполнен так как пользователь ввел неправильный пароль");
        }
        _logger.LogInformation($"Вход в аккаунт {user_name} был успешно выполнен");
        return await GetUserAsync(u.NameID);
    }
    /*Функция входа в аккаунт пользователя по имени пользователя и паролю
     Возвращает полную информацию о пользователе если такой пользователь существует и пароль верен
     иначе возвращает null*/
    public User LogIn(string user_name, string password)
    {
        _logger.LogInformation($"Пользователь запросил вход в аккаунт с именем {user_name}");
        var u = _userRepo.TryGet(user_name);
        if (u == null)
        {
            _logger.LogInformation($"Вход в аккаунт {user_name} не был выполнен так как такого пользователя не существует");
            throw new Exception($"Вход в аккаунт {user_name} не был выполнен так как такого пользователя не существует");
        }
        if (u.PasswordHash != password)
        {
            _logger.LogInformation($"Вход в аккаунт {user_name} не был выполнен так как пользователь ввел неправильный пароль");
            throw new Exception($"Вход в аккаунт {user_name} не был выполнен так как пользователь ввел неправильный пароль");
        }
        _logger.LogInformation($"Вход в аккаунт {user_name} был успешно выполнен");
        return GetUser(u.NameID);
    }
    public async Task<Tuple<User, List<Habit>>> ImportNewSheduleAsync(string user_name, string path)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил импорт нового расписания из файла {path}");
        if (await _userRepo.TryGetAsync(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        List<Event> events;
        try
        {
            events = _shedLoader.LoadShedule(user_name, path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Ошибка загрузки расписания для пользователя {user_name}: {ex.Message}");
            throw new Exception($"Ошибка загрузки расписания для пользователя {user_name}: {ex.Message}");
        }
        var habits = await _habitRepo.TryGetAsync(user_name);
        if (habits == null)
        {
            _logger.LogError($"Не удалось получить привычки для пользователя {user_name}");
            throw new Exception($"Не удалось получить привычки для пользователя {user_name}");
        }
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!await _eventRepo.TryReplaceEventsAsync(events, user_name))
        {
            _logger.LogError($"Ошибка при попытке перезаписи событий пользователя {user_name} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи событий пользователя {user_name} в базу данных");
        }
        if (!await _habitRepo.TryReplaceHabitsAsync(habits, user_name))
        {
            _logger.LogError($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
        }
        _logger.LogInformation($"Импорт нового расписания для пользователя {user_name} из файла {path} произошел успешно");
        return new Tuple<User, List<Habit>>(await GetUserAsync(user_name), no_distributed);
    }
    public async Task<Tuple<User, List<Habit>>> ImportNewSheduleAsync(string user_name, Stream stream, string extension)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил импорт нового расписания из переданного потока");
        if (await _userRepo.TryGetAsync(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        List<Event> events;
        try
        {
            events = _shedLoader.LoadShedule(user_name, stream, extension);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Ошибка загрузки расписания для пользователя {user_name}: {ex.Message}");
            throw new Exception($"Ошибка загрузки расписания для пользователя {user_name}: {ex.Message}");
        }
        var habits = await _habitRepo.TryGetAsync(user_name);
        if (habits == null)
        {
            _logger.LogError($"Не удалось получить привычки для пользователя {user_name}");
            throw new Exception($"Не удалось получить привычки для пользователя {user_name}");
        }
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!await _eventRepo.TryReplaceEventsAsync(events, user_name))
        {
            _logger.LogError($"Ошибка при попытке перезаписи событий пользователя {user_name} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи событий пользователя {user_name} в базу данных");
        }
        if (!await _habitRepo.TryReplaceHabitsAsync(habits, user_name))
        {
            _logger.LogError($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
        }
        _logger.LogInformation($"Импорт нового расписания для пользователя {user_name} из переданного потока произошел успешно");
        return new Tuple<User, List<Habit>>(await GetUserAsync(user_name), no_distributed);
    }
    /*Функция импорта нового расписания для пользователя с именем-идентификатором user_name
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>> ImportNewShedule(string user_name, string path)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил импорт нового расписания из файла {path}");
        if (_userRepo.TryGet(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        List<Event> events;
        try
        {
            events = _shedLoader.LoadShedule(user_name, path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Ошибка загрузки расписания для пользователя {user_name}: {ex.Message}");
            throw new Exception($"Ошибка загрузки расписания для пользователя {user_name}: {ex.Message}");
        }
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null)
        {
            _logger.LogError($"Не удалось получить привычки для пользователя {user_name}");
            throw new Exception($"Не удалось получить привычки для пользователя {user_name}");
        }
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_eventRepo.TryReplaceEvents(events, user_name))
        {
            _logger.LogError($"Ошибка при попытке перезаписи событий пользователя {user_name} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи событий пользователя {user_name} в базу данных");
        }
        if (!_habitRepo.TryReplaceHabits(habits, user_name))
        {
            _logger.LogError($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
        }
        _logger.LogInformation($"Импорт нового расписания для пользователя {user_name} из файла {path} произошел успешно");
        return new Tuple<User, List<Habit>>(GetUser(user_name), no_distributed);
    }
    public Tuple<User, List<Habit>> ImportNewShedule(string user_name, Stream stream, string extension)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил импорт нового расписания из переданного потока");
        if (_userRepo.TryGet(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        List<Event> events;
        try
        {
            events = _shedLoader.LoadShedule(user_name, stream, extension);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Ошибка загрузки расписания для пользователя {user_name}: {ex.Message}");
            throw new Exception($"Ошибка загрузки расписания для пользователя {user_name}: {ex.Message}");
        }
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null)
        {
            _logger.LogError($"Не удалось получить привычки для пользователя {user_name}");
            throw new Exception($"Не удалось получить привычки для пользователя {user_name}");
        }
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_eventRepo.TryReplaceEvents(events, user_name))
        {
            _logger.LogError($"Ошибка при попытке перезаписи событий пользователя {user_name} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи событий пользователя {user_name} в базу данных");
        }
        if (!_habitRepo.TryReplaceHabits(habits, user_name))
        {
            _logger.LogError($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
        }
        _logger.LogInformation($"Импорт нового расписания для пользователя {user_name} из переданного потока произошел успешно");
        return new Tuple<User, List<Habit>>(GetUser(user_name), no_distributed);
    }
    public async Task<Tuple<User, List<Habit>>> AddHabitAsync(Habit habit)
    {
        _logger.LogInformation($"Пользователь с именем {habit.UserNameID} запросил добавление привычки {habit.Name}");
        if (await _userRepo.TryGetAsync(habit.UserNameID) == null)
        {
            _logger.LogError($"Пользователя с именем {habit.UserNameID} не существует в базе данных");
            throw new Exception($"Пользователя с именем {habit.UserNameID} не существует в базе данных");
        }

        var events = await _eventRepo.TryGetAsync(habit.UserNameID);
        if (events == null)
        {
            _logger.LogError($"Не удалось получить события для пользователя {habit.UserNameID}");
            throw new Exception($"Не удалось получить события для пользователя {habit.UserNameID}");
        }
        var habits = await _habitRepo.TryGetAsync(habit.UserNameID);
        if (habits == null)
        {
            _logger.LogError($"Не удалось получить привычки для пользователя {habit.UserNameID}");
            throw new Exception($"Не удалось получить привычки для пользователя {habit.UserNameID}");
        }
        habits.Add(habit);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!await _habitRepo.TryReplaceHabitsAsync(habits, habit.UserNameID))
        {
            _logger.LogError($"Ошибка при попытке перезаписи привычек пользователя {habit.UserNameID} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи привычек пользователя {habit.UserNameID} в базу данных");
        }
        _logger.LogInformation($"Привычка {habit.Name} была успешно добавлена для пользователя {habit.UserNameID}");
        return new Tuple<User, List<Habit>>(await GetUserAsync(habit.UserNameID), no_distributed);
    }
    /*Функция добавления привычки для пользователя с именем-идентификатором user_name
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>> AddHabit(Habit habit)
    {
        _logger.LogInformation($"Пользователь с именем {habit.UserNameID} запросил добавление привычки {habit.Name}");
        if (_userRepo.TryGet(habit.UserNameID) == null)
        {
            _logger.LogError($"Пользователя с именем {habit.UserNameID} не существует в базе данных");
            throw new Exception($"Пользователя с именем {habit.UserNameID} не существует в базе данных");
        }

        var events = _eventRepo.TryGet(habit.UserNameID);
        if (events == null)
        {
            _logger.LogError($"Не удалось получить события для пользователя {habit.UserNameID}");
            throw new Exception($"Не удалось получить события для пользователя {habit.UserNameID}");
        }
        var habits = _habitRepo.TryGet(habit.UserNameID);
        if (habits == null)
        {
            _logger.LogError($"Не удалось получить привычки для пользователя {habit.UserNameID}");
            throw new Exception($"Не удалось получить привычки для пользователя {habit.UserNameID}");
        }
        habits.Add(habit);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_habitRepo.TryReplaceHabits(habits, habit.UserNameID))
        {
            _logger.LogError($"Ошибка при попытке перезаписи привычек пользователя {habit.UserNameID} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи привычек пользователя {habit.UserNameID} в базу данных");
        }
        _logger.LogInformation($"Привычка {habit.Name} была успешно добавлена для пользователя {habit.UserNameID}");
        return new Tuple<User, List<Habit>>(GetUser(habit.UserNameID), no_distributed);
    }
    public async Task<Tuple<User, List<Habit>>> DeleteHabitAsync(string user_name, string name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление привычки {name}");
        if (await _userRepo.TryGetAsync(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }

        var events = await _eventRepo.TryGetAsync(user_name);
        if (events == null)
        {
            _logger.LogError($"Не удалось получить события для пользователя {user_name}");
            throw new Exception($"Не удалось получить события для пользователя {user_name}");
        }
        var habits = await _habitRepo.TryGetAsync(user_name);
        if (habits == null)
        {
            _logger.LogError($"Не удалось получить привычки для пользователя {user_name}");
            throw new Exception($"Не удалось получить привычки для пользователя {user_name}");
        }

        habits.RemoveAll(h => h.Name == name);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!await _habitRepo.TryReplaceHabitsAsync(habits, user_name))
        {
            _logger.LogError($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
        }
        _logger.LogInformation($"Удаление привычки {name} для пользователя {user_name} произведено успешно");
        return new Tuple<User, List<Habit>>(await GetUserAsync(user_name), no_distributed);
    }
    /*Функция удаления привычки для пользователя с именем-идентификатором user_name
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>> DeleteHabit(string user_name, string name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление привычки {name}");
        if (_userRepo.TryGet(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }

        var events = _eventRepo.TryGet(user_name);
        if (events == null)
        {
            _logger.LogError($"Не удалось получить события для пользователя {user_name}");
            throw new Exception($"Не удалось получить события для пользователя {user_name}");
        }
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null)
        {
            _logger.LogError($"Не удалось получить привычки для пользователя {user_name}");
            throw new Exception($"Не удалось получить привычки для пользователя {user_name}");
        }

        habits.RemoveAll(h => h.Name == name);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_habitRepo.TryReplaceHabits(habits, user_name))
        {
            _logger.LogError($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
            throw new Exception($"Ошибка при попытке перезаписи привычек пользователя {user_name} в базу данных");
        }
        _logger.LogInformation($"Удаление привычки {name} для пользователя {user_name} произведено успешно");
        return new Tuple<User, List<Habit>>(GetUser(user_name), no_distributed);
    }
    public async Task<Tuple<User, List<Habit>>> DeleteHabitsAsync(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление всех своих привычек");
        if (await _userRepo.TryGetAsync(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        var habits = await _habitRepo.TryGetAsync(user_name);
        if (habits == null)
        {
            _logger.LogError($"Не удалось получить привычки для пользователя {user_name}");
            throw new Exception($"Не удалось получить привычки для пользователя {user_name}");
        }
        if (!await _habitRepo.TryDeleteHabitsAsync(user_name))
        {
            _logger.LogError($"Не удалось удалить привычки для пользователя {user_name}");
            throw new Exception($"Не удалось удалить привычки для пользователя {user_name}");
        }
        _logger.LogInformation($"Удаление привычек для пользователя {user_name} произведено успешно");
        return new Tuple<User, List<Habit>>(await GetUserAsync(user_name), []);
    }
    public Tuple<User, List<Habit>> DeleteHabits(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление всех своих привычек");
        if (_userRepo.TryGet(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null)
        {
            _logger.LogError($"Не удалось получить привычки для пользователя {user_name}");
            throw new Exception($"Не удалось получить привычки для пользователя {user_name}");
        }
        if (!_habitRepo.TryDeleteHabits(user_name))
        {
            _logger.LogError($"Не удалось удалить привычки для пользователя {user_name}");
            throw new Exception($"Не удалось удалить привычки для пользователя {user_name}");
        }
        _logger.LogInformation($"Удаление привычек для пользователя {user_name} произведено успешно");
        return new Tuple<User, List<Habit>>(GetUser(user_name), []);
    }
    public async Task<User> ChangeSettingsAsync(UserSettings settings)
    {
        _logger.LogInformation($"Пользователь с именем {settings.UserNameID} запросил изменение своих настроек");
        if (await _userRepo.TryGetAsync(settings.UserNameID) == null)
        {
            _logger.LogError($"Пользователя с именем {settings.UserNameID} не существует в базе данных");
            throw new Exception($"Пользователя с именем {settings.UserNameID} не существует в базе данных");
        }
        if (!await _userRepo.TryUpdateSettingsAsync(settings))
        {
            _logger.LogError($"Не удалось изменить настройки для пользователя {settings.UserNameID}");
            throw new Exception($"Не удалось изменить настройки для пользователя {settings.UserNameID}");
        }
        _logger.LogInformation($"Изменение настроек для пользователя {settings.UserNameID} произведено успешно");
        return await GetUserAsync(settings.UserNameID);
    }
    public User ChangeSettings(UserSettings settings)
    {
        _logger.LogInformation($"Пользователь с именем {settings.UserNameID} запросил изменение своих настроек");
        if (_userRepo.TryGet(settings.UserNameID) == null)
        {
            _logger.LogError($"Пользователя с именем {settings.UserNameID} не существует в базе данных");
            throw new Exception($"Пользователя с именем {settings.UserNameID} не существует в базе данных");
        }
        if (!_userRepo.TryUpdateSettings(settings))
        {
            _logger.LogError($"Не удалось изменить настройки для пользователя {settings.UserNameID}");
            throw new Exception($"Не удалось изменить настройки для пользователя {settings.UserNameID}");
        }
        _logger.LogInformation($"Изменение настроек для пользователя {settings.UserNameID} произведено успешно");
        return GetUser(settings.UserNameID);
    }
    public async Task<User> NotificationsOnAsync(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил включение уведомлений");
        if (await _userRepo.TryGetAsync(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        if (!await _userRepo.TryNotificationsOnAsync(user_name))
        {
            _logger.LogError($"Не удалось включить уведомления для пользователя {user_name}");
            throw new Exception($"Не удалось включить уведомления для пользователя {user_name}");
        }
        _logger.LogInformation($"Включение уведомлений для пользователя {user_name} произведено успешно");
        return await GetUserAsync(user_name);
    }
    public User NotificationsOn(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил включение уведомлений");
        if (_userRepo.TryGet(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        if (!_userRepo.TryNotificationsOn(user_name))
        {
            _logger.LogError($"Не удалось включить уведомления для пользователя {user_name}");
            throw new Exception($"Не удалось включить уведомления для пользователя {user_name}");
        }
        _logger.LogInformation($"Включение уведомлений для пользователя {user_name} произведено успешно");
        return GetUser(user_name);
    }
    public async Task<User> NotificationsOffAsync(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил выключение уведомлений");
        if (await _userRepo.TryGetAsync(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        if (!await _userRepo.TryNotificationsOffAsync(user_name))
        {
            _logger.LogError($"Не удалось выключить уведомления для пользователя {user_name}");
            throw new Exception($"Не удалось выключить уведомления для пользователя {user_name}");
        }
        _logger.LogInformation($"Выключение уведомлений для пользователя {user_name} произведено успешно");
        return await GetUserAsync(user_name);
    }
    public User NotificationsOff(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил выключение уведомлений");
        if (_userRepo.TryGet(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        if (!_userRepo.TryNotificationsOff(user_name))
        {
            _logger.LogError($"Не удалось выключить уведомления для пользователя {user_name}");
            throw new Exception($"Не удалось выключить уведомления для пользователя {user_name}");
        }
        _logger.LogInformation($"Выключение уведомлений для пользователя {user_name} произведено успешно");
        return GetUser(user_name);
    }
    public async Task<User> UpdateNotificationTimingsAsync(List<Tuple<TimeOnly, TimeOnly>> newTimings, string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил обновление времени запрета уведомлений");
        if (await _userRepo.TryGetAsync(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        if (!await _userRepo.TryUpdateNotificationTimingsAsync(newTimings, user_name))
        {
            _logger.LogError($"Не удалось обновить время запрета уведомлений для пользователя {user_name}");
            throw new Exception($"Не удалось обновить время запрета уведомлений для пользователя {user_name}");
        }
        _logger.LogInformation($"Обновление времени запрета уведомлений для пользователя {user_name} произведено успешно");
        return await GetUserAsync(user_name);
    }
    public User UpdateNotificationTimings(List<Tuple<TimeOnly, TimeOnly>> newTimings, string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил обновление времени запрета уведомлений");
        if (_userRepo.TryGet(user_name) == null)
        {
            _logger.LogError($"Пользователя с именем {user_name} не существует в базе данных");
            throw new Exception($"Пользователя с именем {user_name} не существует в базе данных");
        }
        if (!_userRepo.TryUpdateNotificationTimings(newTimings, user_name))
        {
            _logger.LogError($"Не удалось обновить время запрета уведомлений для пользователя {user_name}");
            throw new Exception($"Не удалось обновить время запрета уведомлений для пользователя {user_name}");
        }
        _logger.LogInformation($"Обновление времени запрета уведомлений для пользователя {user_name} произведено успешно");
        return GetUser(user_name);
    }
    public async Task DeleteUserAsync(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление своей учетной записи");
        var ret = await _userRepo.TryDeleteAsync(user_name);
        if (!ret)
        {
            _logger.LogError($"Не удалось удалить пользователя с именем {user_name}");
            throw new Exception($"Не удалось удалить пользователя с именем {user_name}");
        }
        _logger.LogInformation($"Удаление учетной записи пользователя {user_name} произведено успешно");
    }
    public void DeleteUser(string user_name)
    {
        _logger.LogInformation($"Пользователь с именем {user_name} запросил удаление своей учетной записи");
        var ret = _userRepo.TryDelete(user_name);
        if (!ret)
        {
            _logger.LogError($"Не удалось удалить пользователя с именем {user_name}");
            throw new Exception($"Не удалось удалить пользователя с именем {user_name}");
        }
        _logger.LogInformation($"Удаление учетной записи пользователя {user_name} произведено успешно");
    }
}