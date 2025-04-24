using Domain.Models;
namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface IHabitRepo
{
    //Здесь будет IEnumerable
    List<Habit>? TryGet(string user_name);
    bool TryCreate(Habit habit);
    bool TryCreateMany(List<Habit> habits);
    bool TryUpdate(Habit habit);
    bool TryDelete(Guid habit_id);
    bool TryDeleteHabits(string user_name);
}
