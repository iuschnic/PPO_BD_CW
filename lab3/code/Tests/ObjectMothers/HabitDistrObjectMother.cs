using Tests.Builders;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Types;
namespace Tests.ObjectMothers;

public static class HabitDistrMother
{
    public static HabitBuilder Habit() => new();

    public static EventBuilder Event() => new();
    public static Event WeeklyFiller(string userName, DayOfWeek day) =>
        new EventBuilder()
        .WithUserName(userName)
        .WithTime("00:00:00", "23:59:59")
        .WithName("Заглушка")
        .AsWeeklyEvent(day)
        .Build();
    public static List<Event> FullWeekFillerExceptDay(string userName, DayOfWeek exceptDay)
    {
        var list = new List<Event>();
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            if (day != exceptDay)
                list.Add(new EventBuilder()
                        .WithUserName(userName)
                        .WithTime("00:00:00", "23:59:59")
                        .WithName("Заглушка")
                        .AsWeeklyEvent(day)
                        .Build());
        }
        return list;
    }
    public static List<Event> DefaultDayShedule(string userName, DayOfWeek day)
    {
        var list = new List<Event>();
        list.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0),
            userName, EventOption.EveryWeek, DayOfWeek.Monday, null));
        list.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0),
            userName, EventOption.EveryWeek, DayOfWeek.Monday, null));
        list.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0),
            userName, EventOption.EveryWeek, DayOfWeek.Monday, null));
        list.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59),
            userName, EventOption.EveryWeek, DayOfWeek.Monday, null));
        return list;
    }

    public static List<Event> CustomDayShedule(string userName, DayOfWeek day, List<Tuple<string, string>> timings)
    {
        var list = new List<Event>();
        foreach (var item in timings)
            list.Add(new EventBuilder().WithUserName(userName).WithTime(item.Item1, item.Item2).AsWeeklyEvent(day).Build());
        return list;
    }
}
