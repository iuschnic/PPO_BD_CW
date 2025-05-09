using Domain.OutPorts;
using Domain.InPorts;
using Domain.Models;
using Types;

namespace Domain;

public class TaskTracker : ITaskTracker
{
    private readonly IEventRepo _eventRepo;
    private readonly IHabitRepo _habitRepo;
    private readonly ISettingsRepo _settingsRepo;
    private readonly IUserRepo _userRepo;
    private readonly IShedLoad _shedLoader;
    private readonly IHabitDistributor _distributer;

    /*Функция по guid получает всю информацию о пользователе, его привычках, событиях расписания и настройках*/
    private User? GetUser(string user_name)
    {
        User? user = _userRepo.TryGet(user_name);
        if (user == null) return null;
        UserSettings? settings = _settingsRepo.TryGet(user_name);
        if (settings == null) return null;
        List<Habit>? habits = _habitRepo.TryGet(user_name);
        if (habits == null) return null;
        if (habits == null) habits = [];
        List<Event>? events = _eventRepo.TryGet(user_name);
        if (events == null) return null;
        user.Habits = habits;
        user.Events = events;
        user.Settings = settings;
        return user;
    }

    public TaskTracker(IEventRepo eventRepo, IHabitRepo habitRepo,
        ISettingsRepo settingsRepo, IUserRepo userRepo, IShedLoad shedLoader, IHabitDistributor distributer)
    {
        _eventRepo = eventRepo;
        _habitRepo = habitRepo;
        //_messageRepo = messageRepo;
        _settingsRepo = settingsRepo;
        _userRepo = userRepo;
        _shedLoader = shedLoader;
        _distributer = distributer;
    }

    /*Функция создания пользователя по имени пользователя, номеру телефона и паролю
     Возвращает полную информацию о пользователе если такого пользователя в системе еще нет
     иначе возвращает null*/
    public User? CreateUser(string username, PhoneNumber phone_number, string password)
    {
        //Возможно userepo лучше объединить с settingsrepo так как не существует пользователя без настроек и наоборот
        var u = new User(username, password, phone_number);
        if (!_userRepo.TryCreate(u)) return null;
        var s = new UserSettings(Guid.NewGuid(), true, u.NameID, []);
        if (!_settingsRepo.TryCreate(s))
        {
            _userRepo.TryDelete(u.NameID);
            return null;
        }

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

    /*Функция импорта нового расписания для пользователя с идентификатором user_id
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>>? ImportNewShedule(string user_name, string path)
    {
        //В текущей реализации удаляем все привычки, перераспределем и добавляем заново, хорошо бы переделать под Update
        User? u = _userRepo.TryGet(user_name);
        if (u == null) return null;

        var events = _shedLoader.LoadShedule(user_name, path);
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null) return null;
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_eventRepo.TryDeleteEvents(user_name)) return null;
        if (!_eventRepo.TryCreateMany(events)) return null;
        if (!_habitRepo.TryDeleteHabits(user_name)) return null;
        if (!_habitRepo.TryCreateMany(habits)) return null;

        u = GetUser(user_name);
        return new Tuple<User, List<Habit>>(u, no_distributed);
    }

    /*Функция добавления привычки для пользователя с идентификатором user_id
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>>? AddHabit(string user_name, string name, int mins_complete, int ndays, TimeOption op,
        List<Tuple<TimeOnly, TimeOnly>> preffixedtimes)
    {
        //В текущей реализации удаляем все привычки, перераспределем и добавляем заново, хорошо бы переделать под Update
        User? u = _userRepo.TryGet(user_name);
        if (u == null) return null;

        var events = _eventRepo.TryGet(user_name);
        if (events == null) return null;
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null) return null;
        Guid hid = Guid.NewGuid();
        List<PrefFixedTime> times = [];
        foreach (var t in preffixedtimes)
            times.Add(new PrefFixedTime(Guid.NewGuid(), t.Item1, t.Item2, hid));
        Habit habit = new Habit(hid, name, mins_complete, op, u.NameID, [], times, ndays);
        habits.Add(habit);
        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_eventRepo.TryDeleteEvents(user_name)) return null;
        if (!_eventRepo.TryCreateMany(events)) return null;
        if (!_habitRepo.TryDeleteHabits(user_name)) return null;
        if (!_habitRepo.TryCreateMany(habits)) return null;

        u = GetUser(user_name);
        return new Tuple<User, List<Habit>>(u, no_distributed);
    }

    /*Функция удаления привычки для пользователя с идентификатором user_id
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>>? DeleteHabit(string user_name, string name)
    {
        User? u = _userRepo.TryGet(user_name);
        if (u == null) return null;

        var events = _eventRepo.TryGet(user_name);
        if (events == null) return null;
        var habits = _habitRepo.TryGet(user_name);
        if (habits == null) return null;
        habits.RemoveAll(h => h.Name == name);

        List<Habit> no_distributed = _distributer.DistributeHabits(habits, events);

        if (!_eventRepo.TryDeleteEvents(user_name)) return null;
        if (!_eventRepo.TryCreateMany(events)) return null;
        if (!_habitRepo.TryDeleteHabits(user_name)) return null;
        if (!_habitRepo.TryCreateMany(habits)) return null;

        u = GetUser(user_name);
        return new Tuple<User, List<Habit>>(u, no_distributed);
    }

    /*Функция изменения флага разрешения отправки сообщения для пользователя с идентификатором user_id
     Возвращает кортеж информацию о пользователе если он существует*/
    //TODO вместо changenotify нудно сделать changesettings
    /*public User? ChangeNotify(string user_name, bool value)
    {
        var settings = _settingsRepo.TryGet(user_name);
        if (settings == null) return null;
        settings.NotifyOn = value;
        _settingsRepo.Update(settings);
        return GetUser(user_name);
    }*/

    public User? ChangeSettings(UserSettings settings)
    {
        var dbs = _settingsRepo.TryGet(settings.UserNameID);
        if (dbs == null) return null;
        if (!_settingsRepo.TryUpdate(settings))
            return null;
        return GetUser(settings.UserNameID);
    }
}
