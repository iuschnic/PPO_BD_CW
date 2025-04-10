using Domain;
using Domain.InPorts;
using Domain.OutPorts;
using Domain.Models;
using LoadAdapters;
using Microsoft.Extensions.DependencyInjection;
using Storage;
using Storage.StorageAdapters;
using Types;

var serviceProvider = new ServiceCollection()
    .AddSingleton<IEventRepo, EventRepo>()
    .AddSingleton<IHabitRepo, HabitRepo>()
    .AddSingleton<IMessageRepo, MessageRepo>()
    .AddSingleton<ISettingsRepo, SettingsRepo>()
    .AddSingleton<IUserRepo, UserRepo>()
    .AddTransient<IShedLoad, DummyShedAdapter>()
    .AddTransient<ITaskTracker, TaskTracker>()
    .AddTransient<IHabitDistributor, HabitDistributor>()
    .BuildServiceProvider();

var TaskService = serviceProvider.GetRequiredService<ITaskTracker>();


Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест создания пользователя");
User? valid_user = TaskService.CreateUser("kulikov_egor", new PhoneNumber("+79161648345"), "12345");
if (valid_user != null)
    Console.Write(valid_user);
else
{
    Console.WriteLine("Что то пошло не так 1");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест входа в аккаунт с несуществующим именем пользователя");
User? invalid_user = TaskService.LogIn("kuli_egor", "12345");
if (invalid_user != null)
{
    Console.WriteLine("Что то пошло не так 2");
    return;
}
else
    Console.WriteLine("OK");

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест входа в аккаунт с неправильным паролем");
invalid_user = TaskService.LogIn("kulikov_egor", "123456");
if (invalid_user != null)
{
    Console.WriteLine("Что то пошло не так 2");
    return;
}
else
    Console.WriteLine("OK");

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест добавления привычки без расписания");
//Для создания привычки от пользователя требуется:
//название, сколько минутв вып, сколько дней вып, опция времени, список таймингов
var ans = TaskService.AddHabit(valid_user.Id, "Тестовая привычка0", 90, 1, TimeOption.NoMatter, []);
if (ans != null)
{
    valid_user = ans.Item1 as User;
    Console.Write(valid_user);
    foreach (var habit in ans.Item2)
        Console.WriteLine("Habit {0} wasn't distributed for {1} times", habit.Name, habit.NDays);
}
else
{
    Console.WriteLine("Что то пошло не так 3");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест добавления привычки с фиксированным временем");
List<Tuple<TimeOnly, TimeOnly>> times = [];
times.Add(new Tuple<TimeOnly, TimeOnly>(new TimeOnly(18, 0, 0), new TimeOnly(20, 0, 0)));
ans = TaskService.AddHabit(valid_user.Id, "Тестовая привычка11", 60, 4, TimeOption.Fixed, times);
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
    var undistributed = ans.Item2;
    foreach (var habit in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", habit.Name, habit.NDays);
}
else
{
    Console.WriteLine("Что то пошло не так 5");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест добавления привычки с предпочтительным временем");
times = [];
times.Add(new Tuple<TimeOnly, TimeOnly>(new TimeOnly(0, 0, 0), new TimeOnly(20, 0, 0)));
ans = TaskService.AddHabit(valid_user.Id, "Тестовая привычка5", 20, 7, TimeOption.Preffered, times);
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
    var undistributed = ans.Item2;
    foreach (var habit in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", habit.Name, habit.NDays);
}
else
{
    Console.WriteLine("Что то пошло не так 5");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест загрузки расписания");
ans = TaskService.ImportNewShedule(valid_user.Id, "dummmy");
if (ans != null)
{
    valid_user = ans.Item1 as User;
    Console.Write(valid_user);
}
else
{
    Console.WriteLine("Что то пошло не так 2");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест добавления привычки которую можно добавить");
//Для создания привычки от пользователя требуется:
//название, сколько минутв вып, сколько дней вып, опция времени, список таймингов
ans = TaskService.AddHabit(valid_user.Id, "Тестовая привычка", 90, 1, TimeOption.NoMatter, []);
if (ans != null)
{
    valid_user = ans.Item1 as User;
    Console.Write(valid_user);
}
else
{
    Console.WriteLine("Что то пошло не так 3");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест добавления привычки которую нельзя добавить");
//Для создания привычки от пользователя требуется:
//название, сколько минутв вып, сколько дней вып, опция времени, список таймингов
ans = TaskService.AddHabit(valid_user.Id, "Тестовая привычка2", 900, 1, TimeOption.NoMatter, []);
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
    var undistributed = ans.Item2;
    foreach (var habit in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", habit.Name, habit.NDays);
}
else
{
    Console.WriteLine("Что то пошло не так 4");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест добавления привычки которую можно добавить лишь частично");
//Для создания привычки от пользователя требуется:
//название, сколько минутв вып, сколько дней вып, опция времени, список таймингов
ans = TaskService.AddHabit(valid_user.Id, "Тестовая привычка1", 10, 4, TimeOption.NoMatter, []);
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
    var undistributed = ans.Item2;
    foreach (var habit in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", habit.Name, habit.NDays);
}
else
{
    Console.WriteLine("Что то пошло не так 5");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("\nТест изменения настроек уведомлений");
//Для создания привычки от пользователя требуется:
//название, сколько минутв вып, сколько дней вып, опция времени, список таймингов
valid_user = TaskService.ChangeNotify(valid_user.Id, false);
if (valid_user != null)
    Console.Write(valid_user);
else
{
    Console.WriteLine("Что то пошло не так 6");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("\nТест удаления привычки с именем Тестовая привычка3");
ans = TaskService.DeleteHabit(valid_user.Id, "Тестовая привычка3");
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
    var undistributed = ans.Item2;
    foreach (var habit in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", habit.Name, habit.NDays);
}
else
{
    Console.WriteLine("Что то пошло не так 7");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест создания еще одного пользователя");
valid_user = TaskService.CreateUser("egor", new PhoneNumber("+71161648345"), "54321");
if (valid_user != null)
    Console.Write(valid_user);
else
{
    Console.WriteLine("Что то пошло не так 8");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест входа в аккаунт первого пользователя");
valid_user = TaskService.LogIn("kulikov_egor", "12345");
if (valid_user != null)
    Console.Write(valid_user);
else
{
    Console.WriteLine("Что то пошло не так 9");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест входа в аккаунт второго пользователя");
valid_user = TaskService.LogIn("egor", "54321");
if (valid_user != null)
    Console.Write(valid_user);
else
{
    Console.WriteLine("Что то пошло не так 9");
    return;
}