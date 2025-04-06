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
    /*По идентификатору пользователя импортирует новое расписание и перераспределяет привычки,
    возвращает всю информацию о пользователе 
    и словарь с нераспределенными привычками (которые распределились не на все указанное количество дней)
    Возвращает null в случае не существующего идентификатора пользователя
    */
    Tuple<User, Dictionary<string, int>>? ImportNewShedule(Guid user_id);
    //Добавляет привычку для указанного пользователя, возвращает null при некорректном идентификаторе пользователя
    Tuple<User, Dictionary<string, int>>? AddHabit(Guid user_id, string name, int mins_complete, int ndays, TimeOption op,
        List<Tuple<TimeOnly, TimeOnly>> preffixedtimes);
    //Tuple<User, Dictionary<string, int>>? DeleteHabit(User u, Habit h);
    User? ChangeNotify(Guid user_id, bool flag);
}
