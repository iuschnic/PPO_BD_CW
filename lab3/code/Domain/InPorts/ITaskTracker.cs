using Domain;
using Types;

namespace Domain.InPorts;

public interface ITaskTracker
{
    //По имени пользователя, телефону и паролю создает нового пользователя или возвращает null если пользователь уже есть
    User? CreateUser(string username, PhoneNumber phone_number, string password);
    //По имени пользователя и паролю возвращает всю информацию о пользователе включая привычки, расписание или null если пользователя
    //нет или пароль неверен
    User? LogIn(string username, string password);
    //По идентификатору пользователя импортирует новое расписание и перераспределяет привычки,
    //возвращает всю информацию о пользователе или null в случае не существующего идентификатора
    User? ImportNewShedule(Guid user_id);
    //Добавляет привычку для указанного пользователя, возвращает null при некорректном идентификаторе пользователя
    User? AddHabit(User u, Habit h);
}
