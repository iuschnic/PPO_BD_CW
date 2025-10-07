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
    List<UserHabitInfo>? GetUsersToNotify();
    Task<List<Habit>?> TryGetAsync(string user_name);
    Task<bool> TryCreateAsync(Habit habit);
    Task<bool> TryCreateManyAsync(List<Habit> habits);
    Task<bool> TryUpdateAsync(Habit habit);
    Task<bool> TryDeleteAsync(Guid habit_id);
    Task<bool> TryDeleteHabitsAsync(string user_name);
    Task<bool> TryReplaceHabitsAsync(List<Habit> habits, string user_name);
    Task<List<UserHabitInfo>?> GetUsersToNotifyAsync();
}
