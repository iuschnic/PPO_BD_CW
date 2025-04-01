namespace Domain.OutPorts;

//Здесь будет наследование от IDisposable
public interface IHabitRepo
{
    //Здесь будет IEnumerable
    List<Habit> Get(Guid user_id);
    void Create(Habit habit);
    void Update(Habit habit);
    void Delete(Guid habit_id);
    void DeleteHabits(Guid user_id);
    void Save();
}
