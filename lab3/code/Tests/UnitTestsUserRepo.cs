using Microsoft.EntityFrameworkCore;
using Moq;
using Storage.Models;
using Storage.PostgresStorageAdapters;

namespace Tests;

public class UnitTestsUserRepo
{
    [Fact]
    public void TryGetUserExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockSettingsDbSet = new Mock<DbSet<DBUserSettings>>();
        var mockSettingsTimesDbSet = new Mock<DbSet<DBSTime>>();
        var userName = "test";
        var dbUser = new DBUser(userName, "+79161648345", "password_hash");
        var usersList = new List<DBUser> { dbUser }.AsQueryable();
        var settingsList = new List<DBUserSettings> { new(Guid.NewGuid(), true, userName) }.AsQueryable();
        var timingsList = new List<DBSTime> { new(Guid.NewGuid(), new TimeOnly(9, 0),
            new TimeOnly(10, 0), Guid.NewGuid()) }.AsQueryable();
        SetupMockDbSet(mockUsersDbSet, usersList);
        SetupMockDbSet(mockSettingsDbSet, settingsList);
        SetupMockDbSet(mockSettingsTimesDbSet, timingsList);
        //mockUsersDbSet.Setup(d => d.Include(It.IsAny<string>())).Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.USettings).Returns(mockSettingsDbSet.Object);
        mockDbContext.Setup(db => db.SettingsTimes).Returns(mockSettingsTimesDbSet.Object);
        var repo = new PostgresUserRepo(mockDbContext.Object);

        var result = repo.TryGet(userName);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
        Assert.Equal("+79161648345", result.Number.StringNumber);
        Assert.Equal("password_hash", result.PasswordHash);
        Assert.NotNull(result.Settings);
        Assert.Single(result.Settings.SettingsTimes);
    }

    [Fact]
    public void TryGet_WhenUserNotExists_ShouldReturnNull()
    {
        // Arrange
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();

        var userName = "non_existent_user";
        var emptyUsers = new List<DBUser>().AsQueryable();

        SetupMockDbSet(mockUsersDbSet, emptyUsers);
        mockUsersDbSet.Setup(d => d.Include(It.IsAny<string>())).Returns(mockUsersDbSet.Object);

        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);

        var repo = new PostgresUserRepo(mockDbContext.Object);

        // Act
        var result = repo.TryGet(userName);

        // Assert
        Assert.Null(result);
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
