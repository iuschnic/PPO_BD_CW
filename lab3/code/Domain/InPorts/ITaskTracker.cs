using Types;
using Domain.Models;
namespace Domain.InPorts;

public interface ITaskTracker
{
    //По имени пользователя, телефону и паролю создает нового пользователя или возвращает null если пользователь уже есть
    User CreateUser(string username, PhoneNumber phone_number, string password);
    //По имени пользователя и паролю возвращает всю информацию о пользователе включая привычки, расписание
    User LogIn(string username, string password);
    /*По идентификатору пользователя импортирует новое расписание и перераспределяет привычки,
    возвращает всю информацию о пользователе 
    и словарь с нераспределенными привычками (которые распределились не на все указанное количество дней)
    */
    Tuple<User, List<Habit>> ImportNewShedule(string user_name, string path);
    //Добавляет привычку для указанного пользователя
    Tuple<User, List<Habit>> AddHabit(Habit habit);
    Tuple<User, List<Habit>> DeleteHabit(string user_name, string name);
    Tuple<User, List<Habit>> DeleteHabits(string name);
    public User ChangeSettings(UserSettings settings);
    void DeleteUser(string username);
}