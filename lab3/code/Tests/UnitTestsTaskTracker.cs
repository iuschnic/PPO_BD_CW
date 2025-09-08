using Moq;
using Microsoft.Extensions.Logging;
using Domain.OutPorts;
using Domain;
using Domain.Models;
using Types;
using Tests.ObjectMothers;
using Allure.Xunit.Attributes;

namespace Tests.UnitTaskTracker;

public class UnitTestsTaskTracker
{
    private readonly Mock<IEventRepo> _mockEventRepo;
    private readonly Mock<IHabitRepo> _mockHabitRepo;
    private readonly Mock<IUserRepo> _mockUserRepo;
    private readonly Mock<ISheduleLoad> _mockShedLoader;
    private readonly Mock<IHabitDistributor> _mockDistributor;
    private readonly Mock<ILogger<TaskTracker>> _mockLogger;
    private readonly TaskTracker _taskTracker;

    public UnitTestsTaskTracker()
    {
        _mockEventRepo = new Mock<IEventRepo>();
        _mockHabitRepo = new Mock<IHabitRepo>();
        _mockUserRepo = new Mock<IUserRepo>();
        _mockShedLoader = new Mock<ISheduleLoad>();
        _mockDistributor = new Mock<IHabitDistributor>();
        _mockLogger = new Mock<ILogger<TaskTracker>>();

        _taskTracker = new TaskTracker(
            _mockEventRepo.Object,
            _mockHabitRepo.Object,
            _mockUserRepo.Object,
            _mockShedLoader.Object,
            _mockDistributor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Создание пользователя")]
    [AllureDescription("Тест создания пользователя с корректными данными")]
    public void CreateUserWithValidData()
    {
        var userName = "test";
        var phoneNumber = new PhoneNumber("+79161648345");
        var password = "123";
        var expectedUser = new User(userName, password, phoneNumber,
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        _mockUserRepo.Setup(r => r.TryCreate(It.IsAny<User>())).Returns(true);
        _mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(expectedUser);
        _mockEventRepo.Setup(r => r.TryGet(userName)).Returns([]);

        var result = _taskTracker.CreateUser(userName, phoneNumber, password);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
        Assert.Equal(phoneNumber, result.Number);
        Assert.True(result.Settings.NotifyOn);
        Assert.Equal(userName, result.Settings.UserNameID);
    }

    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Создание пользователя")]
    [AllureDescription("Тест создания пользователя который уже существует")]
    public void CreateUserAlreadyExists()
    {
        var userName = "existingtest";
        var phoneNumber = new PhoneNumber("+79161648345");
        var password = "123";
        _mockUserRepo.Setup(r => r.TryCreate(It.IsAny<User>())).Returns(false);

        var exception = Assert.Throws<Exception>(() =>
            _taskTracker.CreateUser(userName, phoneNumber, password));

        Assert.Contains(userName, exception.Message);
    }

    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Авторизация пользователя")]
    [AllureDescription("Тест авторизации с правильными данными")]
    public void LogInWithValidCredentials()
    {
        var userName = "test";
        var password = "correctPassword";
        var user = new User(userName, password, new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        _mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        _mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        _mockEventRepo.Setup(r => r.TryGet(userName)).Returns([]);

        var result = _taskTracker.LogIn(userName, password);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
    }

    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Авторизация пользователя")]
    [AllureDescription("Тест авторизации с неправильным паролем")]
    public void LogInWithInvalidPassword()
    {
        var userName = "test";
        var correctPassword = "correctPassword";
        var wrongPassword = "wrongPassword";
        var user = new User(userName, correctPassword, new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, userName, []));
        _mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);

        var exception = Assert.Throws<Exception>(() =>
            _taskTracker.LogIn(userName, wrongPassword));

        Assert.Contains("неправильный пароль", exception.Message);
    }

    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Загрузка нового расписания")]
    [AllureDescription("Тест загрузки расписания с правильным файлом")]
    public void ImportNewSheduleValidFile()
    {
        var userName = "test";
        var path = "valid_file.json";
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var events = new List<Event>();
        events.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var habits = new List<Habit> { new Habit(Guid.NewGuid(), "Тренировка", 30,
            TimeOption.NoMatter, userName, [], [], 1) };
        _mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        _mockShedLoader.Setup(s => s.LoadShedule(userName, path)).Returns(events);
        _mockHabitRepo.Setup(r => r.TryGet(userName)).Returns(habits);
        _mockDistributor.Setup(d => d.DistributeHabits(habits, events)).Returns([]);
        _mockEventRepo.Setup(r => r.TryReplaceEvents(events, userName)).Returns(true);
        _mockHabitRepo.Setup(r => r.TryReplaceHabits(habits, userName)).Returns(true);
        _mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        _mockEventRepo.Setup(r => r.TryGet(userName)).Returns(events);

        var result = _taskTracker.ImportNewShedule(userName, path);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }

    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Загрузка нового расписания")]
    [AllureDescription("Тест загрузки расписания с неправильным форматом файла")]
    public void ImportNewSheduleInvalidFile()
    {
        var userName = "test";
        var invalidFilePath = "invalid_file.json";
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        _mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        _mockShedLoader.Setup(s => s.LoadShedule(userName, invalidFilePath))
                      .Throws(new InvalidDataException($"Ошибка чтения файла"));

        var exception = Assert.Throws<Exception>(() =>
            _taskTracker.ImportNewShedule(userName, invalidFilePath));

        Assert.Contains("Ошибка загрузки расписания", exception.Message);
    }
    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Добавление привычки")]
    [AllureDescription("Тест добавления валидной привычки")]
    public void AddValidHabit()
    {
        var userName = "test";
        var habit = new Habit(Guid.NewGuid(), "Чтение", 30, TimeOption.NoMatter, userName, [], [], 1);
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var existingEvents = new List<Event>();
        existingEvents.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        existingEvents.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var existingHabits = new List<Habit> { new Habit(Guid.NewGuid(), "Тренировка", 30,
            TimeOption.NoMatter, userName, [], [], 1) };
        _mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        _mockEventRepo.Setup(r => r.TryGet(userName)).Returns(existingEvents);
        _mockHabitRepo.Setup(r => r.TryGet(userName)).Returns(existingHabits);
        _mockDistributor.Setup(d => d.DistributeHabits(It.Is<List<Habit>>(h => h.Contains(habit)), existingEvents))
                       .Returns([]);
        _mockEventRepo.Setup(r => r.TryReplaceEvents(existingEvents, userName)).Returns(true);
        _mockHabitRepo.Setup(r => r.TryReplaceHabits(It.Is<List<Habit>>(h => h.Contains(habit)), userName)).Returns(true);
        _mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        _mockEventRepo.Setup(r => r.TryGet(userName)).Returns(existingEvents);

        var result = _taskTracker.AddHabit(habit);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Добавление привычки")]
    [AllureDescription("Тест добавления валидной привычки не существующему пользователю")]
    public void AddHabitNoValidUser()
    {
        var notExistUserName = "not_exists";
        var habit = new Habit(Guid.NewGuid(), "Спорт", 30, TimeOption.NoMatter, notExistUserName, [], [], 1);
        _mockUserRepo.Setup(r => r.TryGet(notExistUserName)).Returns((User?)null);

        var exception = Assert.Throws<Exception>(() => _taskTracker.AddHabit(habit));

        Assert.Contains($"Пользователя с именем {notExistUserName} не существует", exception.Message);
    }
    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Удаление привычки")]
    [AllureDescription("Тест удаления существующей привычки")]
    public void DeleteValidHabit()
    {
        var userName = "test";
        var habitName = "Чтение";
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var existingEvents = new List<Event>();
        existingEvents.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        existingEvents.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var habits = new List<Habit>
        {
            new Habit(Guid.NewGuid(), habitName, 60, TimeOption.NoMatter, userName, [], [], 1),
        };
        _mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        _mockEventRepo.Setup(r => r.TryGet(userName)).Returns(existingEvents);
        _mockHabitRepo.Setup(r => r.TryGet(userName)).Returns(habits);
        _mockDistributor.Setup(d => d.DistributeHabits(
                It.Is<List<Habit>>(h => !h.Any(habit => habit.Name == habitName)),
                existingEvents))
            .Returns([]);
        _mockEventRepo.Setup(r => r.TryReplaceEvents(existingEvents, userName)).Returns(true);
        _mockHabitRepo.Setup(r => r.TryReplaceHabits(
                It.Is<List<Habit>>(h => !h.Any(habit => habit.Name == habitName)),
                userName))
            .Returns(true);
        _mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        _mockEventRepo.Setup(r => r.TryGet(userName)).Returns(existingEvents);

        var result = _taskTracker.DeleteHabit(userName, habitName);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Удаление привычки")]
    [AllureDescription("Тест удаления несуществующей привычки")]
    public void DeleteHabitInvalidUser()
    {
        var notExistUserName = "not_exists";
        var habitName = "Чтение";

        _mockUserRepo.Setup(r => r.TryGet(notExistUserName)).Returns((User?)null);

        var exception = Assert.Throws<Exception>(() =>
            _taskTracker.DeleteHabit(notExistUserName, habitName));

        Assert.Contains($"Пользователя с именем {notExistUserName} не существует", exception.Message);
    }
    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Удаление всех привычек")]
    [AllureDescription("Тест удаления всех привычек")]
    public void DeleteHabitsValidUser()
    {
        var userName = "testUser";
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        var habits = new List<Habit>
        {
            new Habit(Guid.NewGuid(), "Чтение", 60, TimeOption.NoMatter, userName, [], [], 2),
            new Habit(Guid.NewGuid(), "Тренировка", 30, TimeOption.NoMatter, userName, [], [], 1)
        };
        _mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        _mockHabitRepo.Setup(r => r.TryGet(userName)).Returns(habits);
        _mockHabitRepo.Setup(r => r.TryDeleteHabits(userName)).Returns(true);
        _mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        _mockEventRepo.Setup(r => r.TryGet(userName)).Returns(new List<Event>());

        var result = _taskTracker.DeleteHabits(userName);

        Assert.NotNull(result);
        Assert.Equal(userName, result.Item1.NameID);
        Assert.Empty(result.Item2);
    }
    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Удаление всех привычек")]
    [AllureDescription("Тест удаления всех привычек у несуществующего пользователя")]
    public void DeleteHabitsInvalidUser()
    {
        var notExistUserName = "not_exists";
        _mockUserRepo.Setup(r => r.TryGet(notExistUserName)).Returns((User?)null);

        var exception = Assert.Throws<Exception>(() =>
            _taskTracker.DeleteHabits(notExistUserName));

        Assert.Contains($"Пользователя с именем {notExistUserName} не существует", exception.Message);
    }
    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Изменение настроек пользователя")]
    [AllureDescription("Тест изменения настроек существующего пользователя")]
    public void ChangeSettingsValidUser()
    {
        var userName = "test";
        var settings = new UserSettings(Guid.NewGuid(), true, userName, []);
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
            settings, [], []);
        var events = new List<Event>();
        _mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        _mockUserRepo.Setup(r => r.TryUpdateSettings(settings)).Returns(true);
        _mockUserRepo.Setup(r => r.TryFullGet(userName)).Returns(user);
        _mockEventRepo.Setup(r => r.TryGet(userName)).Returns(events);

        var result = _taskTracker.ChangeSettings(settings);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
        Assert.Equal(settings, result.Settings);
    }
    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Изменение настроек пользователя")]
    [AllureDescription("Тест изменения настроек несуществующего пользователя")]
    public void ChangeSettingsInvalidUser()
    {
        var notExistsUserName = "not_exists";
        var settings = new UserSettings(Guid.NewGuid(), true, notExistsUserName, []);
        _mockUserRepo.Setup(r => r.TryGet(notExistsUserName)).Returns((User?)null);

        var exception = Assert.Throws<Exception>(() =>
            _taskTracker.ChangeSettings(settings));

        Assert.Contains($"Пользователя с именем {notExistsUserName} не существует", exception.Message);
    }
    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Удаление пользователя")]
    [AllureDescription("Тест удаления существующего пользователя")]
    public void DeleteValidUser()
    {
        var userName = "test";
        _mockUserRepo.Setup(r => r.TryDelete(userName)).Returns(true);

        _taskTracker.DeleteUser(userName);
    }
    [Fact]
    [AllureStory("Методы бизнес логики")]
    [AllureFeature("Удаление пользователя")]
    [AllureDescription("Тест удаления несуществующего пользователя")]
    public void DeleteUser_WhenDeleteFails_ThrowsExceptionAndLogsError()
    {
        var userName = "test";
        _mockUserRepo.Setup(r => r.TryDelete(userName)).Returns(false);

        var exception = Assert.Throws<Exception>(() =>
            _taskTracker.DeleteUser(userName));

        Assert.Contains("Не удалось удалить пользователя", exception.Message);
        Assert.Contains(userName, exception.Message);
    }
}
