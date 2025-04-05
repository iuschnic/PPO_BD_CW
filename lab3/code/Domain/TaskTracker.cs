using Domain.OutPorts;
using Domain.InPorts;
using Types;

namespace Domain;

public class TimeInterval
{
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }

    public TimeInterval(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }
}

public class TaskTracker : ITaskTracker
{
    private readonly IEventRepo _eventRepo;
    private readonly IHabitRepo _habitRepo;
    private readonly IMessageRepo _messageRepo;
    private readonly ISettingsRepo _settingsRepo;
    private readonly IUserRepo _userRepo;
    private readonly IShedLoad _shedLoader;

    private User? GetUser(Guid user_id)
    {
        User? user = _userRepo.Get(user_id);
        if (user == null) return null;
        List<Habit> habits = _habitRepo.Get(user_id);
        if (habits == null) habits = [];
        List<Event> events = _eventRepo.Get(user_id);
        if (events == null) events = [];
        UserSettings? settings = _settingsRepo.Get(user_id);
        if (settings == null) settings = null;
        user.Habits = habits;
        user.Events = events;
        user.Settings = settings;
        return user;
    }

    //По списку интервалов занятости получает список свободных временных интервалов
    private List<TimeInterval> GetFreeIntervals(List<TimeInterval> events)
    {
        // Если нет событий, весь день свободен
        if (events.Count == 0)
        {
            return new List<TimeInterval> { new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59)) };
        }

        // Сортируем события по времени начала
        var sortedEvents = events.OrderBy(e => e.Start).ToList();

        List<TimeInterval> freeIntervals = new List<TimeInterval>();

        // Первый свободный интервал: от начала дня до первого события
        TimeOnly previousEnd = new TimeOnly(0, 0, 0);
        if (sortedEvents[0].Start > previousEnd)
        {
            freeIntervals.Add(new TimeInterval(previousEnd, sortedEvents[0].Start));
        }

        // Проверяем промежутки между событиями
        for (int i = 1; i < sortedEvents.Count; i++)
        {
            TimeOnly currentStart = sortedEvents[i].Start;
            TimeOnly lastEnd = sortedEvents[i - 1].End;

            if (currentStart > lastEnd)
            {
                freeIntervals.Add(new TimeInterval(lastEnd, currentStart));
            }
        }

        // Последний свободный интервал: от конца последнего события до конца дня
        TimeOnly endOfDay = new TimeOnly(23, 59, 59);
        if (sortedEvents.Last().End < endOfDay)
        {
            freeIntervals.Add(new TimeInterval(sortedEvents.Last().End, endOfDay));
        }

        return freeIntervals;
    }

    private Dictionary<string, int> DistributeHabits(List<Habit> habits, List<Event> events)
    {
        //Получаем списки интервалов занятости из расписания для каждого дня
        Dictionary<WeekDay, List<TimeInterval>> eventsIntervals = [];
        foreach (var ev in events)
        {
            if (!eventsIntervals.ContainsKey(ev.Day))
                eventsIntervals[ev.Day] = [];
            var interval = new TimeInterval(ev.Start, ev.End);
            eventsIntervals[ev.Day].Add(interval);
        }
        //Получаем списки интервалов свободного времени в расписании для каждого дня
        Dictionary<string, List<TimeInterval>> freeIntervals = [];
        foreach (var ev in eventsIntervals)
            freeIntervals[ev.Key.StringDay] = GetFreeIntervals(ev.Value);


        /*Получаем списки привычек по приоритетам
        Dictionary<string, Habit> option_habits = [];
        foreach (var h in habits)
            option_habits[h.Option.StringTimeOption] = h;*/

        //Словарь нераспределенных привычек, его возвращаем в интерфейс для уведомления пользователя
        Dictionary<string, int> not_distributed = [];
        /*TODO Различное распределение привычек с разными приоритетами*/
        //Распределяем каждую привычку
        foreach (var h in habits)
        {
            h.ActualTimings.Clear();
            //Сколько дней в неделю нужно выполнять привычку
            int ndays = h.NDays;
            //Цикл по дням, пробуем распределить в каждый
            foreach (var day in freeIntervals)
            {
                //Цикл по интервалам одного дня
                foreach (var interval in day.Value)
                {
                    int w_days;
                    //Если в данный интервал можно добавить привычку, добавляем
                    if (interval.Start.AddMinutes(h.MinsToComplete, out w_days) <= interval.End && w_days == 0)
                    {
                        //устанавливаем занятость времени
                        h.ActualTimings.Add(new ActualTime(Guid.NewGuid(), interval.Start, interval.Start.AddMinutes(h.MinsToComplete),
                            new WeekDay(day.Key), h.Id));
                        interval.Start = interval.Start.AddMinutes(h.MinsToComplete);
                        ndays--;
                        //выходим из цикла по интервалам дня так как в один день привычка выполняется только один раз
                        break;
                    }
                }
                //Если уже получилось распределить привычку на всю неделю (на указанное количество дней)
                if (ndays == 0)
                    break;
            }
            //Если привычку распределили не полностью, заносим в словарь для отправки в интерфейс
            if (ndays > 0)
                not_distributed[h.Name] = ndays;
            
        }
        return not_distributed;
    }

    public TaskTracker(IEventRepo eventRepo, IHabitRepo habitRepo, IMessageRepo messageRepo,
        ISettingsRepo settingsRepo, IUserRepo userRepo, IShedLoad shedLoader)
    {
        _eventRepo = eventRepo;
        _habitRepo = habitRepo;
        _messageRepo = messageRepo;
        _settingsRepo = settingsRepo;
        _userRepo = userRepo;
        _shedLoader = shedLoader;
    }

    public User? CreateUser(string username, PhoneNumber phone_number, string password)
    {
        var u = new User(Guid.NewGuid(), username, password, phone_number);
        if (_userRepo.Create(u) != 0) return null;
        var s = new UserSettings(Guid.NewGuid(), true, u.Id, []);
        if (_settingsRepo.Create(s) != 0) return null;
        return GetUser(u.Id);
    }

    public User? LogIn(string username, string password)
    {
        var u = _userRepo.Get(username);
        if (u == null) return null;
        if (u.PasswordHash != password) return null;
        return GetUser(u.Id);
    }

    public Tuple<User, Dictionary<string, int>>? ImportNewShedule(Guid user_id)
    {
        User? u = _userRepo.Get(user_id);
        if (u == null) return null;
        //ВАЖНО обновить привычки и события в базе данных
        //В текущей реализации удаляем все привычки, перераспределем и добавляем заново, хорошо бы переделать под Update

        //Загрузка нового расписания (удаляем все старые события, загружаем новые в базу)
        _eventRepo.DeleteEvents(user_id);
        var events = _shedLoader.LoadShedule(user_id);
        foreach (var e in events)
            _eventRepo.Create(e);

        //Получаем текущие привычки
        var habits = _habitRepo.Get(user_id);
        //Удаляем из БД все привычки, но в идеале вместо удаления будет update
        _habitRepo.DeleteHabits(user_id);
        //var settings = _settingsRepo.Get(user_id);

        Dictionary<string, int> no_distributed = DistributeHabits(habits, events);

        /*u.Events = events;
        u.Habits = habits;
        u.Settings = settings;*/

        foreach (var h in habits)
            _habitRepo.Create(h);

        //Пока что возвращаем новый экземпляр user взятый из БД напрямую после всех изменений
        //проверяем точно ли все правильно занеслось в БД
        u = GetUser(user_id);

        return new Tuple<User, Dictionary<string, int>>(u, no_distributed);
    }

    //Для создания привычки от пользователя требуется:
    //название, сколько минутв вып, сколько дней вып, опция времени, список таймингов
    public Tuple<User, Dictionary<string, int>>? AddHabit(Guid user_id, string name, int mins_complete, int ndays, TimeOption op,
        List<Tuple<TimeOnly, TimeOnly>> preffixedtimes)
    { 
        User? u = _userRepo.Get(user_id);
        if (u == null) return null;

        //Получаем текущее расписание
        var events = _eventRepo.Get(user_id);
        //Получаем текущие привычки
        var habits = _habitRepo.Get(user_id);
        Guid hid = Guid.NewGuid();
        List<PrefFixedTime> times = [];
        foreach (var t in preffixedtimes)
            times.Add(new PrefFixedTime(Guid.NewGuid(), t.Item1, t.Item2, hid));
        Habit habit = new Habit(hid, name, mins_complete, op, u.Id, [], times, ndays);
        habits.Add(habit);

        //Удаляем из БД все привычки, но в идеале вместо удаления будет update
        _habitRepo.DeleteHabits(user_id);

        Dictionary<string, int> no_distributed = DistributeHabits(habits, events);

        foreach (var h in habits)
            _habitRepo.Create(h);

        //Пока что возвращаем новый экземпляр user взятый из БД напрямую после всех изменений
        //проверяем точно ли все правильно занеслось в БД
        u = GetUser(user_id);

        return new Tuple<User, Dictionary<string, int>>(u, no_distributed);
    }

    public User? ChangeNotify(Guid user_id)
    {
        var settings = _settingsRepo.Get(user_id);
        if (settings == null) return null;
        settings.NotifyOn = !settings.NotifyOn;
        _settingsRepo.Update(settings);
        return GetUser(user_id);
    }
}
