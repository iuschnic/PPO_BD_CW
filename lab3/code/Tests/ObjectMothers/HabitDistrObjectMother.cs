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

    // Методы для Habit
    /*public static Habit CreateDefaultHabit() =>
        new HabitBuilder()
            .WithName("Утренняя зарядка")
            .WithDuration(30)
            .WithUserName("default_user")
            .Build();

    public static Habit CreateWorkoutHabit() =>
        new HabitBuilder()
            .WithName("Тренировка")
            .WithDuration(60)
            .WithUserName("sportsman")
            .Build();

    public static Habit CreateReadingHabit() =>
        new HabitBuilder()
            .WithName("Чтение")
            .WithDuration(45)
            .WithUserName("book_lover")
            .Build();

    // Методы для Event
    public static Event CreateWorkEvent() =>
        new EventBuilder()
            .WithName("Работа")
            .AsWeeklyEvent(DayOfWeek.Monday)
            .WithTime("09:00", "18:00")
            .WithUserName("office_worker")
            .Build();

    public static Event CreateSleepEvent() =>
        new EventBuilder()
            .WithName("Сон")
            .AsWeeklyEvent(DayOfWeek.Monday)
            .WithTime("23:00", "07:00")
            .WithUserName("default_user")
            .Build();

    public static Event CreateMeetingEvent() =>
        new EventBuilder()
            .WithName("Совещание")
            .AsWeeklyEvent(DayOfWeek.Wednesday)
            .WithTime("14:00", "15:00")
            .WithUserName("manager")
            .Build();

    // Комплексные методы для создания связанных данных
    public static (List<Habit> habits, List<Event> events) CreateBusyDayScenario()
    {
        return (
            new List<Habit> { CreateWorkoutHabit(), CreateReadingHabit() },
            new List<Event> { CreateWorkEvent(), CreateMeetingEvent() }
        );
    }

    public static (List<Habit> habits, List<Event> events) CreateFreeDayScenario()
    {
        return (
            new List<Habit> { CreateDefaultHabit() },
            new List<Event> { CreateSleepEvent() }
        );
    }*/
}
