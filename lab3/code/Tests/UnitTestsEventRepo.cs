using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.OutPorts;
using Microsoft.EntityFrameworkCore;
using Moq;
using Storage.PostgresStorageAdapters;
using Storage.Models;
using Domain.Models;
using Tests.ObjectMothers;
using Types;

namespace Tests.UnitEventRepo;

public class UnitTestsEventRepo
{
    [Fact]
    public void TryGetForValidUser()
    {
        var userName = "test";
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var testUser = new DBUser(userName, "+79161648345", "test");
        mockUsersDbSet.Setup(d => d.Find(userName))
                     .Returns(testUser);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        List<Event> events = [];
        events.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var dbEvents = events.Select(el => new DBEvent(el)).ToList();
        //без этого не работает LINQ и тест падает!!
        var queryableEvents = dbEvents.AsQueryable();
        mockEventsDbSet.As<IQueryable<DBEvent>>()
            .Setup(m => m.Provider)
            .Returns(queryableEvents.Provider);
        mockEventsDbSet.As<IQueryable<DBEvent>>()
            .Setup(m => m.Expression)
            .Returns(queryableEvents.Expression);
        mockEventsDbSet.As<IQueryable<DBEvent>>()
            .Setup(m => m.ElementType)
            .Returns(queryableEvents.ElementType);
        mockEventsDbSet.As<IQueryable<DBEvent>>()
            .Setup(m => m.GetEnumerator())
            .Returns(queryableEvents.GetEnumerator());
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryGet(userName);

        Assert.NotNull(result);
        Assert.Equal(events.Select(el => el.Id), result.Select(el => el.Id));
    }

    [Fact]
    public void TryGetForInvalidUser()
    {
        var notExistUserName = "test";
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        mockUsersDbSet.Setup(d => d.Find(notExistUserName))
                     .Returns((DBUser?)null);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryGet(notExistUserName);

        Assert.Null(result);
    }

    [Fact]
    public void TryCreateEventNotExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var newEvent = new Event(Guid.NewGuid(), "test",
            new TimeOnly(10, 0), new TimeOnly(11, 0), "test",
            EventOption.Once, DayOfWeek.Monday, DateOnly.FromDateTime(DateTime.Now));
        mockEventsDbSet.Setup(d => d.Find(newEvent.Id))
                     .Returns((DBEvent?)null);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryCreate(newEvent);

        Assert.True(result);
    }

    [Fact]
    public void TryCreateEventAlreadyExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();

        var existingEvent = new Event(Guid.NewGuid(), "test",
            new TimeOnly(10, 0), new TimeOnly(11, 0), "test",
            EventOption.Once, DayOfWeek.Monday, DateOnly.FromDateTime(DateTime.Now));
        var existingDbEvent = new DBEvent(existingEvent);
        mockEventsDbSet.Setup(d => d.Find(existingEvent.Id))
                     .Returns(existingDbEvent);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryCreate(existingEvent);

        Assert.False(result);
    }
}
