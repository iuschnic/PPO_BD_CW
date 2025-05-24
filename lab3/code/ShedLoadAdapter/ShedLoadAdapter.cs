using Domain;
using Domain.OutPorts;
using Domain.Models;
using Types;
using CsvHelper.Configuration;
using CsvHelper;
using Ical.Net;
using System.Globalization;

namespace LoadAdapters;

public class DummyShedAdapter : ISheduleLoad
{
    public List<Event> LoadDummyShedule(string user_name, string path)
    {
        List<Event> events = new();
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Monday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 30, 0), new TimeOnly(9, 0, 0), DayOfWeek.Monday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Monday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Monday, user_name));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Tuesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(9, 0, 0), DayOfWeek.Tuesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(21, 0, 0), DayOfWeek.Tuesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Tuesday, user_name));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Wednesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(9, 0, 0), DayOfWeek.Wednesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(21, 0, 0), DayOfWeek.Wednesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Wednesday, user_name));
;

        events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Thursday, user_name));

        //events.Add(new Event(Guid.NewGuid(), "Заглушка", new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_name));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Friday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(9, 0, 0), DayOfWeek.Friday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(9, 0, 0), new TimeOnly(23, 50, 0), DayOfWeek.Friday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Friday, user_name));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(2, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(9, 0, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Бассейн", new TimeOnly(13, 0, 0), new TimeOnly(14, 30, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Бассейн", new TimeOnly(13, 0, 0), new TimeOnly(14, 30, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Saturday, user_name));

        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(0, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Sunday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Завтрак", new TimeOnly(8, 0, 0), new TimeOnly(16, 0, 0), DayOfWeek.Sunday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Работа", new TimeOnly(16, 0, 0), new TimeOnly(18, 20, 0), DayOfWeek.Sunday, user_name));
        events.Add(new Event(Guid.NewGuid(), "Сон", new TimeOnly(23, 0, 0), new TimeOnly(23, 59, 59), DayOfWeek.Sunday, user_name));
        return events;
    }
    public List<Event> LoadSheduleForMeasures(string user_name)
    {
        List<Event> events = new();
        events.Add(new Event(Guid.NewGuid(), "1", new TimeOnly(1, 0, 0), new TimeOnly(2, 0, 0), DayOfWeek.Monday, user_name));
        events.Add(new Event(Guid.NewGuid(), "2", new TimeOnly(6, 0, 0), new TimeOnly(7, 0, 0), DayOfWeek.Monday, user_name));
        events.Add(new Event(Guid.NewGuid(), "3", new TimeOnly(11, 0, 0), new TimeOnly(12, 0, 0), DayOfWeek.Monday, user_name));
        events.Add(new Event(Guid.NewGuid(), "4", new TimeOnly(16, 0, 0), new TimeOnly(17, 0, 0), DayOfWeek.Monday, user_name));

        events.Add(new Event(Guid.NewGuid(), "1", new TimeOnly(2, 0, 0), new TimeOnly(3, 0, 0), DayOfWeek.Tuesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "2", new TimeOnly(7, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Tuesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "3", new TimeOnly(12, 0, 0), new TimeOnly(13, 0, 0), DayOfWeek.Tuesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "4", new TimeOnly(17, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Tuesday, user_name));

        events.Add(new Event(Guid.NewGuid(), "1", new TimeOnly(3, 0, 0), new TimeOnly(4, 0, 0), DayOfWeek.Wednesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "2", new TimeOnly(8, 0, 0), new TimeOnly(9, 0, 0), DayOfWeek.Wednesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "3", new TimeOnly(13, 0, 0), new TimeOnly(14, 0, 0), DayOfWeek.Wednesday, user_name));
        events.Add(new Event(Guid.NewGuid(), "4", new TimeOnly(18, 0, 0), new TimeOnly(19, 0, 0), DayOfWeek.Wednesday, user_name));

        events.Add(new Event(Guid.NewGuid(), "1", new TimeOnly(4, 0, 0), new TimeOnly(5, 0, 0), DayOfWeek.Thursday, user_name));
        events.Add(new Event(Guid.NewGuid(), "2", new TimeOnly(9, 0, 0), new TimeOnly(10, 0, 0), DayOfWeek.Thursday, user_name));
        events.Add(new Event(Guid.NewGuid(), "3", new TimeOnly(14, 0, 0), new TimeOnly(15, 0, 0), DayOfWeek.Thursday, user_name));
        events.Add(new Event(Guid.NewGuid(), "4", new TimeOnly(19, 0, 0), new TimeOnly(20, 0, 0), DayOfWeek.Thursday, user_name));

        events.Add(new Event(Guid.NewGuid(), "1", new TimeOnly(5, 0, 0), new TimeOnly(6, 0, 0), DayOfWeek.Friday, user_name));
        events.Add(new Event(Guid.NewGuid(), "2", new TimeOnly(10, 0, 0), new TimeOnly(11, 0, 0), DayOfWeek.Friday, user_name));
        events.Add(new Event(Guid.NewGuid(), "3", new TimeOnly(15, 0, 0), new TimeOnly(16, 0, 0), DayOfWeek.Friday, user_name));
        events.Add(new Event(Guid.NewGuid(), "4", new TimeOnly(20, 0, 0), new TimeOnly(21, 0, 0), DayOfWeek.Friday, user_name));

        events.Add(new Event(Guid.NewGuid(), "1", new TimeOnly(6, 0, 0), new TimeOnly(7, 0, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "2", new TimeOnly(11, 0, 0), new TimeOnly(12, 0, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "3", new TimeOnly(16, 0, 0), new TimeOnly(17, 0, 0), DayOfWeek.Saturday, user_name));
        events.Add(new Event(Guid.NewGuid(), "4", new TimeOnly(21, 0, 0), new TimeOnly(22, 0, 0), DayOfWeek.Saturday, user_name));

        events.Add(new Event(Guid.NewGuid(), "1", new TimeOnly(7, 0, 0), new TimeOnly(8, 0, 0), DayOfWeek.Sunday, user_name));
        events.Add(new Event(Guid.NewGuid(), "2", new TimeOnly(12, 0, 0), new TimeOnly(13, 0, 0), DayOfWeek.Sunday, user_name));
        events.Add(new Event(Guid.NewGuid(), "3", new TimeOnly(17, 0, 0), new TimeOnly(18, 0, 0), DayOfWeek.Sunday, user_name));
        events.Add(new Event(Guid.NewGuid(), "4", new TimeOnly(22, 0, 0), new TimeOnly(23, 0, 0), DayOfWeek.Sunday, user_name));
        return events;
    }
    public List<Event> LoadShedule(string user_name, string file_path)
    {
        string extension = Path.GetExtension(file_path);
        if (extension == ".csv")
        {
            return LoadCsv(user_name, file_path);
        }
        else
        {
            throw new Exception($"Не поддерживаемый формат файла - {extension}");
        }
    }

    private List<Event> LoadCsv(string user_name, string file_path)
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

                DayOfWeek? day = null;
                if (!string.IsNullOrEmpty(record.Day))
                    day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), record.Day);
                var newEvent = new Event(
                    id: Guid.NewGuid(),
                    name: record.Name,
                    start: timeStart,
                    end: timeEnd,
                    user_id: user_name,
                    day: (DayOfWeek) day
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
        public string? Day { get; set; }
    }
}