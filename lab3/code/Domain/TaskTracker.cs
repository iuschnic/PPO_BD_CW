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

    private void SpreadHabits(List<Habit> habits, List<Event> events)
    {
        //Получаем списки интервалов занятости из расписания для каждого дня
        Dictionary<WeekDay, List<TimeInterval>> eventsIntervals = [];
        foreach (var ev in events)
        {
            var interval = new TimeInterval(ev.Start, ev.End);
            eventsIntervals[ev.Day].Add(interval);
        }
        //Получаем списки интервалов свободного времени в расписании для каждого дня
        Dictionary<string, List<TimeInterval>> freeIntervals = [];
        foreach (var ev in eventsIntervals)
            freeIntervals[ev.Key.StringDay] = GetFreeIntervals(ev.Value);
        //Получаем списки привычек по приоритетам
        /*Dictionary<string, Habit> option_habits = [];
        foreach (var h in habits)
            option_habits[h.Option.StringTimeOption] = h;*/
        /*TODO Различное распределение привычек с разными приоритетами*/
        //Распределяем каждую привычку
        foreach (var h in habits)
        {
            //Цикл по дням, пробуем распределить в каждый
            foreach (var day in freeIntervals)
            {
                bool flag = false;
                //Цикл по интервалам одного дня
                foreach (var interval in day.Value)
                {
                    //Если в данный интервал можно добавить привычку, добавляем
                    if (interval.Start.AddMinutes(h.MinsToComplete) < interval.End)
                    {
                        //устанавливаем занятость времени
                        interval.Start = interval.Start.AddMinutes(h.MinsToComplete);

                        flag = true;
                        break;
                    }
                }
                if (flag)
                    break;
            }
        }
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
        return GetUser(u.Id);
    }

    public User? ImportNewShedule(Guid user_id)
    {
        User? u = _userRepo.Get(user_id);
        if (u == null) return null;
        var events = _shedLoader.LoadShedule(u.Id);
        var habits = _habitRepo.Get(user_id);

    }
}
