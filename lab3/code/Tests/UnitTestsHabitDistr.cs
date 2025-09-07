using Domain;
using Domain.Models;
using Types;
using Tests.ObjectMothers;
using Allure.Xunit.Attributes;

namespace Tests.HabitDistr;

public class UnitTestsHabitDistr
{
    [Fact]
    [AllureStory("Распределение привычек по известному расписанию")]
    [AllureFeature("Привычки с безразличным временем")]
    [AllureDescription("Тест распределения одной привычки с безразличным временем выполнения на два дня," +
        " только один из которых подходит")]
    public void Test1()
    {
        string userName = "egor";
        List<Habit> habits = [];
        List<Event> events = [];
        HabitDistributor distr = new();
        var h = HabitDistrMother.Habit().WithUserName(userName).WithCountInWeek(2).Build();
        events.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        habits.Add(h);

        var undistr = distr.DistributeHabits(habits, events);

        Assert.Single(undistr);
        Assert.Single(h.ActualTimings);
        Assert.Equal(new TimeOnly(18, 0, 0), h.ActualTimings[0].Start);
        Assert.Equal(new TimeOnly(19, 0, 0), h.ActualTimings[0].End);
    }
    [Fact]
    [AllureStory("Распределение привычек по известному расписанию")]
    [AllureFeature("Привычки с фиксированным временем")]
    [AllureDescription("Тест распределения одной привычки с фиксированным временем выполнения на два дня," +
        " только один из которых подходит")]
    public void Test2()
    {
        string userName = "egor";
        List<Habit> habits = [];
        List<Event> events = [];
        HabitDistributor distr = new();
        var guid = Guid.NewGuid();
        var h = HabitDistrMother.Habit()
            .WithId(guid)
            .WithUserName(userName)
            .WithOption(TimeOption.Fixed)
            .WithMinsToComplete(30)
            .WithPrefFixedTiming(new TimeOnly(7, 0, 0), new TimeOnly(9, 0, 0))
            .WithCountInWeek(2)
            .Build();
        events.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        habits.Add(h);

        var undistr = distr.DistributeHabits(habits, events);

        Assert.Single(undistr);
        Assert.Single(h.ActualTimings);
        Assert.Equal(new TimeOnly(8, 0, 0), h.ActualTimings[0].Start);
        Assert.Equal(new TimeOnly(8, 30, 0), h.ActualTimings[0].End);
    }
    [Fact]
    [AllureStory("Распределение привычек по известному расписанию")]
    [AllureFeature("Привычки с предпочтительным временем")]
    [AllureDescription("Тест распределения одной привычки с предпочтительным временем выполнения на два дня," +
        " только один из которых подходит")]
    public void Test3()
    {
        string userName = "egor";
        List<Habit> habits = [];
        List<Event> events = [];
        HabitDistributor distr = new();
        var guid = Guid.NewGuid();
        var h = HabitDistrMother.Habit()
            .WithId(guid)
            .WithUserName(userName)
            .WithOption(TimeOption.Preffered)
            .WithMinsToComplete(30)
            .WithPrefFixedTiming(new TimeOnly(7, 0, 0), new TimeOnly(9, 0, 0))
            .WithCountInWeek(2)
            .Build();
        events.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        habits.Add(h);

        var undistr = distr.DistributeHabits(habits, events);

        Assert.Single(undistr);
        Assert.Single(h.ActualTimings);
        Assert.Equal(new TimeOnly(8, 0, 0), h.ActualTimings[0].Start);
        Assert.Equal(new TimeOnly(8, 30, 0), h.ActualTimings[0].End);
    }
    [Fact]
    [AllureStory("Распределение привычек по известному расписанию")]
    [AllureFeature("Привычки с предпочтительным временем")]
    [AllureDescription("Тест распределения одной привычки с предпочтительным временем, которая не влезает в расписание")]
    public void Test4()
    {
        string userName = "egor";
        List<Habit> habits = [];
        List<Event> events = [];
        HabitDistributor distr = new();
        var guid = Guid.NewGuid();
        var h = HabitDistrMother.Habit()
            .WithId(guid)
            .WithUserName(userName)
            .WithOption(TimeOption.Preffered)
            .WithMinsToComplete(500)
            .WithPrefFixedTiming(new TimeOnly(7, 0, 0), new TimeOnly(9, 0, 0))
            .Build();
        events.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        habits.Add(h);

        var undistr = distr.DistributeHabits(habits, events);

        Assert.Single(undistr);
        Assert.Empty(h.ActualTimings);
    }
    [Fact]
    [AllureStory("Распределение привычек по известному расписанию")]
    [AllureFeature("Привычки с безразличным временем")]
    [AllureDescription("Тест распределения одной привычки с безразличным временем, которая не влезает в расписание")]
    public void Test5()
    {
        string userName = "egor";
        List<Habit> habits = [];
        List<Event> events = [];
        HabitDistributor distr = new();
        var guid = Guid.NewGuid();
        var h = HabitDistrMother.Habit()
            .WithId(guid)
            .WithUserName(userName)
            .WithOption(TimeOption.NoMatter)
            .WithMinsToComplete(500)
            .WithPrefFixedTiming(new TimeOnly(7, 0, 0), new TimeOnly(9, 0, 0))
            .Build();
        events.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        habits.Add(h);

        var undistr = distr.DistributeHabits(habits, events);

        Assert.Single(undistr);
        Assert.Empty(h.ActualTimings);
    }
    [Fact]
    [AllureStory("Распределение привычек по известному расписанию")]
    [AllureFeature("Привычки с фиксированным временем")]
    [AllureDescription("Тест распределения одной привычки с фиксированным временем, которая влезает, но не по своему времени")]
    public void Test6()
    {
        string userName = "egor";
        List<Habit> habits = [];
        List<Event> events = [];
        HabitDistributor distr = new();
        var guid = Guid.NewGuid();
        var h = HabitDistrMother.Habit()
            .WithId(guid)
            .WithUserName(userName)
            .WithOption(TimeOption.Fixed)
            .WithMinsToComplete(10)
            .WithPrefFixedTiming(new TimeOnly(9, 0, 0), new TimeOnly(17, 0, 0))
            .Build();
        events.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        habits.Add(h);

        var undistr = distr.DistributeHabits(habits, events);

        Assert.Single(undistr);
        Assert.Empty(h.ActualTimings);
    }
    [Fact]
    [AllureStory("Распределение привычек по известному расписанию")]
    [AllureFeature("Привычки с предпочтительным временем")]
    [AllureDescription("Тест распределения одной привычки с предпочтительным временем, которая влезает, но не по своему времени")]
    public void Test7()
    {
        string userName = "egor";
        List<Habit> habits = [];
        List<Event> events = [];
        HabitDistributor distr = new();
        var guid = Guid.NewGuid();
        var h = HabitDistrMother.Habit()
            .WithId(guid)
            .WithUserName(userName)
            .WithOption(TimeOption.Preffered)
            .WithMinsToComplete(10)
            .WithPrefFixedTiming(new TimeOnly(9, 0, 0), new TimeOnly(17, 0, 0))
            .Build();
        events.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        habits.Add(h);

        var undistr = distr.DistributeHabits(habits, events);

        Assert.Empty(undistr);
        Assert.Single(h.ActualTimings);
        Assert.Equal(new TimeOnly(8, 0, 0), h.ActualTimings[0].Start);
        Assert.Equal(new TimeOnly(8, 10, 0), h.ActualTimings[0].End);
    }
    [Fact]
    [AllureStory("Распределение привычек по известному расписанию")]
    [AllureFeature("Привычки с бфиксированным временем")]
    [AllureDescription("Тест распределения одной привычки с предпочтительным временем," +
        " которая не влезает в первый день, но влезает во второй")]
    public void Test8()
    {
        string userName = "egor";
        List<Habit> habits = [];
        List<Event> events = [];
        HabitDistributor distr = new();
        var guid = Guid.NewGuid();
        var h = HabitDistrMother.Habit()
            .WithId(guid)
            .WithUserName(userName)
            .WithOption(TimeOption.Preffered)
            .WithMinsToComplete(40)
            .WithPrefFixedTiming(new TimeOnly(7, 0, 0), new TimeOnly(9, 0, 0))
            .Build();
        events.AddRange(HabitDistrMother.CustomDayShedule(userName, DayOfWeek.Monday, 
            [new("00:00:00", "08:00:00"),
            new("08:30:00", "09:00:00"),
            new("09:00:00", "18:00:00"),
            new("23:00:00", "23:59:59")]));
        events.AddRange(HabitDistrMother.CustomDayShedule(userName, DayOfWeek.Tuesday,
            [new("00:00:00", "08:00:00"),
            new("08:50:00", "09:00:00"),
            new("09:00:00", "18:00:00"),
            new("23:00:00", "23:59:59")]));
        events.Add(HabitDistrMother.WeeklyFiller(userName, DayOfWeek.Wednesday));
        events.Add(HabitDistrMother.WeeklyFiller(userName, DayOfWeek.Thursday));
        events.Add(HabitDistrMother.WeeklyFiller(userName, DayOfWeek.Friday));
        events.Add(HabitDistrMother.WeeklyFiller(userName, DayOfWeek.Saturday));
        events.Add(HabitDistrMother.WeeklyFiller(userName, DayOfWeek.Sunday));
        habits.Add(h);

        var undistr = distr.DistributeHabits(habits, events);

        Assert.Empty(undistr);
        Assert.Single(h.ActualTimings);
        Assert.Equal(DayOfWeek.Tuesday, h.ActualTimings[0].Day);
        Assert.Equal(new TimeOnly(8, 0, 0), h.ActualTimings[0].Start);
        Assert.Equal(new TimeOnly(8, 40, 0), h.ActualTimings[0].End);
    }
}
