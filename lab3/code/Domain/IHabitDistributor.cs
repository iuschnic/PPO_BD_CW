using Domain.Models;

namespace Domain;

public interface IHabitDistributor
{
    /*Функция распределения привычек по расписанию
     Получает на вход списки привычек и событий расписания, изменяет привычки в полученном списке
     добавляя им реальное время в которое они могут быть выполнены при текущем расписании
     Возвращает список новых экземпляров привычек, которые не были распределены полностью или частично*/
    List<Habit> DistributeHabits(List<Habit> habitsForDistribution, List<Event> events);
}
