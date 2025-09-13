using Microsoft.EntityFrameworkCore;
using Moq;
using Storage.PostgresStorageAdapters;
using Storage.Models;
using Domain.Models;
using Tests.ObjectMothers;
using Types;
using Allure.Xunit.Attributes;

namespace Tests.UnitTests;

public class UnitTestsEventRepo
{
    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест получения событий пользователя когда он существует")]
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
        events.AddRange(TaskTrackerMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(TaskTrackerMother.DefaultDayShedule(userName, DayOfWeek.Monday));
        var dbEvents = events.Select(el => new DBEvent(el)).ToList();
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

        Assert.Null(result);
        Assert.Equal(events.Select(el => el.Id), result.Select(el => el.Id));
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
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
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryGet(notExistUserName);

        Assert.Null(result);
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
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
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryCreate(newEvent);

        Assert.True(result);
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
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
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryCreate(existingEvent);

        Assert.False(result);
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
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
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryCreateMany(events);

        Assert.True(result);
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
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
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryCreateMany(events);

        Assert.False(result);
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
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
        var repo = new PostgresEventRepo(mockDbContext.Object);

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
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
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
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryUpdate(ev);

        Assert.False(result);
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
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
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryDelete(eventId);

        Assert.True(result);
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
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
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryDelete(eventId);

        Assert.False(result);
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест удаления событий у существующего пользователя")]
    public void TryDeleteEventsUserExists()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var userName = "test";
        var existingUser = new DBUser(userName, "+79161648345", "test");
        var events = new List<Event>
        {
            new Event(Guid.NewGuid(), "1", new TimeOnly(10, 0), new TimeOnly(11, 0),
                "test", EventOption.Once, DayOfWeek.Monday, DateOnly.FromDateTime(DateTime.Now)),
            new Event(Guid.NewGuid(), "2", new TimeOnly(12, 0), new TimeOnly(13, 0),
                "test", EventOption.EveryWeek, DayOfWeek.Tuesday, null)
        };
        var dbEvents = events.Select(el => new DBEvent(el)).ToList();
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
        mockUsersDbSet.Setup(d => d.Find(userName))
                     .Returns(existingUser);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryDeleteEvents(userName);

        Assert.True(result);
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
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
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryDeleteEvents(userName);

        Assert.False(result);
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
    [AllureDescription("Тест замены событий у существующего пользователя")]
    public void TryReplaceEventsValidUser()
    {
        var mockDbContext = new Mock<ITaskTrackerContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();
        var userName = "test";
        var existingUser = new DBUser(userName, "+79161648345", "test");
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
        mockEventsDbSet.As<IQueryable<DBEvent>>()
            .Setup(m => m.Provider)
            .Returns(emptyEvents.Provider);
        mockEventsDbSet.As<IQueryable<DBEvent>>()
            .Setup(m => m.Expression)
            .Returns(emptyEvents.Expression);
        mockEventsDbSet.As<IQueryable<DBEvent>>()
            .Setup(m => m.ElementType)
            .Returns(emptyEvents.ElementType);
        //TryCreateMany
        mockEventsDbSet.Setup(d => d.Find(It.IsAny<Guid>()))
                     .Returns((DBEvent?)null);
        mockDbContext.Setup(db => db.Users)
                    .Returns(mockUsersDbSet.Object);
        mockDbContext.Setup(db => db.Events)
                    .Returns(mockEventsDbSet.Object);
        mockDbContext.Setup(db => db.SaveChanges())
                    .Returns(1);
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryReplaceEvents(newEvents, userName);

        Assert.True(result);
    }

    [Fact]
    [AllureFeature("EventRepo")]
    [AllureStory("Тесты репозитория")]
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
        var repo = new PostgresEventRepo(mockDbContext.Object);

        var result = repo.TryReplaceEvents(events, userName);

        Assert.False(result);
    }
}
