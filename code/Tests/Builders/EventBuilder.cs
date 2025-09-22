using Domain.Models;
using Types;
namespace Tests.Builders;

public class EventBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Event";
    private TimeOnly _start = new(9, 0, 0);
    private TimeOnly _end = new(10, 0, 0);
    private DayOfWeek? _day = DayOfWeek.Monday;
    private string _userName = "Test User";
    private EventOption _option = EventOption.EveryWeek;
    private DateOnly? _eDate = null;

    public EventBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public EventBuilder WithTime(TimeOnly start, TimeOnly end)
    {
        if (start >= end)
            throw new ArgumentException("start < end!");

        _start = start;
        _end = end;
        return this;
    }
    public EventBuilder WithTime(string start, string end)
    {
        return WithTime(TimeOnly.Parse(start), TimeOnly.Parse(end));
    }
    public EventBuilder WithUserName(string userName)
    {
        _userName = userName;
        return this;
    }
    public EventBuilder AsOneTimeEvent(DateOnly date)
    {
        _option = EventOption.Once;
        _eDate = date;
        _day = ((DateOnly)_eDate).DayOfWeek;
        return this;
    }
    public EventBuilder AsWeeklyEvent(DayOfWeek day)
    {
        _option = EventOption.EveryWeek;
        _day = day;
        _eDate = null;
        return this;
    }
    public EventBuilder AsBiWeeklyEvent(DateOnly startDate)
    {
        _option = EventOption.EveryTwoWeeks;
        _eDate = startDate;
        _day = ((DateOnly)_eDate).DayOfWeek;
        return this;
    }

    public Event Build()
    {
        return new Event(_id, _name, _start, _end, _userName, _option, _day, _eDate);
    }
}
