using Domain.Models;
namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface IHabitRepo
{
    //Здесь будет IEnumerable
    List<Habit> Get(string user_name);
    void Create(Habit habit);
    void CreateMany(List<Habit> habits);
    void Update(Habit habit);
    void Delete(Guid habit_id);
    void DeleteHabits(string user_name);
    //void Save();
}
