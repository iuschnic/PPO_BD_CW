using Domain.OutPorts;
using Domain.Models;
using Types;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace LoadAdapters;

public class ShedAdapter : ISheduleLoad
{
    public List<Event> LoadDummyShedule(string user_name, string path)
    {
        List<Event> events = new();
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), 
            user_name, EventOption.EveryWeek, DayOfWeek.Monday, null));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Monday, null));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Monday, null));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59),
            user_name, EventOption.EveryWeek, DayOfWeek.Monday, null));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Tuesday, null));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(9, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Tuesday, null));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(20, 50, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Tuesday, null));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59),
            user_name, EventOption.EveryWeek, DayOfWeek.Tuesday, null));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Wednesday, null));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59),
            user_name, EventOption.EveryWeek, DayOfWeek.Wednesday, null));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Thursday, null));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59),
            user_name, EventOption.EveryWeek, DayOfWeek.Thursday, null));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Friday, null));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(9, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Friday, null));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(21, 50, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Friday, null));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59),
            user_name, EventOption.EveryWeek, DayOfWeek.Friday, null));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Saturday, null));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(16, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Saturday, null));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(16, 0, 0), new TimeOnly(19, 20, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Saturday, null));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59),
            user_name, EventOption.EveryWeek, DayOfWeek.Saturday, null));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Sunday, null));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(16, 0, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Sunday, null));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(16, 0, 0), new TimeOnly(18, 20, 0),
            user_name, EventOption.EveryWeek, DayOfWeek.Sunday, null));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59),
            user_name, EventOption.EveryWeek, DayOfWeek.Sunday, null));

        events.Add(new Event(Guid.NewGuid(), "Встреча с друзьми", new TimeOnly(16, 0, 0), new TimeOnly(22, 0, 0),
            user_name, EventOption.Once, null, new DateOnly(2025, 5, 21)));
        events.Add(new Event(Guid.NewGuid(), "Совещание", new TimeOnly(16, 0, 0), new TimeOnly(17, 30, 0),
            user_name, EventOption.Once, null, new DateOnly(2025, 5, 19)));
        events.Add(new Event(Guid.NewGuid(), "Корпоратив", new TimeOnly(18, 30, 0), new TimeOnly(22, 0, 0),
            user_name, EventOption.Once, null, new DateOnly(2025, 5, 25)));
        events.Add(new Event(Guid.NewGuid(), "Бассейн", new TimeOnly(18, 0, 0), new TimeOnly(19, 0, 0),
            user_name, EventOption.Once, null, new DateOnly(2025, 5, 21)));
        events.Add(new Event(Guid.NewGuid(), "Встреча с клиентом", new TimeOnly(19, 0, 0), new TimeOnly(20, 0, 0),
            user_name, EventOption.Once, null, new DateOnly(2025, 5, 26)));
        events.Add(new Event(Guid.NewGuid(), "Консультация1 раз в две недели", new TimeOnly(16, 0, 0), new TimeOnly(17, 0, 0),
            user_name, EventOption.Once, null, new DateOnly(2025, 5, 22)));
        events.Add(new Event(Guid.NewGuid(), "Консультация2 раз в две недели", new TimeOnly(16, 0, 0), new TimeOnly(17, 0, 0),
            user_name, EventOption.Once, null, new DateOnly(2025, 5, 15)));

        return events;
    }
    public List<Event> LoadShedule(string userName, string filePath)
    {
        // Проверка существования файла
        if (!File.Exists(filePath))
        {
            throw new Exception($"Файла {filePath} не существует");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        var records = new List<EventCsvRecord>();
        try
        {
            records = csv.GetRecords<EventCsvRecord>().ToList();
        }
        catch (CsvHelperException ex)
        {
            throw new InvalidDataException($"Ошибка чтения CSV файла: {ex.Message}");
        }

        var events = new List<Event>();
        foreach (var record in records)
        {
            try
            {
                var timeStart = TimeOnly.Parse(record.StartTime);
                var timeEnd = TimeOnly.Parse(record.EndTime);
                var option = (EventOption)Enum.Parse(typeof(EventOption), record.Option);

                DayOfWeek? day = null;
                if (!string.IsNullOrEmpty(record.Day))
                {
                    day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), record.Day);
                }

                DateOnly? date = null;
                if (!string.IsNullOrEmpty(record.Date))
                {
                    date = DateOnly.Parse(record.Date);
                }

                var newEvent = new Event(
                    id: Guid.NewGuid(),
                    name: record.Name,
                    start: timeStart,
                    end: timeEnd,
                    userNameID: userName,
                    option: option,
                    day: day,
                    eDate: date
                );

                events.Add(newEvent);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Ошибка обработки записи: {record.Name}. {ex.Message}");
            }
        }

        return events;
    }

    private class EventCsvRecord
    {
        public string? Name { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? Option { get; set; }
        public string? Day { get; set; }
        public string? Date { get; set; }
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