using Domain.OutPorts;
using Domain.InPorts;
using Domain.Models;
using Types;

namespace Domain;

public class TaskTracker : ITaskTracker
{
    private readonly IEventRepo _eventRepo;
    private readonly IHabitRepo _habitRepo;
    private readonly IUserRepo _userRepo;
    private readonly ISheduleLoad _shedLoader;
    private readonly IHabitDistributor _distributer;

    public TaskTracker(IEventRepo eventRepo, IHabitRepo habitRepo,
        IUserRepo userRepo, ISheduleLoad shedLoader, IHabitDistributor distributer)
    {
        _eventRepo = eventRepo;
        _habitRepo = habitRepo;
        _userRepo = userRepo;
        _shedLoader = shedLoader;
        _distributer = distributer;
    }

    /*Функция по username получает всю информацию о пользователе, его привычках, событиях расписания и настройках*/
    private User? GetUser(string user_name)
    {
        return _userRepo.TryFullGet(user_name);
    }

    /*Функция создания пользователя по имени пользователя, номеру телефона и паролю
     Возвращает полную информацию о пользователе если такого пользователя в системе еще нет
     иначе возвращает null*/
    public User? CreateUser(string username, PhoneNumber phone_number, string password)
    {
        var s = new UserSettings(Guid.NewGuid(), true, username, []);
        var u = new User(username, password, phone_number, s);
        if (!_userRepo.TryCreate(u)) return null;
        return GetUser(u.NameID);
    }

    /*Функция входа в аккаунт пользователя по имени пользователя и паролю
     Возвращает полную информацию о пользователе если такой пользователь существует и пароль верен
     иначе возвращает null*/
    public User? LogIn(string username, string password)
    {
        var u = _userRepo.TryGet(username);
        if (u == null) return null;
        if (u.PasswordHash != password) return null;
        return GetUser(u.NameID);
    }

    /*Функция импорта нового расписания для пользователя с именем-идентификатором user_name
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>>? ImportNewShedule(string user_name, string path)
    {
        //В текущей реализации удаляем все привычки, перераспределем и добавляем заново, хорошо бы переделать под Update
        if (_userRepo.TryGet(user_name) == null) return null;

        var events = _shedLoader.LoadShedule(user_name, path);
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null) return null;
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_eventRepo.TryReplaceEvents(events, user_name)) return null;
        if (!_habitRepo.TryReplaceHabits(habits, user_name)) return null;

        return new Tuple<User, List<Habit>>(GetUser(user_name), no_distributed);
    }

    /*Функция добавления привычки для пользователя с именем-идентификатором user_name
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>>? AddHabit(Habit habit)
    {
        //В текущей реализации удаляем все привычки, перераспределем и добавляем заново, хорошо бы переделать под Update
        if (_userRepo.TryGet(habit.UserNameID) == null) return null;

        var events = _eventRepo.TryGet(habit.UserNameID);
        if (events == null) return null;
        var habits = _habitRepo.TryGet(habit.UserNameID);
        if (habits == null) return null;
        habits.Add(habit);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_eventRepo.TryReplaceEvents(events, habit.UserNameID)) return null;
        if (!_habitRepo.TryReplaceHabits(habits, habit.UserNameID)) return null;

        return new Tuple<User, List<Habit>>(GetUser(habit.UserNameID), no_distributed);
    }

    /*Функция удаления привычки для пользователя с именем-идентификатором user_name
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>>? DeleteHabit(string user_name, string name)
    {
        if (_userRepo.TryGet(user_name) == null) return null;

        var events = _eventRepo.TryGet(user_name);
        if (events == null) return null;
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null) return null;
        habits.RemoveAll(h => h.Name == name);

        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_eventRepo.TryReplaceEvents(events, user_name)) return null;
        if (!_habitRepo.TryReplaceHabits(habits, user_name)) return null;

        return new Tuple<User, List<Habit>>(GetUser(user_name), no_distributed);
    }

    public User? ChangeSettings(UserSettings settings)
    {
        if (_userRepo.TryGet(settings.UserNameID) == null) return null;
        if (!_userRepo.TryUpdateSettings(settings))
            return null;
        return GetUser(settings.UserNameID);
    }
}