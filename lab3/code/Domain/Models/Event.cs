using Types;

namespace Domain.Models;

public class Event
{
    public Guid Id { get; }
    public string Name { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public DayOfWeek Day { get; }
    public Guid UserID { get; }

    public Event(Guid id, string name, TimeOnly start, TimeOnly end, DayOfWeek day, Guid user_id)
    {
        Id = id;
        Name = name;
        Start = start;
        End = end;
        Day = day;
        UserID = user_id;
    }
    public override string ToString()
    {
        return $"EVENT: Name = {Name}, Day = {Day}, Start = {Start}, End = {End}\n";
    }
}
