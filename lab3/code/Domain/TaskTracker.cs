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

    /*Функция по guid получает всю информацию о пользователе, его привычках, событиях расписания и настройках*/
    private User? GetUser(Guid user_id)
    {
        User? user = _userRepo.TryGet(user_id);
        if (user == null) return null;
        UserSettings? settings = _settingsRepo.TryGet(user_id);
        if (settings == null) return null;
        List<Habit> habits = _habitRepo.Get(user_id);
        if (habits == null) habits = [];
        List<Event> events = _eventRepo.Get(user_id);
        if (events == null) events = [];
        user.Habits = habits;
        user.Events = events;
        user.Settings = settings;
        return user;
    }

    /*Функция по списку интервалов занятости получает список свободных временных интервалов*/
    private List<TimeInterval> GetFreeIntervals(List<TimeInterval> events)
    {
        if (events.Count == 0)
            return new List<TimeInterval> { new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59)) };
        var sortedEvents = events.OrderBy(e => e.Start).ToList();
        List<TimeInterval> freeIntervals = new List<TimeInterval>();
        TimeOnly previousEnd = new TimeOnly(0, 0, 0);
        if (sortedEvents[0].Start > previousEnd)
            freeIntervals.Add(new TimeInterval(previousEnd, sortedEvents[0].Start));

        for (int i = 1; i < sortedEvents.Count; i++)
        {
            TimeOnly currentStart = sortedEvents[i].Start;
            TimeOnly lastEnd = sortedEvents[i - 1].End;
            if (currentStart > lastEnd)
                freeIntervals.Add(new TimeInterval(lastEnd, currentStart));
        }

        TimeOnly endOfDay = new TimeOnly(23, 59, 59);
        if (sortedEvents.Last().End < endOfDay)
            freeIntervals.Add(new TimeInterval(sortedEvents.Last().End, endOfDay));
        return freeIntervals;
    }

    /*Функуция по двум спискам временных интервалов получает список-пересечение (временные интервалы принадлежащие обоим спискам)*/
    public static List<TimeInterval> FindIntersection(List<TimeInterval> intervals1, List<TimeInterval> intervals2)
    {
        var sorted1 = intervals1.OrderBy(x => x.Start).ToList();
        var sorted2 = intervals2.OrderBy(x => x.Start).ToList();
        List<TimeInterval> result = [];
        int i = 0, j = 0;
        while (i < sorted1.Count && j < sorted2.Count)
        {
            // Находим позднее начало и раннее окончание для текущих интервалов
            TimeOnly maxStart = sorted1[i].Start > sorted2[j].Start ? sorted1[i].Start : sorted2[j].Start;
            TimeOnly minEnd = sorted1[i].End < sorted2[j].End ? sorted1[i].End : sorted2[j].End;

            // Если есть пересечение (maxStart <= minEnd), добавляем его в результат
            if (maxStart <= minEnd)
                result.Add(new TimeInterval(maxStart, minEnd));

            // Переходим к следующему интервалу в том списке, где текущий интервал заканчивается раньше
            if (sorted1[i].End < sorted2[j].End)
                i++;
            else
                j++;
        }
        return result;
    }

    /*Функция конвертирует список предпочтительного/фиксированного времени для привычки
     в список временных интервалов(в другой тип)*/
    private List<TimeInterval> HabitToTimeIntervals(Habit h)
    {
        List<TimeInterval> timings = [];
        if (h.PrefFixedTimings == null || h.PrefFixedTimings.Count == 0)
        {
            timings.Add(new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59)));
            return timings;
        }
        foreach (var t in h.PrefFixedTimings)
            timings.Add(new TimeInterval(t.Start, t.End));
        return timings;
    }

    /*Функция из списка временных интервалов вычитает заданный интервал
     Предполагается что вычитаемый интервал принадлежит одному из интервалов списка!
     Список интервалов сортируется на выходе по Start*/
    private bool SubstractInterval(List<TimeInterval> intervals, TimeInterval substracted)
    {
        //Находим интервал, требующий модификации (вычитания из него заданного интервала)
        TimeInterval? modified = intervals.Find(i => i.End >= substracted.End && i.Start <= substracted.Start);
        if (modified == null)
            return false;
        if (modified.End == substracted.End)
        {
            modified.End = substracted.Start;
            return true;
        }
        if (modified.Start == substracted.Start)
        {
            modified.Start = substracted.End;
            return true;
        }
        //Вычитаемый строго внутри модифицируемого
        else
        {
            //Интервал разбивается на два, добавляется один новый и модифицируется старый
            intervals.Add(new TimeInterval(substracted.End, modified.End));
            modified.End = substracted.Start;
            return true;
        }
    }

    /*Функция ДОраспределяет(не удаляет ранее заданные интервалы выполнения)
     привычки по свободным временным интервалам недели с учетом жестко фиксированных разрешенных интервалов выполнения
     Принимает на вход список привычек и словарь вида <День - список свободных интервалов>
     Возвращает список новых экземпляров привычек, которые не были распределены полностью или частично*/
    private List<Habit> DistributeWithFixedTime(List<Habit> habits, Dictionary<WeekDay, List<TimeInterval>> freeIntervals)
    {
        List<Habit> undistributed = [];
        foreach (var h in habits)
        {
            //ВАЖНО
            int ndays = h.NDays - h.ActualTimings.Count;
            List<TimeInterval> fixedTimings = HabitToTimeIntervals(h);
            foreach (var day in freeIntervals)
            {
                /*Привычку можно распределить только на фиксированное время заданное пользователем
                поэтому ищем пересечение свободных интервалов дня и фиксированных интервалов (их задавал пользователь)
                и распределять привычку можем только в эти разрешенные интервалы*/
                List<TimeInterval> allowedIntervals = FindIntersection(day.Value, fixedTimings);
                foreach (var interval in allowedIntervals)
                {
                    int w_days;
                    //Если в данный интервал можно добавить привычку, добавляем
                    if (interval.Start.AddMinutes(h.MinsToComplete, out w_days) <= interval.End && w_days == 0)
                    {
                        h.ActualTimings.Add(new ActualTime(Guid.NewGuid(), interval.Start, interval.Start.AddMinutes(h.MinsToComplete),
                            day.Key, h.Id));
                        //устанавливаем занятость времени вычитая полученный интервал из свободных интервалов на данный день
                        SubstractInterval(day.Value, new TimeInterval(interval.Start, interval.Start.AddMinutes(h.MinsToComplete)));
                        ndays--;
                        //привычка выполняется не более 1 раза в день
                        break;
                    }
                }
                if (ndays == 0)
                    break;
            }
            //Не полностью распределенные привычки нужно вернуть
            if (ndays > 0)
                undistributed.Add(new Habit(h.Id, h.Name, h.MinsToComplete, h.Option, h.UserID, [], [], ndays));
        }
        return undistributed;
    }

    /*Функция ДОраспределяет(не удаляет ранее заданные интервалы выполнения)
     привычки по свободным временным интервалам недели без учета предпочтений пользователя
     Принимает на вход список привычек и словарь вида <День - список свободных интервалов>
     Ввозвращает список новых экземпляров привычек, которые не были распределены полностью или частично*/
    private List<Habit> DistributeWithNoMatterTime(List<Habit> habits, Dictionary<WeekDay, List<TimeInterval>> freeIntervals)
    {
        List<Habit> undistributed = [];
        foreach (var h in habits)
        {
            //ВАЖНО
            int ndays = h.NDays - h.ActualTimings.Count;
            foreach (var day in freeIntervals)
            {
                foreach (var interval in day.Value)
                {
                    int w_days;
                    //Если в данный интервал можно добавить привычку, добавляем
                    if (interval.Start.AddMinutes(h.MinsToComplete, out w_days) <= interval.End && w_days == 0)
                    {
                        //устанавливаем занятость времени
                        h.ActualTimings.Add(new ActualTime(Guid.NewGuid(), interval.Start, interval.Start.AddMinutes(h.MinsToComplete),
                            day.Key, h.Id));
                        interval.Start = interval.Start.AddMinutes(h.MinsToComplete);
                        ndays--;
                        //привычка выполняется не более 1 раза в день
                        break;
                    }
                }
                if (ndays == 0)
                    break;
            }
            //Не полностью распределенные привычки нужно вернуть
            if (ndays > 0)
                undistributed.Add(new Habit(h.Id, h.Name, h.MinsToComplete, h.Option, h.UserID, [], [], ndays));
        }
        return undistributed;
    }

    /*Функция распределения привычек по расписанию
     Получает на вход списки привычек и событий расписания, изменяет привычки в полученном списке
     добавляя им реальное время в которое они могут быть выполнены при текущем расписании
     Возвращает список новых экземпляров привычек, которые не были распределены полностью или частично*/
    private List<Habit> DistributeHabits(List<Habit> habits, List<Event> events)
    {
        foreach (var h in habits)
            h.ActualTimings.Clear();
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
        Dictionary<WeekDay, List<TimeInterval>> freeIntervals = [];
        freeIntervals[new WeekDay("Monday")] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[new WeekDay("Tuesday")] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[new WeekDay("Wednesday")] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[new WeekDay("Thursday")] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[new WeekDay("Friday")] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[new WeekDay("Saturday")] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[new WeekDay("Sunday")] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        foreach (var ev in eventsIntervals)
            freeIntervals[ev.Key] = GetFreeIntervals(ev.Value);

        List<Habit> undistributed = [];
        /*TODO Различное распределение привычек с разными приоритетами*/
        //Формирование словаря привычек по приоритету - фиксированное время, предпочитаемое время, безразличное время
        Dictionary<string, List<Habit>> habitsByPrio = [];
        foreach (var h in habits)
        {
            if (!habitsByPrio.ContainsKey(h.Option.StringTimeOption))
                habitsByPrio[h.Option.StringTimeOption] = [];
            habitsByPrio[h.Option.StringTimeOption].Add(h);
        }
        //Распределение привычек с фиксированным временем
        if (habitsByPrio.ContainsKey("Fixed"))
            undistributed.AddRange(DistributeWithFixedTime(habitsByPrio["Fixed"], freeIntervals));
        //Распределение привычек с предпочтительным временем
        if (habitsByPrio.ContainsKey("Preffered"))
        {
            //Сначала распределяем по-максимуму на предпочитаемое время
            var undistr_pref = DistributeWithFixedTime(habitsByPrio["Preffered"], freeIntervals);
            //По остаточному принципу распределяем остальное
            if (undistr_pref.Count != 0)
                undistributed.AddRange(DistributeWithNoMatterTime(habitsByPrio["Preffered"], freeIntervals));
        }
        //Распределение привычек с безразличным временем
        if (habitsByPrio.ContainsKey("NoMatter"))
            undistributed.AddRange(DistributeWithNoMatterTime(habitsByPrio["NoMatter"], freeIntervals));
        return undistributed;
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

    /*Функция создания пользователя по имени пользователя, номеру телефона и паролю
     Возвращает полную информацию о пользователе если такого пользователя в системе еще нет
     иначе возвращает null*/
    public User? CreateUser(string username, PhoneNumber phone_number, string password)
    {
        //Возможно userepo лучше объединить с settingsrepo так как не существует пользователя без настроек и наоборот
        var u = new User(Guid.NewGuid(), username, password, phone_number);
        if (!_userRepo.TryCreate(u)) return null;
        var s = new UserSettings(Guid.NewGuid(), true, u.Id, []);
        if (!_settingsRepo.TryCreate(s))
        {
            _userRepo.Delete(u.Id);
            return null;
        }
        return GetUser(u.Id);
    }

    /*Функция входа в аккаунт пользователя по имени пользователя и паролю
     Возвращает полную информацию о пользователе если такой пользователь существует и пароль верен
     иначе возвращает null*/
    public User? LogIn(string username, string password)
    {
        var u = _userRepo.TryGet(username);
        if (u == null) return null;
        if (u.PasswordHash != password) return null;
        return GetUser(u.Id);
    }

    /*Функция импорта нового расписания для пользователя с идентификатором user_id
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>>? ImportNewShedule(Guid user_id, string path)
    {
        //В текущей реализации удаляем все привычки, перераспределем и добавляем заново, хорошо бы переделать под Update
        User? u = _userRepo.TryGet(user_id);
        if (u == null) return null;

        var events = _shedLoader.LoadShedule(user_id, path);
        var habits = _habitRepo.Get(user_id);
        List<Habit> no_distributed = DistributeHabits(habits, events);

        _eventRepo.DeleteEvents(user_id);
        _eventRepo.CreateMany(events);
        _habitRepo.DeleteHabits(user_id);
        _habitRepo.CreateMany(habits);

        u = GetUser(user_id);
        return new Tuple<User, List<Habit>>(u, no_distributed);
    }

    /*Функция добавления привычки для пользователя с идентификатором user_id
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>>? AddHabit(Guid user_id, string name, int mins_complete, int ndays, TimeOption op,
        List<Tuple<TimeOnly, TimeOnly>> preffixedtimes)
    {
        //В текущей реализации удаляем все привычки, перераспределем и добавляем заново, хорошо бы переделать под Update
        User? u = _userRepo.TryGet(user_id);
        if (u == null) return null;

        var events = _eventRepo.Get(user_id);
        var habits = _habitRepo.Get(user_id);
        Guid hid = Guid.NewGuid();
        List<PrefFixedTime> times = [];
        foreach (var t in preffixedtimes)
            times.Add(new PrefFixedTime(Guid.NewGuid(), t.Item1, t.Item2, hid));
        Habit habit = new Habit(hid, name, mins_complete, op, u.Id, [], times, ndays);
        habits.Add(habit);
        List<Habit> no_distributed = DistributeHabits(habits, events);

        _eventRepo.DeleteEvents(user_id);
        _eventRepo.CreateMany(events);
        _habitRepo.DeleteHabits(user_id);
        _habitRepo.CreateMany(habits);

        u = GetUser(user_id);
        return new Tuple<User, List<Habit>>(u, no_distributed);
    }

    /*Функция удаления привычки для пользователя с идентификатором user_id
     Возвращает кортеж из информации о пользователе и информации о нераспределенных привычках*/
    public Tuple<User, List<Habit>>? DeleteHabit(Guid user_id, string name)
    {
        User? u = _userRepo.TryGet(user_id);
        if (u == null) return null;

        var events = _eventRepo.Get(user_id);
        var habits = _habitRepo.Get(user_id);
        habits.RemoveAll(h => h.Name == name);

        List<Habit> no_distributed = DistributeHabits(habits, events);

        _eventRepo.DeleteEvents(user_id);
        _eventRepo.CreateMany(events);
        _habitRepo.DeleteHabits(user_id);
        _habitRepo.CreateMany(habits);

        u = GetUser(user_id);
        return new Tuple<User, List<Habit>>(u, no_distributed);
    }

    /*Функция изменения флага разрешения отправки сообщения для пользователя с идентификатором user_id
     Возвращает кортеж информацию о пользователе если он существует*/
    public User? ChangeNotify(Guid user_id, bool value)
    {
        var settings = _settingsRepo.TryGet(user_id);
        if (settings == null) return null;
        settings.NotifyOn = value;
        _settingsRepo.Update(settings);
        return GetUser(user_id);
    }
}
