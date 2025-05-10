using Domain;
using Domain.OutPorts;
using Domain.Models;
using Types;

namespace LoadAdapters;

public class DummyShedAdapter: IShedLoad
{
    public List<Event> LoadShedule(string user_name, string path)
    {
        List<Event> events = new();
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Monday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), DayOfWeek.Monday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Monday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Monday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Tuesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(9, 0, 0), DayOfWeek.Tuesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Tuesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Tuesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Wednesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Thursday, user_name));
        //events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Friday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(9, 0, 0), DayOfWeek.Friday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(21, 50, 0), DayOfWeek.Friday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_name));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(16, 0, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(16, 0, 0), new TimeOnly(19, 20, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Saturday, user_name));

        //events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Sunday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(14, 30, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(16, 0, 0), new TimeOnly(21, 50, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Saturday, user_name));
        return events;
    }
    /*public List<Event> LoadShedule(string user_name, string path)
    {
        List<Event> events = new();

        if (!File.Exists(path))
        {
            Console.WriteLine($"Файл не найден: {path}");
            return events;
        }
        var lines = File.ReadAllLines(path);

        foreach (var line in lines)
        {
            // Пропускаем пустые строки и комментарии
            if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                continue;

            // Название|ДеньНедели|ВремяНачала|ВремяОкончания
            var parts = line.Split('|');
            if (parts.Length != 4)
            {
                Console.WriteLine($"Неверный формат строки: {line}");
                continue;
            }

            var name = parts[0].Trim();
            var day = ParseDayOfWeek(parts[1].Trim());
            if (day == null)
            {
                Console.WriteLine($"Ошибка парсинга строки '{line}'\n");
                continue;
            }
            var startTime = TimeOnly.Parse(parts[2].Trim());
            var endTime = TimeOnly.Parse(parts[3].Trim());

            events.Add(new Event(Guid.NewGuid(), name, startTime, endTime, (DayOfWeek) day, user_name));
        }
        return events;
    }

    private DayOfWeek? ParseDayOfWeek(string dayString)
    {
        return dayString.ToLower() switch
        {
            "понедельник" or "monday" => DayOfWeek.Monday,
            "вторник" or "tuesday" => DayOfWeek.Tuesday,
            "среда" or "wednesday" => DayOfWeek.Wednesday,
            "четверг" or "thursday" => DayOfWeek.Thursday,
            "пятница" or "friday" => DayOfWeek.Friday,
            "суббота" or "saturday" => DayOfWeek.Saturday,
            "воскресенье" or "sunday" => DayOfWeek.Sunday,
            _ => null
        };
    }*/
}