using Types;
using Domain.Models;
namespace Domain.InPorts;

public interface ITaskTracker
{
    Task<User> CreateUserAsync(string username, PhoneNumber phoneNumber, string password);
    Task<User> LogInAsync(string userName, string password, string? twoFactorCode = null);
    Task<Tuple<User, List<Habit>>> ImportNewSheduleAsync(string userName, string path);
    Task<Tuple<User, List<Habit>>> ImportNewSheduleAsync(string userName, Stream stream, string extension);
    Task<Tuple<User, List<Habit>>> AddHabitAsync(Habit habit);
    Task<Tuple<User, List<Habit>>> DeleteHabitAsync(string userName, string name);
    Task<Tuple<User, List<Habit>>> DeleteHabitsAsync(string name);
    Task<User> ChangeSettingsAsync(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string userName);
    Task DeleteUserAsync(string userName);
    Task ChangeTwoFactorAuthAsync(string userName, bool isEnabled);
    Task ChangePasswordAsync(string userName, string password, string newPassword, string? twoFactorCode = null);

    //По имени пользователя, телефону и паролю создает нового пользователя
    User CreateUser(string userName, PhoneNumber phoneNumber, string password);
    //По имени пользователя и паролю возвращает всю информацию о пользователе включая привычки, расписание
    User LogIn(string userName, string password);
    /*По идентификатору пользователя импортирует новое расписание и перераспределяет привычки,
    возвращает всю информацию о пользователе 
    и словарь с нераспределенными привычками (которые распределились не на все указанное количество дней)
    */
    Tuple<User, List<Habit>> ImportNewShedule(string userName, string path);
    Tuple<User, List<Habit>> ImportNewShedule(string userName, Stream stream, string extension);
    Tuple<User, List<Habit>> AddHabit(Habit habit);
    Tuple<User, List<Habit>> DeleteHabit(string userName, string name);
    Tuple<User, List<Habit>> DeleteHabits(string name);
    User ChangeSettings(List<Tuple<TimeOnly, TimeOnly>>? newTimings, bool? notifyOn, string userName);
    void DeleteUser(string userName);
}