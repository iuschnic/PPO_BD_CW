using Types;

namespace Domain.Models;

public class Event
{
    public Guid Id { get; }
    public string Name { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public DayOfWeek Day { get; }
    public string? UserNameID { get; }

    public Event(Guid id, string name, TimeOnly start, TimeOnly end, DayOfWeek day, string user_id)
    {
        Id = id;
        Name = name;
        Start = start;
        End = end;
        Day = day;
        UserNameID = user_id;
    }
    public override string ToString()
    {
        return $"EVENT: Name = {Name}, Day = {Day}, Start = {Start}, End = {End}\n";
    }
}