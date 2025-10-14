using Microsoft.EntityFrameworkCore;
using Moq;
using Storage.EfAdapters;
using Storage.Models;
using Domain.Models;
using Tests.ObjectMothers;
using Types;
using Allure.Xunit.Attributes;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Tests.UnitTests;

public class UnitTestsEventRepo
{
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест получения событий пользователя когда он существует")]
    public void TryGetForValidUser()
    {
        var userName = "test";
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var testUser = new DBUser(userName, "+71111111111", "test");
        mockUsersDbSet.Setup(d => d.Find(userName))
                     .Returns(testUser);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        List<Event> events = [];
        events.AddRange(TaskTrackerMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(TaskTrackerMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var dbEvents = events.Select(el => new DBEvent(el)).ToList();
        var queryableEvents = dbEvents.AsQueryable();
        SetupMockDbSet(mockEventsDbSet, queryableEvents);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryGet(userName);

        Assert.NotNull(result);
        Assert.Equal(events.Select(el => el.Id), result.Select(el => el.Id));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест получения событий пользователя когда он существует")]
    public async Task TryGetAsyncForValidUser()
    {
        var userName = "test";
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var testUser = new DBUser(userName, "+71111111111", "test");
        mockUsersDbSet.Setup(d => d.FindAsync(userName))
                     .ReturnsAsync(testUser);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        List<Event> events = [];
        events.AddRange(TaskTrackerMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(TaskTrackerMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var dbEvents = events.Select(el => new DBEvent(el)).ToList();
        var queryableEvents = dbEvents.AsQueryable();
        SetupMockDbSetForAsync(mockEventsDbSet, queryableEvents);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryGetAsync(userName);

        Assert.NotNull(result);
        Assert.Equal(events.Select(el => el.Id), result.Select(el => el.Id));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест получения событий пользователя когда он не существует")]
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
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryGet(notExistUserName);

        Assert.Null(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест получения событий пользователя когда он не существует")]
    public async Task TryGetAsyncForInvalidUser()
    {
        var notExistUserName = "test";
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        mockUsersDbSet.Setup(d => d.Find(notExistUserName))
                     .Returns((DBUser?)null);
        mockUsersDbSet.Setup(d => d.FindAsync(notExistUserName))
                     .ReturnsAsync((DBUser?)null);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryGetAsync(notExistUserName);

        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания события которого еще не существует")]
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
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryCreate(newEvent);

        Assert.True(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания события которого еще не существует")]
    public async Task TryCreateAsyncEventNotExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var newEvent = new Event(Guid.NewGuid(), "test",
            new TimeOnly(10, 0), new TimeOnly(11, 0), "test",
            EventOption.Once, DayOfWeek.Monday, DateOnly.FromDateTime(DateTime.Now));
        mockEventsDbSet.Setup(d => d.Find(newEvent.Id))
                     .Returns((DBEvent?)null);
        mockEventsDbSet.Setup(d => d.FindAsync(newEvent.Id))
                     .ReturnsAsync((DBEvent?)null);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryCreateAsync(newEvent);

        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания события которое уже существует")]
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
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryCreate(existingEvent);

        Assert.False(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания события которое уже существует")]
    public async Task TryCreateAsyncEventAlreadyExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();

        var existingEvent = new Event(Guid.NewGuid(), "test",
            new TimeOnly(10, 0), new TimeOnly(11, 0), "test",
            EventOption.Once, DayOfWeek.Monday, DateOnly.FromDateTime(DateTime.Now));
        var existingDbEvent = new DBEvent(existingEvent);
        mockEventsDbSet.Setup(d => d.FindAsync(existingEvent.Id))
                     .ReturnsAsync(existingDbEvent);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryCreateAsync(existingEvent);

        Assert.False(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания нескольких еще не существующих событий")]
    public void TryCreateManyEventsNotExisting()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var events = new List<Event>
        {
            new Event(Guid.NewGuid(), "1", new TimeOnly(10, 0), new TimeOnly(11, 0),
                "test", EventOption.Once, DayOfWeek.Monday, DateOnly.FromDateTime(DateTime.Now)),
            new Event(Guid.NewGuid(), "2", new TimeOnly(12, 0), new TimeOnly(13, 0),
                "test", EventOption.EveryWeek, DayOfWeek.Tuesday, null)
        };
        mockEventsDbSet.Setup(d => d.Find(It.IsAny<Guid>()))
                     .Returns((DBEvent?)null);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryCreateMany(events);

        Assert.True(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания нескольких еще не существующих событий")]
    public async Task TryCreateManyAsyncEventsNotExisting()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var events = new List<Event>
        {
            new Event(Guid.NewGuid(), "1", new TimeOnly(10, 0), new TimeOnly(11, 0),
                "test", EventOption.Once, DayOfWeek.Monday, DateOnly.FromDateTime(DateTime.Now)),
            new Event(Guid.NewGuid(), "2", new TimeOnly(12, 0), new TimeOnly(13, 0),
                "test", EventOption.EveryWeek, DayOfWeek.Tuesday, null)
        };
        mockEventsDbSet.Setup(d => d.FindAsync(It.IsAny<Guid>()))
                     .ReturnsAsync((DBEvent?)null);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryCreateManyAsync(events);

        Assert.True(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания нескольких еще не существующих событий, одно из которых уже существует")]
    public void TryCreateManyEventsAlreadyExisting()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var events = new List<Event>
        {
            new Event(Guid.NewGuid(), "1", new TimeOnly(10, 0), new TimeOnly(11, 0),
                "test", EventOption.Once, DayOfWeek.Monday, DateOnly.FromDateTime(DateTime.Now)),
            new Event(Guid.NewGuid(), "2", new TimeOnly(12, 0), new TimeOnly(13, 0),
                "test", EventOption.EveryWeek, DayOfWeek.Tuesday, null)
        };
        var existingDbEvent = events[0];
        mockEventsDbSet.Setup(d => d.Find(events[0].Id))
                     .Returns(new DBEvent(existingDbEvent));
        mockEventsDbSet.Setup(d => d.Find(events[1].Id))
                     .Returns((DBEvent?)null);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryCreateMany(events);

        Assert.False(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест создания нескольких еще не существующих событий, одно из которых уже существует")]
    public async Task TryCreateManyAsyncEventsAlreadyExisting()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var events = new List<Event>
        {
            new Event(Guid.NewGuid(), "1", new TimeOnly(10, 0), new TimeOnly(11, 0),
                "test", EventOption.Once, DayOfWeek.Monday, DateOnly.FromDateTime(DateTime.Now)),
            new Event(Guid.NewGuid(), "2", new TimeOnly(12, 0), new TimeOnly(13, 0),
                "test", EventOption.EveryWeek, DayOfWeek.Tuesday, null)
        };
        var existingDbEvent = events[0];
        mockEventsDbSet.Setup(d => d.FindAsync(events[0].Id))
                     .ReturnsAsync(new DBEvent(existingDbEvent));
        mockEventsDbSet.Setup(d => d.FindAsync(events[1].Id))
                     .ReturnsAsync((DBEvent?)null);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryCreateManyAsync(events);

        Assert.False(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест обновления существующего события")]
    public void TryUpdateEventExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var newName = "New";
        var newStart = new TimeOnly(12, 0);
        var newEnd = new TimeOnly(13, 0);
        var newDay = DayOfWeek.Tuesday;
        var newOption = EventOption.EveryWeek;
        var userName = "test";
        DateOnly? newDate = null;
        var originalEvent = new Event(Guid.NewGuid(), "Old", new TimeOnly(10, 0),
            new TimeOnly(11, 0), userName, EventOption.Once, DayOfWeek.Monday,
            DateOnly.FromDateTime(DateTime.Now));
        var updatedEvent = new Event(originalEvent.Id, newName, newStart,
            newEnd, userName, newOption, newDay, newDate);
        var existingDbEvent = new DBEvent(originalEvent);
        mockEventsDbSet.Setup(d => d.Find(updatedEvent.Id))
                     .Returns(existingDbEvent);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryUpdate(updatedEvent);

        Assert.True(result);
        Assert.Equal(newName, existingDbEvent.Name);
        Assert.Equal(newStart, existingDbEvent.Start);
        Assert.Equal(newEnd, existingDbEvent.End);
        Assert.Equal(newDay, existingDbEvent.Day);
        Assert.Equal(newOption, existingDbEvent.Option);
        Assert.Equal(newDate, existingDbEvent.EDate);
        Assert.Equal(userName, existingDbEvent.DBUserNameID);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест обновления существующего события")]
    public async Task TryUpdateAsyncEventExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var newName = "New";
        var newStart = new TimeOnly(12, 0);
        var newEnd = new TimeOnly(13, 0);
        var newDay = DayOfWeek.Tuesday;
        var newOption = EventOption.EveryWeek;
        var userName = "test";
        DateOnly? newDate = null;
        var originalEvent = new Event(Guid.NewGuid(), "Old", new TimeOnly(10, 0),
            new TimeOnly(11, 0), userName, EventOption.Once, DayOfWeek.Monday,
            DateOnly.FromDateTime(DateTime.Now));
        var updatedEvent = new Event(originalEvent.Id, newName, newStart,
            newEnd, userName, newOption, newDay, newDate);
        var existingDbEvent = new DBEvent(originalEvent);
        mockEventsDbSet.Setup(d => d.FindAsync(updatedEvent.Id))
                     .ReturnsAsync(existingDbEvent);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryUpdateAsync(updatedEvent);

        Assert.True(result);
        Assert.Equal(newName, existingDbEvent.Name);
        Assert.Equal(newStart, existingDbEvent.Start);
        Assert.Equal(newEnd, existingDbEvent.End);
        Assert.Equal(newDay, existingDbEvent.Day);
        Assert.Equal(newOption, existingDbEvent.Option);
        Assert.Equal(newDate, existingDbEvent.EDate);
        Assert.Equal(userName, existingDbEvent.DBUserNameID);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест обновления несуществующего события")]
    public void TryUpdateEventNotExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();

        var ev = new Event(Guid.NewGuid(), "test", new TimeOnly(10, 0),
            new TimeOnly(11, 0), "test", EventOption.Once, DayOfWeek.Monday,
            DateOnly.FromDateTime(DateTime.Now));

        mockEventsDbSet.Setup(d => d.Find(ev.Id))
                     .Returns((DBEvent?)null);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryUpdate(ev);

        Assert.False(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест обновления несуществующего события")]
    public async Task TryUpdateAsyncEventNotExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();

        var ev = new Event(Guid.NewGuid(), "test", new TimeOnly(10, 0),
            new TimeOnly(11, 0), "test", EventOption.Once, DayOfWeek.Monday,
            DateOnly.FromDateTime(DateTime.Now));

        mockEventsDbSet.Setup(d => d.FindAsync(ev.Id))
                     .ReturnsAsync((DBEvent?)null);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryUpdateAsync(ev);

        Assert.False(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления существующего события")]
    public void TryDeleteEventExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var eventId = Guid.NewGuid();
        var existingDbEvent = new DBEvent(eventId, "Event", new TimeOnly(10, 0),
            new TimeOnly(11, 0), "user1", EventOption.Once, DayOfWeek.Monday,
            DateOnly.FromDateTime(DateTime.Now));

        mockEventsDbSet.Setup(d => d.Find(eventId))
                     .Returns(existingDbEvent);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryDelete(eventId);

        Assert.True(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления существующего события")]
    public async Task TryDeleteAsyncEventExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var eventId = Guid.NewGuid();
        var existingDbEvent = new DBEvent(eventId, "Event", new TimeOnly(10, 0),
            new TimeOnly(11, 0), "user1", EventOption.Once, DayOfWeek.Monday,
            DateOnly.FromDateTime(DateTime.Now));

        mockEventsDbSet.Setup(d => d.FindAsync(eventId))
                     .ReturnsAsync(existingDbEvent);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryDeleteAsync(eventId);

        Assert.True(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления несуществующего события")]
    public void TryDeleteEventNotExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var eventId = Guid.NewGuid();
        mockEventsDbSet.Setup(d => d.Find(eventId))
                     .Returns((DBEvent?)null);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryDelete(eventId);

        Assert.False(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления несуществующего события")]
    public async Task TryDeleteAsyncEventNotExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var eventId = Guid.NewGuid();
        mockEventsDbSet.Setup(d => d.FindAsync(eventId))
                     .ReturnsAsync((DBEvent?)null);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryDeleteAsync(eventId);

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления событий у существующего пользователя")]
    public void TryDeleteEventsUserExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var userName = "test";
        var existingUser = new DBUser(userName, "+71111111111", "test");
        var events = new List<Event>
        {
            new Event(Guid.NewGuid(), "1", new TimeOnly(10, 0), new TimeOnly(11, 0),
                "test", EventOption.Once, DayOfWeek.Monday, DateOnly.FromDateTime(DateTime.Now)),
            new Event(Guid.NewGuid(), "2", new TimeOnly(12, 0), new TimeOnly(13, 0),
                "test", EventOption.EveryWeek, DayOfWeek.Tuesday, null)
        };
        var dbEvents = events.Select(el => new DBEvent(el)).ToList();
        var queryableEvents = dbEvents.AsQueryable();
        SetupMockDbSet(mockEventsDbSet, queryableEvents);
        mockUsersDbSet.Setup(d => d.Find(userName))
                     .Returns(existingUser);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryDeleteEvents(userName);

        Assert.True(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления событий у существующего пользователя")]
    public async Task TryDeleteAsyncEventsUserExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var userName = "test";
        var existingUser = new DBUser(userName, "+71111111111", "test");
        var events = new List<Event>
        {
            new Event(Guid.NewGuid(), "1", new TimeOnly(10, 0), new TimeOnly(11, 0),
                "test", EventOption.Once, DayOfWeek.Monday, DateOnly.FromDateTime(DateTime.Now)),
            new Event(Guid.NewGuid(), "2", new TimeOnly(12, 0), new TimeOnly(13, 0),
                "test", EventOption.EveryWeek, DayOfWeek.Tuesday, null)
        };
        var dbEvents = events.Select(el => new DBEvent(el)).ToList();
        var queryableEvents = dbEvents.AsQueryable();
        SetupMockDbSetForAsync(mockEventsDbSet, queryableEvents);
        mockUsersDbSet.Setup(d => d.FindAsync(userName))
                     .ReturnsAsync(existingUser);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryDeleteEventsAsync(userName);

        Assert.True(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест удаления событий у несуществующего пользователя")]
    public void TryDeleteEventsUserNoExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "test";
        mockUsersDbSet.Setup(d => d.Find(userName))
                     .Returns((DBUser?)null);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryDeleteEvents(userName);

        Assert.False(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест удаления событий у несуществующего пользователя")]
    public async Task TryDeleteAsyncEventsUserNoExist()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var userName = "test";
        mockUsersDbSet.Setup(d => d.FindAsync(userName))
                     .ReturnsAsync((DBUser?)null);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryDeleteEventsAsync(userName);

        Assert.False(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест замены событий у существующего пользователя")]
    public void TryReplaceEventsValidUser()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var userName = "test";
        var existingUser = new DBUser(userName, "+71111111111", "test");
        var newEvents = new List<Event>
        {
            new Event(Guid.NewGuid(), "new1", new TimeOnly(10, 0),
                new TimeOnly(11, 0), userName, EventOption.Once, DayOfWeek.Monday,
                DateOnly.FromDateTime(DateTime.Now)),
            new Event(Guid.NewGuid(), "new2", new TimeOnly(12, 0), 
                new TimeOnly(13, 0), userName, EventOption.EveryWeek, DayOfWeek.Tuesday, null)
        };
        //TryDeleteEvents
        mockUsersDbSet.Setup(d => d.Find(userName))
                     .Returns(existingUser);

        var emptyEvents = new List<DBEvent>().AsQueryable();
        SetupMockDbSet(mockEventsDbSet, emptyEvents);
        //TryCreateMany
        mockEventsDbSet.Setup(d => d.Find(It.IsAny<Guid>()))
                     .Returns((DBEvent?)null);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        mockDbContext.Setup(db => db.SaveChanges())
                    .Returns(1);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryReplaceEvents(newEvents, userName);

        Assert.True(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест замены событий у существующего пользователя")]
    public async Task TryReplaceAsyncEventsValidUser()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var userName = "test";
        var existingUser = new DBUser(userName, "+71111111111", "test");
        var newEvents = new List<Event>
        {
            new Event(Guid.NewGuid(), "new1", new TimeOnly(10, 0),
                new TimeOnly(11, 0), userName, EventOption.Once, DayOfWeek.Monday,
                DateOnly.FromDateTime(DateTime.Now)),
            new Event(Guid.NewGuid(), "new2", new TimeOnly(12, 0),
                new TimeOnly(13, 0), userName, EventOption.EveryWeek, DayOfWeek.Tuesday, null)
        };
        //TryDeleteEvents
        mockUsersDbSet.Setup(d => d.FindAsync(userName))
                     .ReturnsAsync(existingUser);
        var emptyEvents = new List<DBEvent>().AsQueryable();
        SetupMockDbSetForAsync(mockEventsDbSet, emptyEvents);
        //TryCreateMany
        mockEventsDbSet.Setup(d => d.FindAsync(It.IsAny<Guid>()))
                     .ReturnsAsync((DBEvent?)null);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryReplaceEventsAsync(newEvents, userName);

        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест замены событий не все из для одного пользователя")]
    public void TryReplaceEventsNotAllForOneUser()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var userName = "test";
        var otherUserName = "other";
        var events = new List<Event>
        {
            new Event(Guid.NewGuid(), "new1", new TimeOnly(10, 0),
                new TimeOnly(11, 0), userName, EventOption.Once, DayOfWeek.Monday,
                DateOnly.FromDateTime(DateTime.Now)),
            new Event(Guid.NewGuid(), "new2", new TimeOnly(12, 0),
                new TimeOnly(13, 0), otherUserName, EventOption.EveryWeek,
                DayOfWeek.Tuesday, null)
        };
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = repo.TryReplaceEvents(events, userName);

        Assert.False(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    [AllureFeature("EventRepo")]
    [AllureStory("Асинхронные методы")]
    [AllureDescription("Тест замены событий не все из для одного пользователя")]
    public async Task TryReplaceAsyncEventsNotAllForOneUser()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var userName = "test";
        var otherUserName = "other";
        var events = new List<Event>
        {
            new Event(Guid.NewGuid(), "new1", new TimeOnly(10, 0),
                new TimeOnly(11, 0), userName, EventOption.Once, DayOfWeek.Monday,
                DateOnly.FromDateTime(DateTime.Now)),
            new Event(Guid.NewGuid(), "new2", new TimeOnly(12, 0),
                new TimeOnly(13, 0), otherUserName, EventOption.EveryWeek,
                DayOfWeek.Tuesday, null)
        };
        var repo = new EfEventRepo(mockDbContext.Object);

        var result = await repo.TryReplaceEventsAsync(events, userName);

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
