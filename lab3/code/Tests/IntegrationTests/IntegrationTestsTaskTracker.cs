using Allure.Xunit.Attributes;
using Domain;
using Domain.InPorts;
using Domain.Models;
using Domain.OutPorts;
using LoadAdapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Storage.EfAdapters;
using Storage.Models;
using Tests.ObjectMothers;
using Types;

namespace Tests.IntegrationTests;


public class IntegrationTestsTaskTracker : IAsyncLifetime
{
    private string? _connString;
    private ServiceProvider _serviceProvider;
    private EfDbContext _dbContext;

    public IntegrationTestsTaskTracker()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("tests_settings.json", optional: false, reloadOnChange: true)
            .Build();

        if ((_connString = configuration.GetConnectionString("IntegrationTestsConnection")) == null)
            throw new InvalidDataException("Не найдена строка подключения к тестовой базе данных");
        _serviceProvider = Setup();
        _dbContext = _serviceProvider.GetRequiredService<EfDbContext>();
    }

    public async Task InitializeAsync()
    {
        await CleanDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanDatabaseAsync();
        await _dbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private ServiceProvider Setup()
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
                .AddSingleton<ITaskTrackerContext, EfDbContext>()
                .AddDbContext<EfDbContext>(options =>
                    options.UseNpgsql(_connString))
                .AddTransient<ISheduleLoad, ShedAdapter>()
                .AddTransient<ITaskTracker, TaskTracker>()
                .AddTransient<IHabitDistributor, HabitDistributor>()
                .BuildServiceProvider();
    }

    private async Task CleanDatabaseAsync()
    {
        if (_dbContext == null) return;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        await _dbContext.SettingsTimes.ExecuteDeleteAsync();
        await _dbContext.USettings.ExecuteDeleteAsync();
        await _dbContext.Events.ExecuteDeleteAsync();
        await _dbContext.ActualTimes.ExecuteDeleteAsync();
        await _dbContext.PrefFixedTimes.ExecuteDeleteAsync();
        await _dbContext.Habits.ExecuteDeleteAsync();
        await _dbContext.UserMessages.ExecuteDeleteAsync();
        await _dbContext.Messages.ExecuteDeleteAsync();
        await _dbContext.Users.ExecuteDeleteAsync();

        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания пользователя с корректными данными")]
    public async Task CreateUserWithValidData()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "test_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";

        User returnedUser = taskTracker.CreateUser(userName, phoneNumber, password);

        DBUser? createdUser = await context.Users.FindAsync(userName);
        DBUserSettings? createdSettings = await context.USettings.
            Where(s => s.DBUserID == userName).FirstOrDefaultAsync();
        Assert.NotNull(createdUser);
        Assert.NotNull(returnedUser);
        Assert.NotNull(createdSettings);
        Assert.Equal(userName, createdUser.NameID);
        Assert.Equal(userName, returnedUser.NameID);
        Assert.Equal(phoneNumber.StringNumber, createdUser.Number);
        Assert.Equal(phoneNumber, returnedUser.Number);
        Assert.Equal(userName, createdSettings.DBUserID);
        Assert.Equal(userName, returnedUser.Settings.UserNameID);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания пользователя с корректными данными")]
    public async Task CreateUserAsyncWithValidData()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "test_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";

        User returnedUser = await taskTracker.CreateUserAsync(userName, phoneNumber, password);

        DBUser? createdUser = await context.Users.FindAsync(userName);
        DBUserSettings? createdSettings = await context.USettings.
            Where(s => s.DBUserID == userName).FirstOrDefaultAsync();
        Assert.NotNull(createdUser);
        Assert.NotNull(returnedUser);
        Assert.NotNull(createdSettings);
        Assert.Equal(userName, createdUser.NameID);
        Assert.Equal(userName, returnedUser.NameID);
        Assert.Equal(phoneNumber.StringNumber, createdUser.Number);
        Assert.Equal(phoneNumber, returnedUser.Number);
        Assert.Equal(userName, createdSettings.DBUserID);
        Assert.Equal(userName, returnedUser.Settings.UserNameID);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания пользователя который уже существует")]
    public async Task CreateUserAlreadyExists()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "existing_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        var user = new DBUser(userName, phoneNumber.StringNumber, password);
        var settings = new DBUserSettings(Guid.NewGuid(), true, userName);
        await context.Users.AddAsync(user);
        await context.USettings.AddAsync(settings);
        await context.SaveChangesAsync();

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.CreateUser(userName, phoneNumber, password));

        Assert.Contains(userName, exception.Message);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания пользователя который уже существует")]
    public async Task CreateUserAsyncAlreadyExists()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "existing_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        var user = new DBUser(userName, phoneNumber.StringNumber, password);
        var settings = new DBUserSettings(Guid.NewGuid(), true, userName);
        await context.Users.AddAsync(user);
        await context.USettings.AddAsync(settings);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            taskTracker.CreateUserAsync(userName, phoneNumber, password));

        Assert.Contains(userName, exception.Message);
    }
    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест авторизации с правильными данными")]
    public async Task LogInWithValidCredentials()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "existing_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        var user = new DBUser(userName, phoneNumber.StringNumber, password);
        var settings = new DBUserSettings(Guid.NewGuid(), true, userName);
        await context.Users.AddAsync(user);
        await context.USettings.AddAsync(settings);
        await context.SaveChangesAsync();

        User returnedUser = taskTracker.LogIn(userName, password);

        Assert.NotNull(returnedUser);
        Assert.Equal(userName, returnedUser.NameID);
        Assert.Equal(phoneNumber, returnedUser.Number);
        Assert.Equal(userName, returnedUser.Settings.UserNameID);
    }
    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест авторизации с правильными данными")]
    public async Task LogInAsyncWithValidCredentials()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "existing_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        var user = new DBUser(userName, phoneNumber.StringNumber, password);
        var settings = new DBUserSettings(Guid.NewGuid(), true, userName);
        await context.Users.AddAsync(user);
        await context.USettings.AddAsync(settings);
        await context.SaveChangesAsync();

        User returnedUser = await taskTracker.LogInAsync(userName, password);

        Assert.NotNull(returnedUser);
        Assert.Equal(userName, returnedUser.NameID);
        Assert.Equal(phoneNumber, returnedUser.Number);
        Assert.Equal(userName, returnedUser.Settings.UserNameID);
    }
    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест авторизации с неправильным паролем")]
    public async Task LogInWithInvalidPassword()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "existing_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        var wrongPassword = "12";
        var user = new DBUser(userName, phoneNumber.StringNumber, password);
        var settings = new DBUserSettings(Guid.NewGuid(), true, userName);
        await context.Users.AddAsync(user);
        await context.USettings.AddAsync(settings);
        await context.SaveChangesAsync();

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.LogIn(userName, wrongPassword));

        Assert.Contains("неправильный пароль", exception.Message);
    }
    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест авторизации с неправильным паролем")]
    public async Task LogInAsyncWithInvalidPassword()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "existing_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        var wrongPassword = "12";
        var user = new DBUser(userName, phoneNumber.StringNumber, password);
        var settings = new DBUserSettings(Guid.NewGuid(), true, userName);
        await context.Users.AddAsync(user);
        await context.USettings.AddAsync(settings);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            taskTracker.LogInAsync(userName, wrongPassword));

        Assert.Contains("неправильный пароль", exception.Message);
    }




    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест добавления валидной привычки")]
    public async Task AddHabitValid()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "existing_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        var user = new DBUser(userName, phoneNumber.StringNumber, password);
        var settings = new DBUserSettings(Guid.NewGuid(), true, userName);
        await context.Users.AddAsync(user);
        await context.USettings.AddAsync(settings);
        await context.SaveChangesAsync();
        var habitGuid = Guid.NewGuid();
        var habitName = "existing_habit_" + Guid.NewGuid().ToString();
        var habitMinsToComplete = 30;
        var habitOption = TimeOption.NoMatter;
        var habitCountInWeek = 1;
        var prefFixedStart = TimeOnly.Parse("00:00");
        var prefFixedEnd = TimeOnly.Parse("23:59");
        var habit = TaskTrackerMother.Habit()
            .WithId(habitGuid)
            .WithName(habitName)
            .WithMinsToComplete(habitMinsToComplete)
            .WithOption(habitOption)
            .WithUserName(userName)
            .WithCountInWeek(habitCountInWeek)
            .WithPrefFixedTiming(prefFixedStart, prefFixedEnd)
            .Build();

        var returnedInfo = taskTracker.AddHabit(habit);

        var returnedUser = returnedInfo.Item1;
        var returnedUndistrHabits = returnedInfo.Item2;
        var createdHabit = await context.Habits.FindAsync(habitGuid);
        var createdPrefFixed = await context.PrefFixedTimes
            .Where(pf => pf.DBHabitID == habitGuid).FirstOrDefaultAsync();
        Assert.NotNull(returnedInfo);
        Assert.NotNull(returnedUser);
        Assert.NotNull(returnedUndistrHabits);
        Assert.NotNull(createdHabit);
        Assert.NotNull(createdPrefFixed);
        Assert.Equal(phoneNumber, returnedUser.Number);
        Assert.Equal(userName, returnedUser.Settings.UserNameID);
        Assert.Equal(userName, returnedUser.NameID);
        Assert.Equal(phoneNumber, returnedUser.Number);
        Assert.Equal(userName, returnedUser.Settings.UserNameID);
        Assert.Equal(habitGuid, createdHabit.Id);
        Assert.Equal(habitName, createdHabit.Name);
        Assert.Equal(habitMinsToComplete, createdHabit.MinsToComplete);
        Assert.Equal(habitOption, createdHabit.Option);
        Assert.Equal(userName, createdHabit.DBUserNameID);
        Assert.Equal(habitCountInWeek, createdHabit.CountInWeek);
        Assert.Equal(createdPrefFixed.Start, prefFixedStart);
        Assert.Equal(createdPrefFixed.End, prefFixedEnd);
    }
    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест добавления валидной привычки")]
    public async Task AddHabitAsyncValid()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "existing_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        var user = new DBUser(userName, phoneNumber.StringNumber, password);
        var settings = new DBUserSettings(Guid.NewGuid(), true, userName);
        await context.Users.AddAsync(user);
        await context.USettings.AddAsync(settings);
        await context.SaveChangesAsync();
        var habitGuid = Guid.NewGuid();
        var habitName = "existing_habit_" + Guid.NewGuid().ToString();
        var habitMinsToComplete = 30;
        var habitOption = TimeOption.NoMatter;
        var habitCountInWeek = 1;
        var prefFixedStart = TimeOnly.Parse("00:00");
        var prefFixedEnd = TimeOnly.Parse("23:59");
        var habit = TaskTrackerMother.Habit()
            .WithId(habitGuid)
            .WithName(habitName)
            .WithMinsToComplete(habitMinsToComplete)
            .WithOption(habitOption)
            .WithUserName(userName)
            .WithCountInWeek(habitCountInWeek)
            .WithPrefFixedTiming(prefFixedStart, prefFixedEnd)
            .Build();

        var returnedInfo = await taskTracker.AddHabitAsync(habit);

        var returnedUser = returnedInfo.Item1;
        var returnedUndistrHabits = returnedInfo.Item2;
        var createdHabit = await context.Habits.FindAsync(habitGuid);
        var createdPrefFixed = await context.PrefFixedTimes
            .Where(pf => pf.DBHabitID == habitGuid).FirstOrDefaultAsync();
        Assert.NotNull(returnedInfo);
        Assert.NotNull(returnedUser);
        Assert.NotNull(returnedUndistrHabits);
        Assert.NotNull(createdHabit);
        Assert.NotNull(createdPrefFixed);
        Assert.Equal(phoneNumber, returnedUser.Number);
        Assert.Equal(userName, returnedUser.Settings.UserNameID);
        Assert.Equal(userName, returnedUser.NameID);
        Assert.Equal(phoneNumber, returnedUser.Number);
        Assert.Equal(userName, returnedUser.Settings.UserNameID);
        Assert.Equal(habitGuid, createdHabit.Id);
        Assert.Equal(habitName, createdHabit.Name);
        Assert.Equal(habitMinsToComplete, createdHabit.MinsToComplete);
        Assert.Equal(habitOption, createdHabit.Option);
        Assert.Equal(userName, createdHabit.DBUserNameID);
        Assert.Equal(habitCountInWeek, createdHabit.CountInWeek);
        Assert.Equal(createdPrefFixed.Start, prefFixedStart);
        Assert.Equal(createdPrefFixed.End, prefFixedEnd);
    }
    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест добавления валидной привычки не существующему пользователю")]
    public async Task AddHabitNoValidUser()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var noExistingUserName = "noexisting_" + Guid.NewGuid().ToString();
        var habitGuid = Guid.NewGuid();
        var habitName = "existing_habit_" + Guid.NewGuid().ToString();
        var habitMinsToComplete = 30;
        var habitOption = TimeOption.NoMatter;
        var habitCountInWeek = 1;
        var prefFixedStart = TimeOnly.Parse("00:00");
        var prefFixedEnd = TimeOnly.Parse("23:59");
        var habit = TaskTrackerMother.Habit()
            .WithId(habitGuid)
            .WithName(habitName)
            .WithMinsToComplete(habitMinsToComplete)
            .WithOption(habitOption)
            .WithUserName(noExistingUserName)
            .WithCountInWeek(habitCountInWeek)
            .WithPrefFixedTiming(prefFixedStart, prefFixedEnd)
            .Build();

        var exception = Assert.Throws<Exception>(() => taskTracker.AddHabit(habit));

        var notCreatedHabit = await context.Habits.FindAsync(habitGuid);
        var notCreatedPrefFixed = await context.PrefFixedTimes
            .Where(pf => pf.DBHabitID == habitGuid).FirstOrDefaultAsync();
        Assert.Null(notCreatedHabit);
        Assert.Null(notCreatedPrefFixed);
    }
    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест добавления валидной привычки не существующему пользователю")]
    public async Task AddHabitAsyncNoValidUser()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var noExistingUserName = "noexisting_" + Guid.NewGuid().ToString();
        var habitGuid = Guid.NewGuid();
        var habitName = "existing_habit_" + Guid.NewGuid().ToString();
        var habitMinsToComplete = 30;
        var habitOption = TimeOption.NoMatter;
        var habitCountInWeek = 1;
        var prefFixedStart = TimeOnly.Parse("00:00");
        var prefFixedEnd = TimeOnly.Parse("23:59");
        var habit = TaskTrackerMother.Habit()
            .WithId(habitGuid)
            .WithName(habitName)
            .WithMinsToComplete(habitMinsToComplete)
            .WithOption(habitOption)
            .WithUserName(noExistingUserName)
            .WithCountInWeek(habitCountInWeek)
            .WithPrefFixedTiming(prefFixedStart, prefFixedEnd)
            .Build();

        var exception = await Assert.ThrowsAsync<Exception>(() => taskTracker.AddHabitAsync(habit));

        var notCreatedHabit = await context.Habits.FindAsync(habitGuid);
        var notCreatedPrefFixed = await context.PrefFixedTimes
            .Where(pf => pf.DBHabitID == habitGuid).FirstOrDefaultAsync();
        Assert.Null(notCreatedHabit);
        Assert.Null(notCreatedPrefFixed);
    }




    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления существующей привычки")]
    public async Task DeleteHabitValid()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "existing_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        var user = new DBUser(userName, phoneNumber.StringNumber, password);
        var settings = new DBUserSettings(Guid.NewGuid(), true, userName);
        var habitGuid = Guid.NewGuid();
        var habitName = "existing_habit_" + Guid.NewGuid().ToString();
        var habitMinsToComplete = 30;
        var habitOption = TimeOption.NoMatter;
        var habitCountInWeek = 1;
        var prefFixedStart = TimeOnly.Parse("00:00");
        var prefFixedEnd = TimeOnly.Parse("23:59");
        var habit = TaskTrackerMother.Habit()
            .WithId(habitGuid)
            .WithName(habitName)
            .WithMinsToComplete(habitMinsToComplete)
            .WithOption(habitOption)
            .WithUserName(userName)
            .WithCountInWeek(habitCountInWeek)
            .WithPrefFixedTiming(prefFixedStart, prefFixedEnd)
            .Build();
        var dbhabit = new DBHabit(habit);
        var dbPrefFixedTime = new DBPrefFixedTime(habit.PrefFixedTimings[0]);
        await context.Users.AddAsync(user);
        await context.USettings.AddAsync(settings);
        await context.Habits.AddAsync(dbhabit);
        await context.PrefFixedTimes.AddAsync(dbPrefFixedTime);
        await context.SaveChangesAsync();

        var returnedInfo = taskTracker.DeleteHabit(userName, habitName);

        var returnedUser = returnedInfo.Item1;
        var returnedUndistrHabits = returnedInfo.Item2;
        var deletedHabit = await context.Habits.FindAsync(habitGuid);
        var deletedPrefFixed = await context.PrefFixedTimes
            .Where(pf => pf.DBHabitID == habitGuid).FirstOrDefaultAsync();
        Assert.NotNull(returnedInfo);
        Assert.NotNull(returnedUser);
        Assert.NotNull(returnedUndistrHabits);
        Assert.Null(deletedHabit);
        Assert.Null(deletedPrefFixed);
    }
    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления существующей привычки")]
    public async Task DeleteHabitAsyncValid()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var userName = "existing_" + Guid.NewGuid().ToString();
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";
        var user = new DBUser(userName, phoneNumber.StringNumber, password);
        var settings = new DBUserSettings(Guid.NewGuid(), true, userName);
        var habitGuid = Guid.NewGuid();
        var habitName = "existing_habit_" + Guid.NewGuid().ToString();
        var habitMinsToComplete = 30;
        var habitOption = TimeOption.NoMatter;
        var habitCountInWeek = 1;
        var prefFixedStart = TimeOnly.Parse("00:00");
        var prefFixedEnd = TimeOnly.Parse("23:59");
        var habit = TaskTrackerMother.Habit()
            .WithId(habitGuid)
            .WithName(habitName)
            .WithMinsToComplete(habitMinsToComplete)
            .WithOption(habitOption)
            .WithUserName(userName)
            .WithCountInWeek(habitCountInWeek)
            .WithPrefFixedTiming(prefFixedStart, prefFixedEnd)
            .Build();
        var dbhabit = new DBHabit(habit);
        var dbPrefFixedTime = new DBPrefFixedTime(habit.PrefFixedTimings[0]);
        await context.Users.AddAsync(user);
        await context.USettings.AddAsync(settings);
        await context.Habits.AddAsync(dbhabit);
        await context.PrefFixedTimes.AddAsync(dbPrefFixedTime);
        await context.SaveChangesAsync();

        var returnedInfo = await taskTracker.DeleteHabitAsync(userName, habitName);

        var returnedUser = returnedInfo.Item1;
        var returnedUndistrHabits = returnedInfo.Item2;
        var deletedHabit = await context.Habits.FindAsync(habitGuid);
        var deletedPrefFixed = await context.PrefFixedTimes
            .Where(pf => pf.DBHabitID == habitGuid).FirstOrDefaultAsync();
        Assert.NotNull(returnedInfo);
        Assert.NotNull(returnedUser);
        Assert.NotNull(returnedUndistrHabits);
        Assert.Null(deletedHabit);
        Assert.Null(deletedPrefFixed);
    }
    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления привычки несуществующего пользователя")]
    public void DeleteHabitInvalidUser()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var notExistUserName = "existing_" + Guid.NewGuid().ToString();
        var habitName = "existing_habit_" + Guid.NewGuid().ToString();

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.DeleteHabit(notExistUserName, habitName));
    }
    [Fact]
    [Trait("Category", "Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления несуществующей привычки")]
    public async Task DeleteHabitAsyncInvalidUser()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskTracker = scope.ServiceProvider.GetRequiredService<ITaskTracker>();
        var context = scope.ServiceProvider.GetRequiredService<EfDbContext>();
        var notExistUserName = "existing_" + Guid.NewGuid().ToString();
        var habitName = "existing_habit_" + Guid.NewGuid().ToString();

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            taskTracker.DeleteHabitAsync(notExistUserName, habitName));
    }
}