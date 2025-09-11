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

namespace Tests.UnitEventRepo;

public class UnitTestsEventRepo
{
    [Fact]
    public void TryGetValidUser()
    {
        var userName = "test";
        var mockDbContext = new Mock<PostgresDBContext>();
        var mockUsersDbSet = new Mock<DbSet<DBUser>>();
        var mockEventsDbSet = new Mock<DbSet<DBEvent>>();

        var testUser = new DBUser(userName, "+79161648345", "test");
        mockUsersDbSet.Setup(d => d.Find(userName))
                     .Returns(testUser);

        // Создаем тестовые события
        List<Event> events = [];
        events.AddRange(HabitDistrMother.FullWeekFillerExceptDay(userName, DayOfWeek.Monday));
        events.AddRange(HabitDistrMother.DefaultDayShedule(userName, DayOfWeek.Monday));

        var DBEvents = new List<DBEvent>(events.Select(el => new DBEvent(el))).AsQueryable();

        // Настройка мока для Events DbSet с LINQ поддержкой
        mockEventsDbSet.As<IQueryable<Event>>()
            .Setup(m => m.Provider)
            .Returns(DBEvents.Provider);
        mockEventsDbSet.As<IQueryable<Event>>()
            .Setup(m => m.Expression)
            .Returns(DBEvents.Expression);
        mockEventsDbSet.As<IQueryable<Event>>()
            .Setup(m => m.ElementType)
            .Returns(DBEvents.ElementType);
        /*mockEventsDbSet.As<IQueryable<Event>>()
            .Setup(m => m.GetEnumerator())
            .Returns(DBEvents.GetEnumerator());*/

        // Настройка контекста
        mockDbContext.Setup(db => db.Users.Find(userName)).Returns(testUser);
        mockDbContext.Setup(db => db.Events).Returns(mockEventsDbSet.Object);

        var repo = new PostgresEventRepo(mockDbContext.Object);
        var result = repo.TryGet("test");

        Assert.NotNull(result);
        Assert.Equal(result.Select(el => el.Id), events.Select(el => el.Id));
    }
}
