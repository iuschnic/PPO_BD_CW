using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Storage.Models;
using Storage.PostgresStorageAdapters;
using Allure.Xunit.Attributes;
using Types;

namespace Tests.UnitTests;
public class UnitTestsUserRepo
{
    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест получения пользователя когда он существует")]
    public void TryGetUserExists()
    {
        Console.WriteLine($"Test1 executed at {DateTime.Now:HH:mm:ss.fff}");
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "test";
        var dbUser = new DBUser(userName, "+79161648345", "test")
        {
            Settings = new DBUserSettings(Guid.NewGuid(), true, userName)
            {
                ForbiddenTimings = [new(Guid.NewGuid(), new TimeOnly(9, 0), new TimeOnly(10, 0), Guid.NewGuid())]
            }
        };
        var usersList = new List<DBUser> { dbUser }.AsQueryable();
        SetupMockDbSet(mockUsersDbSet, usersList);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);

        var result = repo.TryGet(userName);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
        Assert.Equal("+79161648345", result.Number.StringNumber);
        Assert.Equal("test", result.PasswordHash);
        Assert.NotNull(result.Settings);
        Assert.Single(result.Settings.SettingsTimes);
    }

    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест получения пользователя когда он не существует")]
    public void TryGetUserNotExist()
    {
        Console.WriteLine($"Test2 executed at {DateTime.Now:HH:mm:ss.fff}");
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "test";
        var emptyUsers = new List<DBUser>().AsQueryable();
        SetupMockDbSet(mockUsersDbSet, emptyUsers);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);

        var result = repo.TryGet(userName);

        Assert.Null(result);
    }

    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест получения пользователя со всей связанной информацией когда он существует")]
    public void TryFullGetUserExists()
    {
        Console.WriteLine($"Test3 executed at {DateTime.Now:HH:mm:ss.fff}");
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "test";
        var dbUser = new DBUser(userName, "+79161648345", "test")
        {
            Settings = new DBUserSettings(Guid.NewGuid(), true, userName),
            Habits = 
                [new(Guid.NewGuid(), "test", 30, TimeOption.NoMatter, userName, 3)
                {
                    ActualTimings = [],
                    PrefFixedTimings = []
                }]
        };
        var usersList = new List<DBUser> { dbUser }.AsQueryable();
        SetupMockDbSet(mockUsersDbSet, usersList);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);

        var result = repo.TryFullGet(userName);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
        Assert.NotNull(result.Habits);
        Assert.Single(result.Habits);
        Assert.Equal("test", result.Habits[0].Name);
        Assert.NotNull(result.Settings);
    }

    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест получения пользователя со всей связанной информацией когда он не существует")]
    public void TryFullGetUserNotExist()
    {
        Console.WriteLine($"Test4 executed at {DateTime.Now:HH:mm:ss.fff}");
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "test";
        var emptyUsers = new List<DBUser>().AsQueryable();
        SetupMockDbSet(mockUsersDbSet, emptyUsers);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);

        var result = repo.TryFullGet(userName);

        Assert.Null(result);
    }

    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест создание пользователя когда он не существует")]
    public void TryCreateUserNotExists()
    {
        Console.WriteLine($"Test5 executed at {DateTime.Now:HH:mm:ss.fff}");
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockSettingsDbSet = new Mock<DbSet<DBUserSettings>>();
        var mockSettingsTimesDbSet = new Mock<DbSet<DBSTime>>();
        var user = new User("test", "test", new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, "test",
               [new(Guid.NewGuid(), new TimeOnly(9, 0), new TimeOnly(10, 0), Guid.NewGuid())]
            ));
        mockUsersDbSet.Setup(d => d.Find(user.NameID)).Returns((DBUser?)null);
        mockSettingsDbSet.Setup(d => d.Find(user.Settings.Id)).Returns((DBUserSettings?)null);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.USettings).Returns(mockSettingsDbSet.Object);
        mockDbContext.Setup(db => db.SettingsTimes).Returns(mockSettingsTimesDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);

        var result = repo.TryCreate(user);

        Assert.True(result);
    }

    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест создание пользователя когда он уже существует")]
    public void TryCreateUserAlreadyExists()
    {
        Console.WriteLine($"Test6 executed at {DateTime.Now:HH:mm:ss.fff}");
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var user = new User("test", "test", new PhoneNumber("+79161648345"),
            new UserSettings(Guid.NewGuid(), true, "test", new List<SettingsTime>()));
        var existingUser = new DBUser("test", "+79161648345", "old");
        mockUsersDbSet.Setup(d => d.Find(user.NameID)).Returns(existingUser);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);

        var result = repo.TryCreate(user);

        Assert.False(result);
    }

    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест обновления информации о пользователе когда он существует")]
    public void TryUpdateUserExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "test";
        var existingUser = new DBUser(userName, "+79161648345", "old");
        mockUsersDbSet.Setup(d => d.Find(userName)).Returns(existingUser);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.SaveChanges()).Returns(1);
        var repo = new PostgresUserRepo(mockDbContext.Object);
        var updatedUser = new User(userName, "new", new PhoneNumber("+79999999999"),
            new UserSettings(Guid.NewGuid(), true, userName, []));

        var result = repo.TryUpdateUser(updatedUser);

        Assert.True(result);
        Assert.Equal("+79999999999", existingUser.Number);
        Assert.Equal("new", existingUser.PasswordHash);
    }

    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест обновления информации о пользователе когда он не существует")]
    public void TryUpdateUserNotExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "test";
        mockUsersDbSet.Setup(d => d.Find(userName)).Returns((DBUser?)null);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);
        var updatedUser = new User(userName, "new", new PhoneNumber("+79999999999"),
            new UserSettings(Guid.NewGuid(), true, userName, []));

        var result = repo.TryUpdateUser(updatedUser);

        Assert.False(result);
    }

    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест обновления настроек пользователя когда они существует")]
    public void TryUpdateSettingsExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockSettingsDbSet = new Mock<DbSet<DBUserSettings>>();
        var mockSettingsTimesDbSet = new Mock<DbSet<DBSTime>>();
        var settingsId = Guid.NewGuid();
        var existingSettings = new DBUserSettings(settingsId, false, "test");
        var existingTimes = new List<DBSTime>().AsQueryable();
        mockSettingsDbSet.Setup(d => d.Find(settingsId)).Returns(existingSettings);
        SetupMockDbSet(mockSettingsTimesDbSet, existingTimes);
        mockDbContext.Setup(db => db.USettings).Returns(mockSettingsDbSet.Object);
        mockDbContext.Setup(db => db.SettingsTimes).Returns(mockSettingsTimesDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);
        var newSettings = new UserSettings(settingsId, true, "test", []);

        var result = repo.TryUpdateSettings(newSettings);

        Assert.True(result);
        Assert.True(existingSettings.NotifyOn);
    }

    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест обновления настроек пользователя когда они не существуют")]
    public void TryUpdateSettingsNoExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockSettingsDbSet = new Mock<DbSet<DBUserSettings>>();
        var mockSettingsTimesDbSet = new Mock<DbSet<DBSTime>>();
        var settingsId = Guid.NewGuid();
        var existingSettings = new DBUserSettings(settingsId, false, "test");
        mockSettingsDbSet.Setup(d => d.Find(settingsId)).Returns((DBUserSettings?)null);
        mockDbContext.Setup(db => db.USettings).Returns(mockSettingsDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);
        var newSettings = new UserSettings(settingsId, true, "test", []);

        var result = repo.TryUpdateSettings(newSettings);

        Assert.False(result);
    }

    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест удаления пользователя когда он существует")]
    public void TryDeleteUserExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockSettingsDbSet = new Mock<DbSet<DBUserSettings>>();
        var mockSettingsTimesDbSet = new Mock<DbSet<DBSTime>>();
        var userName = "test";
        var existingUser = new DBUser(userName, "+79161648345", "test");
        var existingSettings = new DBUserSettings(Guid.NewGuid(), true, userName);
        var existingTimes = new List<DBSTime>().AsQueryable();
        mockUsersDbSet.Setup(d => d.Find(userName)).Returns(existingUser);
        SetupMockDbSet(mockSettingsTimesDbSet, existingTimes);
        SetupMockDbSet(mockSettingsDbSet, new List<DBUserSettings>([existingSettings]).AsQueryable());
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.USettings).Returns(mockSettingsDbSet.Object);
        mockDbContext.Setup(db => db.SettingsTimes).Returns(mockSettingsTimesDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);

        var result = repo.TryDelete(userName);

        Assert.True(result);
    }

    [Fact]
    [AllureFeature("UserRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест удаления пользователя когда он не существует")]
    public void TryDeleteUserNoExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockSettingsDbSet = new Mock<DbSet<DBUserSettings>>();
        var mockSettingsTimesDbSet = new Mock<DbSet<DBSTime>>();
        var userName = "test";
        mockUsersDbSet.Setup(d => d.Find(userName)).Returns((DBUser?)null);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);

        var result = repo.TryDelete(userName);

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
}
