using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Storage.Models;
using Storage.PostgresStorageAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace Tests;

public class UnitTestsHabitRepo
{
    [Fact]
    public void TryGetUserExists()
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
            new DBHabit(Guid.NewGuid(), "habit", 30, TimeOption.Fixed, userName, 3)
            {
                ActualTimings = new List<DBActualTime> { new DBActualTime(Guid.NewGuid(), 
                    new TimeOnly(10, 0), new TimeOnly(10, 30), DayOfWeek.Monday, Guid.NewGuid()) },
                PrefFixedTimings = new List<DBPrefFixedTime> { new DBPrefFixedTime(Guid.NewGuid(), 
                    new TimeOnly(10, 0), new TimeOnly(10, 30), Guid.NewGuid()) }
            }
        }.AsQueryable();
        mockUsersDbSet.Setup(d => d.Find(userName)).Returns(testUser);
        mockHabitsDbSet.As<IQueryable<DBHabit>>()
            .Setup(m => m.Provider).Returns(dbHabits.Provider);
        mockHabitsDbSet.As<IQueryable<DBHabit>>()
            .Setup(m => m.Expression).Returns(dbHabits.Expression);
        mockHabitsDbSet.As<IQueryable<DBHabit>>()
            .Setup(m => m.ElementType).Returns(dbHabits.ElementType);
        mockHabitsDbSet.As<IQueryable<DBHabit>>()
            .Setup(m => m.GetEnumerator()).Returns(dbHabits.GetEnumerator());
        mockDbContext.Setup(db => db.Users).Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        var repo = new PostgresHabitRepo(mockDbContext.Object);

        var result = repo.TryGet(userName);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("habit", result[0].Name);
    }

    [Fact]
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
    public void TryCreateHabitNotExists()
    {
        // Arrange
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockHabitsDbSet = new Mock<DbSet<DBHabit>>();
        var mockActualTimesDbSet = new Mock<DbSet<DBActualTime>>();
        var mockPrefFixedTimesDbSet = new Mock<DbSet<DBPrefFixedTime>>();

        var habit = new Habit(
            Guid.NewGuid(), "Test Habit", 30, TimeOption.Fixed, "test_user",
            new List<ActualTime> { new ActualTime(Guid.NewGuid(), new TimeOnly(9, 0), new TimeOnly(9, 30), DayOfWeek.Monday, Guid.NewGuid()) },
            new List<PrefFixedTime> { new PrefFixedTime(Guid.NewGuid(), new TimeOnly(10, 0), new TimeOnly(10, 30), Guid.NewGuid()) },
            3
        );

        mockHabitsDbSet.Setup(d => d.Find(habit.Id)).Returns((DBHabit)null);
        mockHabitsDbSet.Setup(d => d.Add(It.IsAny<DBHabit>())).Verifiable();
        mockActualTimesDbSet.Setup(d => d.AddRange(It.IsAny<IEnumerable<DBActualTime>>())).Verifiable();
        mockPrefFixedTimesDbSet.Setup(d => d.AddRange(It.IsAny<IEnumerable<DBPrefFixedTime>>())).Verifiable();

        mockDbContext.Setup(db => db.Habits).Returns(mockHabitsDbSet.Object);
        mockDbContext.Setup(db => db.ActualTimes).Returns(mockActualTimesDbSet.Object);
        mockDbContext.Setup(db => db.PrefFixedTimes).Returns(mockPrefFixedTimesDbSet.Object);
        mockDbContext.Setup(db => db.SaveChanges()).Returns(1);

        var repo = new PostgresHabitRepo(mockDbContext.Object);

        // Act
        var result = repo.TryCreate(habit);

        // Assert
        Assert.True(result);
        mockHabitsDbSet.Verify(d => d.Add(It.IsAny<DBHabit>()), Times.Once);
        mockActualTimesDbSet.Verify(d => d.AddRange(It.IsAny<IEnumerable<DBActualTime>>()), Times.Once);
        mockPrefFixedTimesDbSet.Verify(d => d.AddRange(It.IsAny<IEnumerable<DBPrefFixedTime>>()), Times.Once);
        mockDbContext.Verify(db => db.SaveChanges(), Times.Once);
    }
}
