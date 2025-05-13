using Domain.Models;
namespace Domain.OutPorts;

public interface IHabitRepo
{
    List<Habit>? TryGet(string user_name);
    bool TryCreate(Habit habit);
    bool TryCreateMany(List<Habit> habits);
    bool TryUpdate(Habit habit);
    bool TryDelete(Guid habit_id);
    bool TryDeleteHabits(string user_name);
    bool TryReplaceHabits(List<Habit> habits, string user_name);
}