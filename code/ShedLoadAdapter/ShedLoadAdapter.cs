using Domain.OutPorts;
using Domain.Models;
using Types;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Ical.Net;

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
    public List<Event> LoadShedule(string user_name, string file_path)
    {
        string extension = Path.GetExtension(file_path);
        if (extension == ".csv")
        {
            if (!File.Exists(file_path))
            {
                throw new Exception($"Файла {file_path} не существует");
            }
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null
            };
            using var reader = new StreamReader(file_path);
            using var csv = new CsvReader(reader, config);

            return LoadCsv(user_name, csv);
        }
        else if (extension == ".ics")
        {
            if (!File.Exists(file_path))
                throw new FileNotFoundException("Файл не существует", file_path);

            var events = new List<Event>();
            var calendar = Ical.Net.Calendar.Load(File.ReadAllText(file_path));

            return LoadIcs(user_name, calendar);
        }
        else
        {
            throw new Exception($"Не поддерживаемый формат файла - {extension}");
        }
    }

    public List<Event> LoadShedule(string user_name, Stream stream, string extension)
    {

        if (extension == ".csv")
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null
            };
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, config);
            return LoadCsv(user_name, csv);
        }
        else if (extension == ".ics")
        {
            var calendar = Ical.Net.Calendar.Load(stream);
            return LoadIcs(user_name, calendar);
        }
        else
        {
            throw new Exception($"Не поддерживаемый формат файла - {extension}");
        }
    }

    private List<Event> LoadCsv(string user_name, CsvReader csv)
    {
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
                    day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), record.Day);

                DateOnly? date = null;
                if (!string.IsNullOrEmpty(record.Date))
                    date = DateOnly.Parse(record.Date);

                var newEvent = new Event(
                    id: Guid.NewGuid(),
                    name: record.Name,
                    start: timeStart,
                    end: timeEnd,
                    userNameID: user_name,
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
    public List<Event> LoadIcs(string user_name, Ical.Net.Calendar calendar)
    {
        var events = new List<Event>();
        foreach (var calendarEvent in calendar.Events)
        {
            try
            {
                var name = calendarEvent.Summary;
                var startTime = TimeOnly.FromDateTime(calendarEvent.DtStart.AsSystemLocal);
                var endTime = TimeOnly.FromDateTime(calendarEvent.DtEnd.AsSystemLocal);

                EventOption option = EventOption.EveryWeek;
                DayOfWeek? dayOfWeek = null;
                DateOnly? eventDate = null;

                if (calendarEvent.RecurrenceRules?.Any() == true)
                {
                    // Повторяющиеся события
                    var rrule = calendarEvent.RecurrenceRules.First();

                    if (rrule.Interval == 2 && rrule.Frequency == FrequencyType.Weekly)
                    {
                        option = EventOption.EveryTwoWeeks;
                        dayOfWeek = calendarEvent.DtStart.AsSystemLocal.DayOfWeek;
                        eventDate = DateOnly.FromDateTime(calendarEvent.DtStart.AsSystemLocal);
                    }
                    else if (rrule.Frequency == FrequencyType.Weekly)
                    {
                        option = EventOption.EveryWeek;
                        dayOfWeek = calendarEvent.DtStart.AsSystemLocal.DayOfWeek;
                    }
                }
                else
                {
                    // Одноразовые события
                    option = EventOption.Once;
                    eventDate = DateOnly.FromDateTime(calendarEvent.DtStart.AsSystemLocal);
                    dayOfWeek = calendarEvent.DtStart.AsSystemLocal.DayOfWeek;
                }

                events.Add(new Event(
                    id: Guid.NewGuid(),
                    name: name,
                    start: startTime,
                    end: endTime,
                    userNameID: user_name,
                    option: option,
                    day: dayOfWeek,
                    eDate: eventDate
                ));
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Ошибка обработки события: {calendarEvent.Summary}. {ex.Message}");
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
}