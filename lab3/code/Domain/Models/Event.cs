using Types;

namespace Domain.Models;

public class Event
{
    public Guid Id { get; }
    public string Name { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public DayOfWeek Day { get; }
    public string UserNameID { get; }
    public EventOption Option { get; }
    public DateOnly? EDate { get; }

    public Event(Guid id, string name, TimeOnly start, TimeOnly end, string userNameID, EventOption option, DayOfWeek? day, DateOnly? eDate)
    {
        if (option == EventOption.Once && eDate == null)
            throw new ArgumentException($"Event {name} with option 'Once' should have specified date");
        if (option == EventOption.EveryTwoWeeks && eDate == null)
            throw new ArgumentException($"Event {name} with option 'EveryTwoWeeks' should have specified date from which the two-week intervals start");
        if (option == EventOption.EveryWeek && day == null)
            throw new ArgumentException($"Event {name} with option 'EveryWeek' should have specified day");
        if (option == EventOption.EveryWeek && day != null)
        {
            Day = (DayOfWeek) day;
            EDate = null;
        }
        else if ((option == EventOption.Once || option == EventOption.EveryTwoWeeks) && eDate != null)
        {
            EDate = eDate;
            Day = ((DateOnly)eDate).DayOfWeek;
        }
        Id = id;
        Name = name;
        Start = start;
        End = end;
        Option = option;
        UserNameID = userNameID;
    }
    public override string ToString()
    {
        if (Option == EventOption.Once)
            return $"EVENT: Day = {Day}, Option = {Option}, Name = {Name}, Start = {Start}, End = {End}, Date = {EDate}\n";
        else if (Option == EventOption.EveryWeek)
            return $"EVENT: Day = {Day}, Option = {Option}, Name = {Name}, Start = {Start}, End = {End}\n";
        else if (Option == EventOption.EveryTwoWeeks)
        {
            int delta1 = DayOfWeek.Sunday - DateTime.Now.DayOfWeek;
            DateOnly event_date = DateOnly.FromDateTime(DateTime.Now.AddDays(delta1 + (int) Day));
            return $"EVENT: Day = {Day}, Option = {Option}, Name = {Name}," +
                $" Start = {Start}, End = {End}, Date = {event_date}\n";
        }
        else
            return "ERROR";
    }
}
