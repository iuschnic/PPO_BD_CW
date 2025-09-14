using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Storage.Models;
using Storage.PostgresStorageAdapters;
using Tests.ObjectMothers;
using Types;
using Allure.Xunit.Attributes;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Tests.UnitTests;

public class UnitTestsHabitRepo
{
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест получения привычек пользователя когда он существует")]
    public void TryGetUserExists()
    {
        var start1 = new TimeOnly(8, 0);
        var end1 = new TimeOnly(8, 30);
        var start2 = new TimeOnly(10, 0);
        var end2 = new TimeOnly(10, 30);
        var timeToComplete = 30;
        var userName = "test";
        var habitName = "test";
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var testUser = new DBUser(userName, "+79161648345", "test");
        mockUsersDbSet.Setup(d => d.Find(userName))
                     .Returns(testUser);
        var habitId = Guid.NewGuid();
        var dbHabit = new DBHabit(habitId, habitName, timeToComplete, TimeOption.Fixed, userName, 3);
        var actualTimings = new List<DBActualTime>
        {
            new(Guid.NewGuid(), start1, end1, DayOfWeek.Monday, habitId),
            new(Guid.NewGuid(), start2, end2, DayOfWeek.Wednesday, habitId)
        };
        var prefFixedTimings = new List<DBPrefFixedTime>
        {
            new(Guid.NewGuid(), start1, end1, habitId),
            new(Guid.NewGuid(), start2, end2, habitId)
        };
        dbHabit.ActualTimings = actualTimings;
        dbHabit.PrefFixedTimings = prefFixedTimings;
        var dbHabits = new List<DBHabit> { dbHabit }.AsQueryable();
        SetupMockDbSet(mockHabitsDbSet, dbHabits);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Habits)
                    .Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes)
                    .Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes)
                    .Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryGet(userName);

        Assert.NotNull(result);
        Assert.Single(result);
        var habit = result[0];
        Assert.Equal(habitName, habit.Name);
        Assert.Equal(timeToComplete, habit.MinsToComplete);
        Assert.Equal(userName, habit.UserNameID);
        Assert.Equal(2, habit.ActualTimings.Count);
        Assert.Equal(2, habit.PrefFixedTimings.Count);
        Assert.Contains(habit.ActualTimings, at =>
            at.Start == start1 && at.End == end1);
        Assert.Contains(habit.PrefFixedTimings, pf =>
            pf.Start == start2 && pf.End == end2);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест получения привычек пользователя когда он существует")]
    public async Task TryGetAsyncUserExists()
    {
        var start1 = new TimeOnly(8, 0);
        var end1 = new TimeOnly(8, 30);
        var start2 = new TimeOnly(10, 0);
        var end2 = new TimeOnly(10, 30);
        var timeToComplete = 30;
        var userName = "test";
        var habitName = "test";
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var testUser = new DBUser(userName, "+79161648345", "test");
        mockUsersDbSet.Setup(d => d.FindAsync(userName))
                     .ReturnsAsync(testUser);
        var habitId = Guid.NewGuid();
        var dbHabit = new DBHabit(habitId, habitName, timeToComplete, TimeOption.Fixed, userName, 3);
        var actualTimings = new List<DBActualTime>
        {
            new(Guid.NewGuid(), start1, end1, DayOfWeek.Monday, habitId),
            new(Guid.NewGuid(), start2, end2, DayOfWeek.Wednesday, habitId)
        };
        var prefFixedTimings = new List<DBPrefFixedTime>
        {
            new(Guid.NewGuid(), start1, end1, habitId),
            new(Guid.NewGuid(), start2, end2, habitId)
        };
        dbHabit.ActualTimings = actualTimings;
        dbHabit.PrefFixedTimings = prefFixedTimings;
        var dbHabits = new List<DBHabit> { dbHabit }.AsQueryable();
        SetupMockDbSetForAsync(mockHabitsDbSet, dbHabits);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Habits)
                    .Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes)
                    .Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes)
                    .Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryGetAsync(userName);

        Assert.NotNull(result);
        Assert.Single(result);
        var habit = result[0];
        Assert.Equal(habitName, habit.Name);
        Assert.Equal(timeToComplete, habit.MinsToComplete);
        Assert.Equal(userName, habit.UserNameID);
        Assert.Equal(2, habit.ActualTimings.Count);
        Assert.Equal(2, habit.PrefFixedTimings.Count);
        Assert.Contains(habit.ActualTimings, at =>
            at.Start == start1 && at.End == end1);
        Assert.Contains(habit.PrefFixedTimings, pf =>
            pf.Start == start2 && pf.End == end2);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест получения привычек пользователя когда он не существует")]
    public void TryGetUserNotExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "еуые";
        mockUsersDbSet.Setup(d => d.Find(userName)).Returns((DBUser?)null);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryGet(userName);

        Assert.Null(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест получения привычек пользователя когда он не существует")]
    public async Task TryGetAsyncUserNotExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "еуые";
        mockUsersDbSet.Setup(d => d.FindAsync(userName)).ReturnsAsync((DBUser?)null);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryGetAsync(userName);

        Assert.Null(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания привычки пользователя которой еще не существует")]
    public void TryCreateHabitNotExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var habit = TaskTrackerMother.Habit().WithName("test").WithMinsToComplete(30)
            .WithOption(TimeOption.Fixed).WithUserName("test")
            .WithActualTiming(new TimeOnly(9, 0), new TimeOnly(9, 30), DayOfWeek.Monday)
            .WithPrefFixedTiming(new TimeOnly(10, 0), new TimeOnly(10, 30))
            .WithCountInWeek(3)
            .Build();
        mockHabitsDbSet.Setup(d => d.Find(habit.Id)).Returns((DBHabit?)null);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryCreate(habit);

        Assert.True(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания привычки пользователя которой еще не существует")]
    public async Task TryCreateAsyncHabitNotExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var habit = TaskTrackerMother.Habit().WithName("test").WithMinsToComplete(30)
            .WithOption(TimeOption.Fixed).WithUserName("test")
            .WithActualTiming(new TimeOnly(9, 0), new TimeOnly(9, 30), DayOfWeek.Monday)
            .WithPrefFixedTiming(new TimeOnly(10, 0), new TimeOnly(10, 30))
            .WithCountInWeek(3)
            .Build();
        mockHabitsDbSet.Setup(d => d.FindAsync(habit.Id)).ReturnsAsync((DBHabit?)null);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryCreateAsync(habit);

        Assert.True(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания привычки пользователя которая уже существует")]
    public void TryCreateHabitAlreadyExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var habit = TaskTrackerMother.Habit().Build();
        mockHabitsDbSet.Setup(d => d.Find(habit.Id)).Returns(new DBHabit(habit));
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryCreate(habit);

        Assert.False(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания привычки пользователя которая уже существует")]
    public async Task TryCreateAsyncHabitAlreadyExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var habit = TaskTrackerMother.Habit().Build();
        mockHabitsDbSet.Setup(d => d.FindAsync(habit.Id)).ReturnsAsync(new DBHabit(habit));
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryCreateAsync(habit);

        Assert.False(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания нескольких привычек пользователя которых еще не существует")]
    public void TryCreateManyNotExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var habits = new List<Habit>
        {
            TaskTrackerMother.Habit().WithName("1").Build(),
            TaskTrackerMother.Habit().WithName("2").Build()
        };
        mockHabitsDbSet.Setup(d => d.Find(It.IsAny<Guid>())).Returns((DBHabit?)null);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryCreateMany(habits);

        Assert.True(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания нескольких привычек пользователя которых еще не существует")]
    public async Task TryCreateAsyncManyNotExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var habits = new List<Habit>
        {
            TaskTrackerMother.Habit().WithName("1").Build(),
            TaskTrackerMother.Habit().WithName("2").Build()
        };
        mockHabitsDbSet.Setup(d => d.FindAsync(It.IsAny<Guid>())).ReturnsAsync((DBHabit?)null);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryCreateManyAsync(habits);

        Assert.True(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания нескольких привычек пользователя одна из которых уже существует")]
    public void TryCreateManyAlreadyExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var habits = new List<Habit>
        {
            TaskTrackerMother.Habit().WithName("1").Build(),
            TaskTrackerMother.Habit().WithName("2").Build()
        };
        var existingDbHabit = new DBHabit(TaskTrackerMother.Habit().WithName("existing").Build());
        mockHabitsDbSet.Setup(d => d.Find(habits[0].Id)).Returns(existingDbHabit);
        mockHabitsDbSet.Setup(d => d.Find(habits[1].Id)).Returns((DBHabit?)null);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryCreateMany(habits);

        Assert.False(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания нескольких привычек пользователя одна из которых уже существует")]
    public async Task TryCreateAsyncManyAlreadyExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var habits = new List<Habit>
        {
            TaskTrackerMother.Habit().WithName("1").Build(),
            TaskTrackerMother.Habit().WithName("2").Build()
        };
        var existingDbHabit = new DBHabit(TaskTrackerMother.Habit().WithName("existing").Build());
        mockHabitsDbSet.Setup(d => d.FindAsync(habits[0].Id)).ReturnsAsync(existingDbHabit);
        mockHabitsDbSet.Setup(d => d.FindAsync(habits[1].Id)).ReturnsAsync((DBHabit?)null);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryCreateManyAsync(habits);

        Assert.False(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления существующей привычки")]
    public void TryDeleteHabitExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var habitId = Guid.NewGuid();
        var dbHabit = new DBHabit(habitId, "test", 30, TimeOption.Fixed, "test", 3)
        {
            ActualTimings = [new DBActualTime(Guid.NewGuid(), new TimeOnly(9, 0),
                new TimeOnly(9, 30), DayOfWeek.Monday, habitId)],
            PrefFixedTimings = [ new DBPrefFixedTime(Guid.NewGuid(), new TimeOnly(8, 0),
                new TimeOnly(8, 30), habitId) ]
        };
        var habitsList = new List<DBHabit> { dbHabit }.AsQueryable();
        SetupMockDbSet(mockHabitsDbSet, habitsList);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryDelete(habitId);

        Assert.True(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления существующей привычки")]
    public async Task TryDeleteAsyncHabitExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var habitId = Guid.NewGuid();
        var dbHabit = new DBHabit(habitId, "test", 30, TimeOption.Fixed, "test", 3)
        {
            ActualTimings = [new DBActualTime(Guid.NewGuid(), new TimeOnly(9, 0),
                new TimeOnly(9, 30), DayOfWeek.Monday, habitId)],
            PrefFixedTimings = [ new DBPrefFixedTime(Guid.NewGuid(), new TimeOnly(8, 0),
                new TimeOnly(8, 30), habitId) ]
        };
        var habitsList = new List<DBHabit> { dbHabit }.AsQueryable();
        SetupMockDbSetForAsync(mockHabitsDbSet, habitsList);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryDeleteAsync(habitId);

        Assert.True(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления несуществующей привычки")]
    public void TryDeleteHabitNotExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var habitId = Guid.NewGuid();
        var emptyHabits = new List<DBHabit>().AsQueryable();
        SetupMockDbSet(mockHabitsDbSet, emptyHabits);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryDelete(habitId);

        Assert.False(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления несуществующей привычки")]
    public async Task TryDeleteAsyncHabitNotExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var habitId = Guid.NewGuid();
        var emptyHabits = new List<DBHabit>().AsQueryable();
        SetupMockDbSetForAsync(mockHabitsDbSet, emptyHabits);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryDeleteAsync(habitId);

        Assert.False(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления привычек у существующего пользователя")]
    public void TryDeleteHabitsUserExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var userName = "test";
        var testUser = new DBUser(userName, "+79161648345", "test");
        var dbHabits = new List<DBHabit>
        {
            new(Guid.NewGuid(), "test", 30, TimeOption.Fixed, userName, 3)
        }.AsQueryable();
        mockUsersDbSet.Setup(d => d.Find(userName)).Returns(testUser);
        SetupMockDbSet(mockHabitsDbSet, dbHabits);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryDeleteHabits(userName);

        Assert.True(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления привычек у существующего пользователя")]
    public async Task TryDeleteAsyncHabitsUserExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var userName = "test";
        var testUser = new DBUser(userName, "+79161648345", "test");
        var dbHabits = new List<DBHabit>
        {
            new(Guid.NewGuid(), "test", 30, TimeOption.Fixed, userName, 3)
        }.AsQueryable();
        mockUsersDbSet.Setup(d => d.FindAsync(userName)).ReturnsAsync(testUser);
        SetupMockDbSetForAsync(mockHabitsDbSet, dbHabits);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryDeleteHabitsAsync(userName);

        Assert.True(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления привычек у несуществующего пользователя")]
    public void TryDeleteHabitUserNotExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "test";
        mockUsersDbSet.Setup(d => d.Find(userName)).Returns((DBUser?)null);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryDeleteHabits(userName);

        Assert.False(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления привычек у несуществующего пользователя")]
    public async Task TryDeleteAsyncHabitUserNotExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "test";
        mockUsersDbSet.Setup(d => d.FindAsync(userName)).ReturnsAsync((DBUser?)null);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryDeleteHabitsAsync(userName);

        Assert.False(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест замены привычек у существующего пользователя")]
    public void TryReplaceValidHabits()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var userName = "test";
        var testUser = new DBUser(userName, "+79161648345", "test");
        var newHabits = new List<Habit>
        {
            new(Guid.NewGuid(), "test", 30, TimeOption.NoMatter, userName, [], [], 3)
        };
        mockUsersDbSet.Setup(d => d.Find(userName)).Returns(testUser);
        // TryDeleteHabits
        var emptyHabits = new List<DBHabit>().AsQueryable();
        SetupMockDbSet(mockHabitsDbSet, emptyHabits);
        // TryCreateMany
        mockHabitsDbSet.Setup(d => d.Find(It.IsAny<Guid>())).Returns((DBHabit?)null);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryReplaceHabits(newHabits, userName);

        Assert.True(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест замены привычек у существующего пользователя")]
    public async Task TryReplaceAsyncValidHabits()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();
        var userName = "test";
        var testUser = new DBUser(userName, "+79161648345", "test");
        var newHabits = new List<Habit>
        {
            new(Guid.NewGuid(), "test", 30, TimeOption.NoMatter, userName, [], [], 3)
        };
        mockUsersDbSet.Setup(d => d.FindAsync(userName)).ReturnsAsync(testUser);
        // TryDeleteHabits
        var emptyHabits = new List<DBHabit>().AsQueryable();
        SetupMockDbSetForAsync(mockHabitsDbSet, emptyHabits);
        // TryCreateMany
        mockHabitsDbSet.Setup(d => d.FindAsync(It.IsAny<Guid>())).ReturnsAsync((DBHabit?)null);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryReplaceHabitsAsync(newHabits, userName);

        Assert.True(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест замены привычек у существующего пользователя, но не все переданные привычки" +
        "принадлежат одному пользователю")]
    public void TryReplaceHabitsNotAllBelongToUser()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var userName = "test";
        var otherUser = "other";
        var habits = new List<Habit>
        {
            new(Guid.NewGuid(), "1", 30, TimeOption.NoMatter, userName, [], [], 3),
            new(Guid.NewGuid(), "2", 45, TimeOption.NoMatter, otherUser, [], [], 5)
        };
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryReplaceHabits(habits, userName);

        Assert.False(result);
    }
    [Fact]
    [AllureFeature("HabitRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест замены привычек у существующего пользователя, но не все переданные привычки" +
        "принадлежат одному пользователю")]
    public async Task TryReplaceHabitsAsyncNotAllBelongToUser()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var userName = "test";
        var otherUser = "other";
        var habits = new List<Habit>
        {
            new(Guid.NewGuid(), "1", 30, TimeOption.NoMatter, userName, [], [], 3),
            new(Guid.NewGuid(), "2", 45, TimeOption.NoMatter, otherUser, [], [], 5)
        };
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = await repo.TryReplaceHabitsAsync(habits, userName);

        Assert.False(result);
    }
    private void SetupMockDbSet<T>(Mock<DbSet<T>> mockDbSet, IQueryable<T> data) where T : class
    {
        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.Provider).Returns(data.Provider);
        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.Expression).Returns(data.Expression);
        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.ElementType).Returns(data.ElementType);
        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
    }
    private void SetupMockDbSetForAsync<T>(Mock<DbSet<T>> mockDbSet, IQueryable<T> data) where T : class
    {
        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.Provider).Returns(data.Provider);
        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.Expression).Returns(data.Expression);
        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.ElementType).Returns(data.ElementType);
        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        mockDbSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(data.Provider));
    }
    internal class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner = inner;
        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }
        public T Current => _inner.Current;
    }
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;
        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }
        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }
        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }
        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(nameof(Execute), 1, new[] { typeof(Expression) })
                ?.MakeGenericMethod(resultType)
                .Invoke(this, new[] { expression });
            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                ?.MakeGenericMethod(resultType)
                .Invoke(null, new[] { executionResult });
        }
    }
    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable)
        { }
        public TestAsyncEnumerable(Expression expression) : base(expression)
        { }
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }
    }
}
