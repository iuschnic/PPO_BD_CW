using Domain;
using Domain.InPorts;
using Domain.OutPorts;
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
    .BuildServiceProvider();

var TaskService = serviceProvider.GetRequiredService<ITaskTracker>();


Console.WriteLine("Тест создания пользователя");
User? valid_user = TaskService.CreateUser("kulikov_egor", new PhoneNumber("+79161648345"), "12345");
if (valid_user != null)
    valid_user.Print();
else
{
    Console.WriteLine("Что то пошло не так 1");
    return;
}


Console.WriteLine("Тест входа в аккаунт с несуществующим именем пользователя");
User? invalid_user = TaskService.LogIn("kuli_egor", "12345");
if (invalid_user != null)
{
    Console.WriteLine("Что то пошло не так 2");
    return;
}
else
    Console.WriteLine("OK");

Console.WriteLine("Тест входа в аккаунт с неправильным паролем");
invalid_user = TaskService.LogIn("kulikov_egor", "123456");
if (invalid_user != null)
{
    Console.WriteLine("Что то пошло не так 2");
    return;
}
else
    Console.WriteLine("OK");

Console.WriteLine("Тест загрузки расписания");
var ans = TaskService.ImportNewShedule(valid_user.Id);
if (ans != null)
{
    valid_user = ans.Item1 as User;
    valid_user.Print();
}
else
{
    Console.WriteLine("Что то пошло не так 2");
    return;
}

Console.WriteLine("Тест добавления привычки которую можно добавить");
//Для создания привычки от пользователя требуется:
//название, сколько минутв вып, сколько дней вып, опция времени, список таймингов
ans = TaskService.AddHabit(valid_user.Id, "Тестовая привычка", 90, 1, new TimeOption("NoMatter"), []);
if (ans != null)
{
    valid_user = ans.Item1 as User;
    valid_user.Print();
}
else
{
    Console.WriteLine("Что то пошло не так 3");
    return;
}

Console.WriteLine("Тест добавления привычки которую нельзя добавить");
//Для создания привычки от пользователя требуется:
//название, сколько минутв вып, сколько дней вып, опция времени, список таймингов
ans = TaskService.AddHabit(valid_user.Id, "Тестовая привычка2", 900, 1, new TimeOption("NoMatter"), []);
if (ans != null)
{
    valid_user = ans.Item1;
    valid_user.Print();
    var undistributed = ans.Item2;
    foreach (var item in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", item.Key, item.Value);
}
else
{
    Console.WriteLine("Что то пошло не так 4");
    return;
}

Console.WriteLine("Тест добавления привычки которую можно добавить лишь частично");
//Для создания привычки от пользователя требуется:
//название, сколько минутв вып, сколько дней вып, опция времени, список таймингов
ans = TaskService.AddHabit(valid_user.Id, "Тестовая привычка3", 120, 4, new TimeOption("NoMatter"), []);
if (ans != null)
{
    valid_user = ans.Item1;
    valid_user.Print();
    var undistributed = ans.Item2;
    foreach (var item in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", item.Key, item.Value);
}
else
{
    Console.WriteLine("Что то пошло не так 5");
    return;
}

Console.WriteLine("Тест изменения настроек уведомлений");
//Для создания привычки от пользователя требуется:
//название, сколько минутв вып, сколько дней вып, опция времени, список таймингов
valid_user = TaskService.ChangeNotify(valid_user.Id);
if (valid_user != null)
    valid_user.Print();
else
{
    Console.WriteLine("Что то пошло не так 6");
    return;
}
