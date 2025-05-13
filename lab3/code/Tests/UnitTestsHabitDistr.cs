using Domain;
using Domain.Models;
namespace Tests
{
    public class UnitTestsHabitDistr
    {
        /*Тест распределения одной привычки с не фиксированным временем выполнения
         на два дня, привычка по алгоритму должна однозначно распределиться на 18 00 на понедельник и больше не распределиться (заглушки мешают)*/
        [Fact]
        public void Test1()
        {
            string user_name = "egor";
            List<Habit> habits = [];
            List<Event> events = [];
            HabitDistributor distr = new();
            var h = new Habit(Guid.NewGuid(), "тест", 60, Types.TimeOption.NoMatter, user_name, [], [], 2);
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Tuesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Wednesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Thursday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Saturday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Sunday, user_name));
            habits.Add(h);
            var undistr = distr.DistributeHabits(habits, events);
            Assert.Single(undistr);
            Assert.Single(h.ActualTimings);
            Assert.Equal(new TimeOnly(18, 0, 0), h.ActualTimings[0].Start);
            Assert.Equal(new TimeOnly(19, 0, 0), h.ActualTimings[0].End);
        }
        /*Тест распределения привычки с фиксированным временем на 7 00 - 9 00 одноразово (заглушки не мешают), привычка по алгоритму должна
         распределиться на 8 00*/
        [Fact]
        public void Test2()
        {
            string user_name = "egor";
            List<Habit> habits = [];
            List<Event> events = [];
            HabitDistributor distr = new();
            var guid = Guid.NewGuid();
            var h = new Habit(guid, "тест", 30, Types.TimeOption.Fixed, user_name, [], 
                [new PrefFixedTime(Guid.NewGuid(), new TimeOnly(7, 0, 0), new TimeOnly(9, 0, 0), guid)], 1);
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Tuesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Wednesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Thursday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Saturday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Sunday, user_name));
            habits.Add(h);
            var undistr = distr.DistributeHabits(habits, events);
            Assert.Empty(undistr);
            Assert.Single(h.ActualTimings);
            Assert.Equal(new TimeOnly(8, 0, 0), h.ActualTimings[0].Start);
            Assert.Equal(new TimeOnly(8, 30, 0), h.ActualTimings[0].End);
        }
        /*Тест распределения привычки с предпочтительным временем на 7 00 - 9 00 одноразово (заглушки не мешают), привычка по алгоритму должна
         распределиться на 8 00*/
        [Fact]
        public void Test3()
        {
            string user_name = "egor";
            List<Habit> habits = [];
            List<Event> events = [];
            HabitDistributor distr = new();
            var guid = Guid.NewGuid();
            var h = new Habit(guid, "тест", 30, Types.TimeOption.Preffered, user_name, [],
                [new PrefFixedTime(Guid.NewGuid(), new TimeOnly(7, 0, 0), new TimeOnly(9, 0, 0), guid)], 1);
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Tuesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Wednesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Thursday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Saturday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Sunday, user_name));
            habits.Add(h);
            var undistr = distr.DistributeHabits(habits, events);
            Assert.Empty(undistr);
            Assert.Single(h.ActualTimings);
            Assert.Equal(new TimeOnly(8, 0, 0), h.ActualTimings[0].Start);
            Assert.Equal(new TimeOnly(8, 30, 0), h.ActualTimings[0].End);
        }
        /*Тест распределения привычки которая не влезает в расписание*/
        [Fact]
        public void Test4()
        {
            string user_name = "egor";
            List<Habit> habits = [];
            List<Event> events = [];
            HabitDistributor distr = new();
            var guid = Guid.NewGuid();
            var h = new Habit(guid, "тест", 500, Types.TimeOption.NoMatter, user_name, [],
                [new PrefFixedTime(Guid.NewGuid(), new TimeOnly(7, 0, 0), new TimeOnly(9, 0, 0), guid)], 1);
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Tuesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Wednesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Thursday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Saturday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Sunday, user_name));
            habits.Add(h);
            var undistr = distr.DistributeHabits(habits, events);
            Assert.Single(undistr);
            Assert.Empty(h.ActualTimings);
        }
        /*Тест распределения привычки с фиксированным временем которая влезает но не по фиксированному времени*/
        [Fact]
        public void Test5()
        {
            string user_name = "egor";
            List<Habit> habits = [];
            List<Event> events = [];
            HabitDistributor distr = new();
            var guid = Guid.NewGuid();
            var h = new Habit(guid, "тест", 10, Types.TimeOption.Fixed, user_name, [],
                [new PrefFixedTime(Guid.NewGuid(), new TimeOnly(9, 0, 0), new TimeOnly(17, 0, 0), guid)], 1);
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Tuesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Wednesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Thursday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Saturday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Sunday, user_name));
            habits.Add(h);
            var undistr = distr.DistributeHabits(habits, events);
            Assert.Single(undistr);
            Assert.Empty(h.ActualTimings);
        }
        /*Тест распределения привычки с предпочтительным временем которая влезает но не по указанному времени*/
        [Fact]
        public void Test6()
        {
            string user_name = "egor";
            List<Habit> habits = [];
            List<Event> events = [];
            HabitDistributor distr = new();
            var guid = Guid.NewGuid();
            var h = new Habit(guid, "тест", 10, Types.TimeOption.Preffered, user_name, [],
                [new PrefFixedTime(Guid.NewGuid(), new TimeOnly(9, 0, 0), new TimeOnly(17, 0, 0), guid)], 1);
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Tuesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Wednesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Thursday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Saturday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Sunday, user_name));
            habits.Add(h);
            var undistr = distr.DistributeHabits(habits, events);
            Assert.Empty(undistr);
            Assert.Single(h.ActualTimings);
            Assert.Equal(new TimeOnly(8, 0, 0), h.ActualTimings[0].Start);
            Assert.Equal(new TimeOnly(8, 10, 0), h.ActualTimings[0].End);
        }
        /*Тест распределения привычки с предпочтительным временем которая распределяется на вторник но не на понедельник*/
        [Fact]
        public void Test7()
        {
            string user_name = "egor";
            List<Habit> habits = [];
            List<Event> events = [];
            HabitDistributor distr = new();
            var guid = Guid.NewGuid();
            var h = new Habit(guid, "тест", 40, Types.TimeOption.Preffered, user_name, [],
                [new PrefFixedTime(Guid.NewGuid(), new TimeOnly(7, 0, 0), new TimeOnly(9, 0, 0), guid)], 1);
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Monday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Tuesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 50, 0), new TimeOnly(9, 0, 0), DayOfWeek.Tuesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Tuesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Tuesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Wednesday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Thursday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Saturday, user_name));
            events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Sunday, user_name));
            habits.Add(h);
            var undistr = distr.DistributeHabits(habits, events);
            Assert.Empty(undistr);
            Assert.Single(h.ActualTimings);
            Assert.Equal(DayOfWeek.Tuesday, h.ActualTimings[0].Day);
            Assert.Equal(new TimeOnly(8, 0, 0), h.ActualTimings[0].Start);
            Assert.Equal(new TimeOnly(8, 40, 0), h.ActualTimings[0].End);
        }
    }
}
