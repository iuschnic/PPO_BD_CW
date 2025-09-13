using Moq;
using Microsoft.Extensions.Logging;
using Domain.OutPorts;
using Domain;
using Domain.Models;
using Types;
using Tests.ObjectMothers;
using Allure.Xunit.Attributes;

namespace Tests.UnitTests;

public class UnitTestsTaskTracker
{
    [Fact]
    [AllureFeature("TaskTracker")]
    [AllureStory("Создание пользователя")]
    [AllureDescription("Тест создания пользователя с корректными данными")]
    public void CreateUserWithValidData()
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
        var phoneNumber = new PhoneNumber("+79161648345");
        var password = "123";
        var expectedUser = new User(userName, password, phoneNumber,
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        mockUserRepo.Setup(r => r.TryCreate(It.IsAny<User>())).Returns(true);
        mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(expectedUser);
        mockEventRepo.Setup(r => r.TryGet(userName)).Returns([]);

        var result = taskTracker.CreateUser(userName, phoneNumber, password);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
        Assert.Equal(phoneNumber, result.Number);
        Assert.True(result.Settings.NotifyOn);
        Assert.Equal(userName, result.Settings.UserNameID);
    }

    [Fact]
    [AllureFeature("TaskTracker")]
    [AllureStory("Создание пользователя")]
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
        var phoneNumber = new PhoneNumber("+79161648345");
        var password = "123";
        mockUserRepo.Setup(r => r.TryCreate(It.IsAny<User>())).Returns(false);

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.CreateUser(userName, phoneNumber, password));

        Assert.Contains(userName, exception.Message);
    }

    [Fact]
    [AllureFeature("TaskTracker")]
    [AllureStory("Авторизация")]
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
        var user = new User(userName, password, new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        mockEventRepo.Setup(r => r.TryGet(userName)).Returns([]);

        var result = taskTracker.LogIn(userName, password);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
    }

    [Fact]
    [AllureFeature("TaskTracker")]
    [AllureStory("Авторизация")]
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
        var user = new User(userName, correctPassword, new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, userName, []));
        mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.LogIn(userName, wrongPassword));

        Assert.Contains("неправильный пароль", exception.Message);
    }

    [Fact]
    [AllureFeature("TaskTracker")]
    [AllureStory("Загрузка нового расписания")]
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
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
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
    [AllureFeature("TaskTracker")]
    [AllureStory("Загрузка нового расписания")]
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
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        mockShedLoader.Setup(s => s.LoadShedule(userName, invalidFilePath))
                      .Throws(new InvalidDataException($"Ошибка чтения файла"));

        var exception = Assert.Throws<Exception>(() =>
            taskTracker.ImportNewShedule(userName, invalidFilePath));

        Assert.Contains("Ошибка загрузки расписания", exception.Message);
    }
    [Fact]
    [AllureFeature("TaskTracker")]
    [AllureStory("Добавление привычки")]
    [AllureDescription("Тест добавления валидной привычки")]
    public void AddValidHabit()
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
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
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
    [AllureFeature("TaskTracker")]
    [AllureStory("Добавление привычки")]
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
    [AllureFeature("TaskTracker")]
    [AllureStory("Удаление привычки")]
    [AllureDescription("Тест удаления существующей привычки")]
    public void DeleteValidHabit()
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
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
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
    [AllureFeature("TaskTracker")]
    [AllureStory("Удаление привычки")]
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
    [AllureFeature("TaskTracker")]
    [AllureStory("Удаление всех привычек")]
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
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
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
        mockEventRepo.Setup(r => r.TryGet(userName)).Returns(new List<Event>());

        var result = taskTracker.DeleteHabits(userName);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [AllureFeature("TaskTracker")]
    [AllureStory("Удаление всех привычек")]
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
    [AllureFeature("TaskTracker")]
    [AllureStory("Изменение настроек пользователя")]
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
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
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
    [AllureFeature("TaskTracker")]
    [AllureStory("Изменение настроек пользователя")]
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
    [AllureFeature("TaskTracker")]
    [AllureStory("Удаление пользователя")]
    [AllureDescription("Тест удаления существующего пользователя")]
    public void DeleteValidUser()
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
    [AllureFeature("TaskTracker")]
    [AllureStory("Удаление пользователя")]
    [AllureDescription("Тест удаления несуществующего пользователя")]
    public void DeleteUser_WhenDeleteFails_ThrowsExceptionAndLogsError()
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
}
