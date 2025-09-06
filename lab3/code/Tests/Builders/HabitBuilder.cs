using Domain.Models;
using Types;

public class HabitBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Habit";
    private int _minsToComplete = 60;
    private List<ActualTime> _actualTimings = new();
    private List<PrefFixedTime> _prefFixedTimings = new();
    private TimeOption _option = TimeOption.NoMatter;
    private string _userName = "egor";
    private int _countInWeek = 1;

    public HabitBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    public HabitBuilder WithMinsToComplete(int minsToComplete)
    {
        _minsToComplete = minsToComplete;
        return this;
    }
    public HabitBuilder WithActualTiming(TimeOnly start, TimeOnly end, DayOfWeek day)
    {
        _actualTimings.Add(new ActualTime(Guid.NewGuid(), start, end, day, _id));
        return this;
    }
    public HabitBuilder WithPrefFixedTiming(TimeOnly start, TimeOnly end, DayOfWeek day)
    {
        _prefFixedTimings.Add(new PrefFixedTime(Guid.NewGuid(), start, end, _id));
        return this;
    }
    public HabitBuilder WithOption(TimeOption option)
    {
        _option = option;
        return this;
    }
    public HabitBuilder WithUserName(string userName)
    {
        _userName = userName;
        return this;
    }
    public HabitBuilder WithCountInWeek(int countInWeek)
    {
        _countInWeek = countInWeek;
        return this;
    }
    public Habit Build()
    {
        return new Habit(_id, _name, _minsToComplete, _option, _userName, _actualTimings, _prefFixedTimings, _countInWeek);
    }
}
