using Domain.Models;
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

public class HabitDistributor : IHabitDistributor
{
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

    /*Функция по двум спискам временных интервалов получает список-пересечение (временные интервалы принадлежащие обоим спискам)*/
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
    private List<Habit> DistributeWithFixedTime(List<Habit> habits, Dictionary<DayOfWeek, List<TimeInterval>> freeIntervals)
    {
        List<Habit> undistributed = [];
        foreach (var h in habits)
        {
            //ВАЖНО
            int ndays = h.NDays - h.ActualTimings.Count;
            List<TimeInterval> fixedTimings = HabitToTimeIntervals(h);
            foreach (var day in freeIntervals)
            {
                //привычка выполняется не более 1 раза в день
                if (h.ActualTimings.Exists(el => el.Day == day.Key))
                    continue;
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
            if (ndays > 0)
                undistributed.Add(new Habit(h.Id, h.Name, h.MinsToComplete, h.Option, h.UserNameID, [], [], ndays));
        }
        return undistributed;
    }

    /*Функция ДОраспределяет(не удаляет ранее заданные интервалы выполнения)
     привычки по свободным временным интервалам недели без учета предпочтений пользователя
     Принимает на вход список привычек и словарь вида <День - список свободных интервалов>
     Ввозвращает список новых экземпляров привычек, которые не были распределены полностью или частично*/
    private List<Habit> DistributeWithNoMatterTime(List<Habit> habits, Dictionary<DayOfWeek, List<TimeInterval>> freeIntervals)
    {
        List<Habit> undistributed = [];
        foreach (var h in habits)
        {
            //ВАЖНО
            int ndays = h.NDays - h.ActualTimings.Count;
            foreach (var day in freeIntervals)
            {
                //привычка выполняется не более 1 раза в день
                if (h.ActualTimings.Exists(el => el.Day == day.Key))
                    continue;
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
            if (ndays > 0)
                undistributed.Add(new Habit(h.Id, h.Name, h.MinsToComplete, h.Option, h.UserNameID, [], [], ndays));
        }
        return undistributed;
    }
    public List<Habit> DistributeHabits(List<Habit> habits, List<Event> events)
    {
        foreach (var h in habits)
            h.ActualTimings.Clear();
        //Получаем списки интервалов занятости из расписания для каждого дня
        Dictionary<DayOfWeek, List<TimeInterval>> eventsIntervals = [];
        foreach (var ev in events)
        {
            if (!eventsIntervals.ContainsKey(ev.Day))
                eventsIntervals[ev.Day] = [];
            var interval = new TimeInterval(ev.Start, ev.End);
            eventsIntervals[ev.Day].Add(interval);
        }
        //Получаем списки интервалов свободного времени в расписании для каждого дня
        Dictionary<DayOfWeek, List<TimeInterval>> freeIntervals = [];
        freeIntervals[DayOfWeek.Monday] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[DayOfWeek.Tuesday] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[DayOfWeek.Wednesday] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[DayOfWeek.Thursday] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[DayOfWeek.Friday] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[DayOfWeek.Saturday] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        freeIntervals[DayOfWeek.Sunday] = [new TimeInterval(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59))];
        foreach (var ev in eventsIntervals)
            freeIntervals[ev.Key] = GetFreeIntervals(ev.Value);

        List<Habit> undistributed = [];
        //Формирование словаря привычек по приоритету - фиксированное время, предпочитаемое время, безразличное время
        Dictionary<TimeOption, List<Habit>> habitsByPrio = [];
        foreach (var h in habits)
        {
            if (!habitsByPrio.ContainsKey(h.Option))
                habitsByPrio[h.Option] = [];
            //Если пользователь не задал конкретные промежутки времени - распределяем как привычку с безразличным временем
            if (h.PrefFixedTimings.Count == 0)
                habitsByPrio[TimeOption.NoMatter].Add(h);
            else
                habitsByPrio[h.Option].Add(h);
        }
        //Распределение привычек с фиксированным временем
        if (habitsByPrio.ContainsKey(TimeOption.Fixed))
            undistributed.AddRange(DistributeWithFixedTime(habitsByPrio[TimeOption.Fixed], freeIntervals));
        //Распределение привычек с предпочтительным временем
        if (habitsByPrio.ContainsKey(TimeOption.Preffered))
        {
            //Сначала распределяем по-максимуму на предпочитаемое время
            var undistr_pref = DistributeWithFixedTime(habitsByPrio[TimeOption.Preffered], freeIntervals);
            //По остаточному принципу распределяем остальное
            if (undistr_pref.Count != 0)
                undistributed.AddRange(DistributeWithNoMatterTime(habitsByPrio[TimeOption.Preffered], freeIntervals));
        }
        //Распределение привычек с безразличным временем
        if (habitsByPrio.ContainsKey(TimeOption.NoMatter))
            undistributed.AddRange(DistributeWithNoMatterTime(habitsByPrio[TimeOption.NoMatter], freeIntervals));
        return undistributed;
    }
}
