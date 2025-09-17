using Allure.Xunit.Attributes;
using Domain;
using Domain.InPorts;
using Domain.Models;
using Domain.OutPorts;
using LoadAdapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Storage.EfAdapters;
using System.ComponentModel;
using Tests.ObjectMothers;
using Types;

namespace Tests.IntegrationTests;

public class IntegrationTestsTaskTracker
{
    private static ITaskTrackerContext GetNewContext()
    {
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();
        var options = new DbContextOptionsBuilder<ITaskTrackerContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .UseInternalServiceProvider(serviceProvider)
            .Options;
        return new PostgresDBContext(options);
    }
    private static ServiceProvider Setup()
    {
        return new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddProvider(NullLoggerProvider.Instance);
                })
                .AddSingleton<IEventRepo, EfEventRepo>()
                .AddSingleton<IHabitRepo, EfHabitRepo>()
                .AddSingleton<IUserRepo, EfUserRepo>()
                .AddSingleton<ITaskTrackerContext, PostgresDBContext>()
                .AddDbContext<PostgresDBContext>(options =>
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"))
                .AddTransient<ISheduleLoad, ShedAdapter>()
                .AddTransient<ITaskTracker, TaskTracker>()
                .AddTransient<IHabitDistributor, HabitDistributor>()
                .BuildServiceProvider();
    }

    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания пользователя с корректными данными")]
    public void CreateUserWithValidData()
    {
        using var serviceProvider = Setup();
        var taskTracker = serviceProvider.GetRequiredService<ITaskTracker>();
        var userName = "test";
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";

        var result = taskTracker.CreateUser(userName, phoneNumber, password);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
        Assert.Equal(phoneNumber, result.Number);
        Assert.True(result.Settings.NotifyOn);
        Assert.Equal(userName, result.Settings.UserNameID);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания пользователя с корректными данными")]
    public async Task CreateUserAsyncWithValidData()
    {
        using var serviceProvider = Setup();
        var taskTracker = serviceProvider.GetRequiredService<ITaskTracker>();
        var userName = "test";
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";

        var result = await taskTracker.CreateUserAsync(userName, phoneNumber, password);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
        Assert.Equal(phoneNumber, result.Number);
        Assert.True(result.Settings.NotifyOn);
        Assert.Equal(userName, result.Settings.UserNameID);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания пользователя который уже существует")]
    public void CreateUserAlreadyExists()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "existingtest";
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        mockUserRepo.Setup(r => r.TryCreate(It.IsAny<User>())).Returns(false);

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.CreateUser(userName, phoneNumber, password));

        Assert.Contains(userName, exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания пользователя который уже существует")]
    public async Task CreateUserAsyncAlreadyExists()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "existingtest";
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        mockUserRepo.Setup(r => r.TryCreateAsync(It.IsAny<User>())).ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            taskTracker.CreateUserAsync(userName, phoneNumber, password));

        Assert.Contains(userName, exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест авторизации с правильными данными")]
    public void LogInWithValidCredentials()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var password = "correctPassword";
        var user = new User(userName, password, new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        mockEventRepo.Setup(r => r.TryGet(userName)).Returns([]);

        var result = taskTracker.LogIn(userName, password);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест авторизации с правильными данными")]
    public async Task LogInAsyncWithValidCredentials()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var password = "correctPassword";
        var user = new User(userName, password, new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        mockUserRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(user);
        mockUserRepo.Setup(r => r.TryFullGetAsync(userName)).ReturnsAsync(user);
        mockEventRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync([]);

        var result = await taskTracker.LogInAsync(userName, password);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест авторизации с неправильным паролем")]
    public void LogInWithInvalidPassword()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var correctPassword = "correctPassword";
        var wrongPassword = "wrongPassword";
        var user = new User(userName, correctPassword, new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []));
        mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.LogIn(userName, wrongPassword));

        Assert.Contains("неправильный пароль", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест авторизации с неправильным паролем")]
    public async Task LogInAsyncWithInvalidPassword()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var correctPassword = "correctPassword";
        var wrongPassword = "wrongPassword";
        var user = new User(userName, correctPassword, new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []));
        mockUserRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(user);

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            taskTracker.LogInAsync(userName, wrongPassword));

        Assert.Contains("неправильный пароль", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест загрузки расписания с правильным файлом")]
    public void ImportNewSheduleValidFile()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var path = "valid_file.json";
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var events = new List<Event>();
        events.AddRange(TaskTrackerMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(TaskTrackerMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var habits = new List<Habit> { new Habit(Guid.NewGuid(), "Тренировка", 30,
            TimeOption.NoMatter, userName, [], [], 1) };
        mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        mockShedLoader.Setup(s => s.LoadShedule(userName, path)).Returns(events);
        mockHabitRepo.Setup(r => r.TryGet(userName)).Returns(habits);
        mockDistributor.Setup(d => d.DistributeHabits(habits, events)).Returns([]);
        mockEventRepo.Setup(r => r.TryReplaceEvents(events, userName)).Returns(true);
        mockHabitRepo.Setup(r => r.TryReplaceHabits(habits, userName)).Returns(true);
        mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        mockEventRepo.Setup(r => r.TryGet(userName)).Returns(events);

        var result = taskTracker.ImportNewShedule(userName, path);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест загрузки расписания с правильным файлом")]
    public async Task ImportNewSheduleAsyncValidFile()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var path = "valid_file.json";
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var events = new List<Event>();
        events.AddRange(TaskTrackerMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(TaskTrackerMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var habits = new List<Habit> { new Habit(Guid.NewGuid(), "Тренировка", 30,
            TimeOption.NoMatter, userName, [], [], 1) };
        mockUserRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(user);
        mockShedLoader.Setup(s => s.LoadShedule(userName, path)).Returns(events);
        mockHabitRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(habits);
        mockDistributor.Setup(d => d.DistributeHabits(habits, events)).Returns([]);
        mockEventRepo.Setup(r => r.TryReplaceEventsAsync(events, userName)).ReturnsAsync(true);
        mockHabitRepo.Setup(r => r.TryReplaceHabitsAsync(habits, userName)).ReturnsAsync(true);
        mockUserRepo.Setup(r => r.TryFullGetAsync(userName)).ReturnsAsync(user);
        mockEventRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(events);

        var result = await taskTracker.ImportNewSheduleAsync(userName, path);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест загрузки расписания с неправильным форматом файла")]
    public void ImportNewSheduleInvalidFile()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var invalidFilePath = "invalid_file.json";
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        mockShedLoader.Setup(s => s.LoadShedule(userName, invalidFilePath))
                      .Throws(new InvalidDataException($"Ошибка чтения файла"));

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.ImportNewShedule(userName, invalidFilePath));

        Assert.Contains("Ошибка загрузки расписания", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест загрузки расписания с неправильным форматом файла")]
    public async Task ImportNewSheduleAsyncInvalidFile()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var invalidFilePath = "invalid_file.json";
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        mockUserRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(user);
        mockShedLoader.Setup(s => s.LoadShedule(userName, invalidFilePath))
                      .Throws(new InvalidDataException($"Ошибка чтения файла"));

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            taskTracker.ImportNewSheduleAsync(userName, invalidFilePath));

        Assert.Contains("Ошибка загрузки расписания", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест добавления валидной привычки")]
    public void AddHabitValid()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var habit = new Habit(Guid.NewGuid(), "Чтение", 30, TimeOption.NoMatter, userName, [], [], 1);
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var existingEvents = new List<Event>();
        existingEvents.AddRange(TaskTrackerMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        existingEvents.AddRange(TaskTrackerMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var existingHabits = new List<Habit> { new Habit(Guid.NewGuid(), "Тренировка", 30,
            TimeOption.NoMatter, userName, [], [], 1) };
        mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        mockEventRepo.Setup(r => r.TryGet(userName)).Returns(existingEvents);
        mockHabitRepo.Setup(r => r.TryGet(userName)).Returns(existingHabits);
        mockDistributor.Setup(d => d.DistributeHabits(It.Is<List<Habit>>(h => h.Contains(habit)), existingEvents))
                       .Returns([]);
        mockEventRepo.Setup(r => r.TryReplaceEvents(existingEvents, userName)).Returns(true);
        mockHabitRepo.Setup(r => r.TryReplaceHabits(It.Is<List<Habit>>(h => h.Contains(habit)), userName)).Returns(true);
        mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        mockEventRepo.Setup(r => r.TryGet(userName)).Returns(existingEvents);

        var result = taskTracker.AddHabit(habit);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест добавления валидной привычки")]
    public async Task AddHabitAsyncValid()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var habit = new Habit(Guid.NewGuid(), "Чтение", 30, TimeOption.NoMatter, userName, [], [], 1);
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var existingEvents = new List<Event>();
        existingEvents.AddRange(TaskTrackerMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        existingEvents.AddRange(TaskTrackerMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var existingHabits = new List<Habit> { new Habit(Guid.NewGuid(), "Тренировка", 30,
            TimeOption.NoMatter, userName, [], [], 1) };
        mockUserRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(user);
        mockEventRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(existingEvents);
        mockHabitRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(existingHabits);
        mockDistributor.Setup(d => d.DistributeHabits(It.Is<List<Habit>>(h => h.Contains(habit)), existingEvents))
                       .Returns([]);
        mockEventRepo.Setup(r => r.TryReplaceEventsAsync(existingEvents, userName)).ReturnsAsync(true);
        mockHabitRepo.Setup(r => r.TryReplaceHabitsAsync
            (It.Is<List<Habit>>(h => h.Contains(habit)), userName)).ReturnsAsync(true);
        mockUserRepo.Setup(r => r.TryFullGetAsync(userName)).ReturnsAsync(user);
        mockEventRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(existingEvents);

        var result = await taskTracker.AddHabitAsync(habit);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест добавления валидной привычки не существующему пользователю")]
    public void AddHabitNoValidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var notExistUserName = "not_exists";
        var habit = new Habit(Guid.NewGuid(), "Спорт", 30, TimeOption.NoMatter, notExistUserName, [], [], 1);
        mockUserRepo.Setup(r => r.TryGet(notExistUserName)).Returns((User?)null);

        var exception = Assert.Throws<Exception>(() => taskTracker.AddHabit(habit));

        Assert.Contains($"Пользователя с именем {notExistUserName} не существует", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест добавления валидной привычки не существующему пользователю")]
    public async Task AddHabitAsyncNoValidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var notExistUserName = "not_exists";
        var habit = new Habit(Guid.NewGuid(), "Спорт", 30, TimeOption.NoMatter, notExistUserName, [], [], 1);
        mockUserRepo.Setup(r => r.TryGetAsync(notExistUserName)).ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<Exception>(() => taskTracker.AddHabitAsync(habit));

        Assert.Contains($"Пользователя с именем {notExistUserName} не существует", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления существующей привычки")]
    public void DeleteHabitValid()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var habitName = "Чтение";
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var existingEvents = new List<Event>();
        existingEvents.AddRange(TaskTrackerMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        existingEvents.AddRange(TaskTrackerMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var habits = new List<Habit>
        {
            new Habit(Guid.NewGuid(), habitName, 60, TimeOption.NoMatter, userName, [], [], 1),
        };
        mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        mockEventRepo.Setup(r => r.TryGet(userName)).Returns(existingEvents);
        mockHabitRepo.Setup(r => r.TryGet(userName)).Returns(habits);
        mockDistributor.Setup(d => d.DistributeHabits(
                It.Is<List<Habit>>(h => !h.Any(habit => habit.Name == habitName)),
                existingEvents))
            .Returns([]);
        mockEventRepo.Setup(r => r.TryReplaceEvents(existingEvents, userName)).Returns(true);
        mockHabitRepo.Setup(r => r.TryReplaceHabits(
                It.Is<List<Habit>>(h => !h.Any(habit => habit.Name == habitName)),
                userName))
            .Returns(true);
        mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        mockEventRepo.Setup(r => r.TryGet(userName)).Returns(existingEvents);

        var result = taskTracker.DeleteHabit(userName, habitName);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления существующей привычки")]
    public async Task DeleteHabitAsyncValid()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var habitName = "Чтение";
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var existingEvents = new List<Event>();
        existingEvents.AddRange(TaskTrackerMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        existingEvents.AddRange(TaskTrackerMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var habits = new List<Habit>
        {
            new Habit(Guid.NewGuid(), habitName, 60, TimeOption.NoMatter, userName, [], [], 1),
        };
        mockUserRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(user);
        mockEventRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(existingEvents);
        mockHabitRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(habits);
        mockDistributor.Setup(d => d.DistributeHabits(
                It.Is<List<Habit>>(h => !h.Any(habit => habit.Name == habitName)),
                existingEvents))
            .Returns([]);
        mockEventRepo.Setup(r => r.TryReplaceEventsAsync(existingEvents, userName)).ReturnsAsync(true);
        mockHabitRepo.Setup(r => r.TryReplaceHabitsAsync(
                It.Is<List<Habit>>(h => !h.Any(habit => habit.Name == habitName)),
                userName))
            .ReturnsAsync(true);
        mockUserRepo.Setup(r => r.TryFullGetAsync(userName)).ReturnsAsync(user);
        mockEventRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(existingEvents);

        var result = await taskTracker.DeleteHabitAsync(userName, habitName);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления несуществующей привычки")]
    public void DeleteHabitInvalidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var notExistUserName = "not_exists";
        var habitName = "Чтение";

        mockUserRepo.Setup(r => r.TryGet(notExistUserName)).Returns((User?)null);

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.DeleteHabit(notExistUserName, habitName));

        Assert.Contains($"Пользователя с именем {notExistUserName} не существует", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления несуществующей привычки")]
    public async Task DeleteHabitAsyncInvalidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var notExistUserName = "not_exists";
        var habitName = "Чтение";

        mockUserRepo.Setup(r => r.TryGetAsync(notExistUserName)).ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            taskTracker.DeleteHabitAsync(notExistUserName, habitName));

        Assert.Contains($"Пользователя с именем {notExistUserName} не существует", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления всех привычек")]
    public void DeleteHabitsValidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "testUser";
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var habits = new List<Habit>
        {
            new Habit(Guid.NewGuid(), "Чтение", 60, TimeOption.NoMatter, userName, [], [], 2),
            new Habit(Guid.NewGuid(), "Тренировка", 30, TimeOption.NoMatter, userName, [], [], 1)
        };
        mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        mockHabitRepo.Setup(r => r.TryGet(userName)).Returns(habits);
        mockHabitRepo.Setup(r => r.TryDeleteHabits(userName)).Returns(true);
        mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        mockEventRepo.Setup(r => r.TryGet(userName)).Returns([]);

        var result = taskTracker.DeleteHabits(userName);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления всех привычек")]
    public async Task DeleteHabitsAsyncValidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "testUser";
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var habits = new List<Habit>
        {
            new Habit(Guid.NewGuid(), "Чтение", 60, TimeOption.NoMatter, userName, [], [], 2),
            new Habit(Guid.NewGuid(), "Тренировка", 30, TimeOption.NoMatter, userName, [], [], 1)
        };
        mockUserRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(user);
        mockHabitRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(habits);
        mockHabitRepo.Setup(r => r.TryDeleteHabitsAsync(userName)).ReturnsAsync(true);
        mockUserRepo.Setup(r => r.TryFullGetAsync(userName)).ReturnsAsync(user);
        mockEventRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync([]);

        var result = await taskTracker.DeleteHabitsAsync(userName);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления всех привычек у несуществующего пользователя")]
    public void DeleteHabitsInvalidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var notExistUserName = "not_exists";
        mockUserRepo.Setup(r => r.TryGet(notExistUserName)).Returns((User?)null);

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.DeleteHabits(notExistUserName));

        Assert.Contains($"Пользователя с именем {notExistUserName} не существует", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления всех привычек у несуществующего пользователя")]
    public async Task DeleteHabitsAsyncInvalidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var notExistUserName = "not_exists";
        mockUserRepo.Setup(r => r.TryGetAsync(notExistUserName)).ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            taskTracker.DeleteHabitsAsync(notExistUserName));

        Assert.Contains($"Пользователя с именем {notExistUserName} не существует", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест изменения настроек существующего пользователя")]
    public void ChangeSettingsValidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var settings = new UserSettings(Guid.NewGuid(), true, userName, []);
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            settings, [], []);
        var events = new List<Event>();
        mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        mockUserRepo.Setup(r => r.TryUpdateSettings(settings)).Returns(true);
        mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        mockEventRepo.Setup(r => r.TryGet(userName)).Returns(events);

        var result = taskTracker.ChangeSettings(settings);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
        Assert.Equal(settings, result.Settings);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест изменения настроек существующего пользователя")]
    public async Task ChangeSettingsAsyncValidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        var settings = new UserSettings(Guid.NewGuid(), true, userName, []);
        var user = new User(userName, "password", new PhoneNumber("+71111111111"),
            settings, [], []);
        var events = new List<Event>();
        mockUserRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(user);
        mockUserRepo.Setup(r => r.TryUpdateSettingsAsync(settings)).ReturnsAsync(true);
        mockUserRepo.Setup(r => r.TryFullGetAsync(userName)).ReturnsAsync(user);
        mockEventRepo.Setup(r => r.TryGetAsync(userName)).ReturnsAsync(events);

        var result = await taskTracker.ChangeSettingsAsync(settings);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
        Assert.Equal(settings, result.Settings);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест изменения настроек несуществующего пользователя")]
    public void ChangeSettingsInvalidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var notExistsUserName = "not_exists";
        var settings = new UserSettings(Guid.NewGuid(), true, notExistsUserName, []);
        mockUserRepo.Setup(r => r.TryGet(notExistsUserName)).Returns((User?)null);

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.ChangeSettings(settings));

        Assert.Contains($"Пользователя с именем {notExistsUserName} не существует", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест изменения настроек несуществующего пользователя")]
    public async Task ChangeSettingsAsyncInvalidUser()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var notExistsUserName = "not_exists";
        var settings = new UserSettings(Guid.NewGuid(), true, notExistsUserName, []);
        mockUserRepo.Setup(r => r.TryGetAsync(notExistsUserName)).ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            taskTracker.ChangeSettingsAsync(settings));

        Assert.Contains($"Пользователя с именем {notExistsUserName} не существует", exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления существующего пользователя")]
    public void DeleteUserValid()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        mockUserRepo.Setup(r => r.TryDelete(userName)).Returns(true);

        taskTracker.DeleteUser(userName);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления существующего пользователя")]
    public async Task DeleteUserAsyncValid()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        mockUserRepo.Setup(r => r.TryDeleteAsync(userName)).ReturnsAsync(true);

        await taskTracker.DeleteUserAsync(userName);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления несуществующего пользователя")]
    public void DeleteUserNotExists()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        mockUserRepo.Setup(r => r.TryDelete(userName)).Returns(false);

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.DeleteUser(userName));

        Assert.Contains("Не удалось удалить пользователя", exception.Message);
        Assert.Contains(userName, exception.Message);
    }
    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления несуществующего пользователя")]
    public async Task DeleteUserAsyncNotExists()
    {
        var mockEventRepo = new Mock<IEventRepo>();
        var mockHabitRepo = new Mock<IHabitRepo>();
        var mockUserRepo = new Mock<IUserRepo>();
        var mockShedLoader = new Mock<ISheduleLoad>();
        var mockDistributor = new Mock<IHabitDistributor>();
        var mockLogger = new Mock<ILogger<TaskTracker>>();
        var taskTracker = new TaskTracker(
            mockEventRepo.Object,
            mockHabitRepo.Object,
            mockUserRepo.Object,
            mockShedLoader.Object,
            mockDistributor.Object,
            mockLogger.Object
        );
        var userName = "test";
        mockUserRepo.Setup(r => r.TryDeleteAsync(userName)).ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            taskTracker.DeleteUserAsync(userName));

        Assert.Contains("Не удалось удалить пользователя", exception.Message);
        Assert.Contains(userName, exception.Message);
    }
}