using Types;
using Domain.Models;
namespace Domain.InPorts;

public interface ITaskTracker
{
    Task<User> CreateUserAsync(string username, PhoneNumber phone_number, string password);
    Task<User> LogInAsync(string username, string password);
    Task<Tuple<User, List<Habit>>> ImportNewSheduleAsync(string user_name, string path);
    Task<Tuple<User, List<Habit>>> ImportNewSheduleAsync(string user_name, Stream stream, string extension);
    Task<Tuple<User, List<Habit>>> AddHabitAsync(Habit habit);
    Task<Tuple<User, List<Habit>>> DeleteHabitAsync(string user_name, string name);
    Task<Tuple<User, List<Habit>>> DeleteHabitsAsync(string name);
    Task<User> ChangeSettingsAsync(UserSettings settings);
    Task<User> ChangeSettingsAsync(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string user_name);
    Task<User> NotificationsOnAsync(string user_name);
    Task<User> NotificationsOffAsync(string user_name);
    Task<User> UpdateNotificationTimingsAsync(List<Tuple<TimeOnly, TimeOnly>> newTimings, string user_name);
    Task DeleteUserAsync(string username);

    //По имени пользователя, телефону и паролю создает нового пользователя
    User CreateUser(string username, PhoneNumber phone_number, string password);
    //По имени пользователя и паролю возвращает всю информацию о пользователе включая привычки, расписание
    User LogIn(string username, string password);
    /*По идентификатору пользователя импортирует новое расписание и перераспределяет привычки,
    возвращает всю информацию о пользователе 
    и словарь с нераспределенными привычками (которые распределились не на все указанное количество дней)
    */
    Tuple<User, List<Habit>> ImportNewShedule(string user_name, string path);
    Tuple<User, List<Habit>> ImportNewShedule(string user_name, Stream stream, string extension);
    Tuple<User, List<Habit>> AddHabit(Habit habit);
    Tuple<User, List<Habit>> DeleteHabit(string user_name, string name);
    Tuple<User, List<Habit>> DeleteHabits(string name);
    User ChangeSettings(UserSettings settings);
    User ChangeSettings(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string user_name);
    User NotificationsOn(string user_name);
    User NotificationsOff(string user_name);
    User UpdateNotificationTimings(List<Tuple<TimeOnly, TimeOnly>> newTimings, string user_name);
    void DeleteUser(string username);
}