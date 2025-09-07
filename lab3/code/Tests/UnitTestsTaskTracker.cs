using Moq;
using Microsoft.Extensions.Logging;
using Domain.OutPorts;
using Domain;
using Domain.Models;
using Types;
using Tests.ObjectMothers;

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
    public void ImportNewSheduleInvalidFile()
    {
        var userName = "test";
        var malformedPath = "invalid_file.json";
        var user = new User(userName, "password", new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, userName, []), [], []);
        _mockUserRepo.Setup(r => r.TryGet(userName)).Returns(user);
        _mockShedLoader.Setup(s => s.LoadShedule(userName, malformedPath))
                      .Throws(new InvalidDataException($"Ошибка чтения файла"));

        var exception = Assert.Throws<Exception>(() =>
            _taskTracker.ImportNewShedule(userName, malformedPath));

        Assert.Contains("Ошибка загрузки расписания", exception.Message);
    }
    [Fact]
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
    public void AddHabitNoValidUser()
    {
        var notExistUserName = "not_exists";
        var habit = new Habit(Guid.NewGuid(), "Спорт", 30, TimeOption.NoMatter, notExistUserName, [], [], 1);
        _mockUserRepo.Setup(r => r.TryGet(notExistUserName)).Returns((User?)null);

        var exception = Assert.Throws<Exception>(() => _taskTracker.AddHabit(habit));

        Assert.Contains($"Пользователя с именем {notExistUserName} не существует", exception.Message);
    }
    [Fact]
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
}
