using Domain;
using Domain.InPorts;
using Domain.OutPorts;
using Domain.Models;
using LoadAdapters;
using Microsoft.Extensions.DependencyInjection;
using Storage.PostgresStorageAdapters;
using Types;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


/*class Program
{
    private static User? AddHabit(ITaskTracker task_service, User user)
    {
        string? user_name, password;
        Console.WriteLine("Введите имя пользователя:");
        user_name = Console.ReadLine();
        if (user_name == null)
        {
            Console.WriteLine("Пустая строка имени пользователя");
            return null;
        }
    }
    private static void LoggedCycle(ITaskTracker task_service, User user)
    {
        while (true)
        {
            Console.WriteLine("1) Импортировать новое расписание\n2) Добавить привычку\n3) Удалить привычку\n" +
                "4) Удалить все привычки\n5) Разрешить/Запретить уведомления\n" +
                "6) Добавить запрещенное время посылки уведомлений\n");
            User? tmp_user = user;
            List<Habit>? undistributed;
            if (!Int32.TryParse(Console.ReadLine(), out int opt))
            {
                Console.WriteLine("Введите целое число от 1 до 6 включительно");
                continue;
            }
            switch (opt)
            {
                case 1:
                    var ret = task_service.ImportNewShedule(tmp_user.NameID, "dummy");
                    if (ret == null)
                        throw new Exception("Критическая ошибка, пользователь не существует");
                    tmp_user = ret.Item1;
                    undistributed = ret.Item2;
                    Console.WriteLine("Расписание было успешно импортировано, нераспределенные привычки:");
                    if (undistributed.Count == 0)
                        Console.WriteLine("Все привычки были распределены успешно");
                    else
                        Console.Write(undistributed);
                    Console.WriteLine();
                    Console.Write(tmp_user);
                    break;
                case 2:
                    ret = task_service.AddHabit(tmp_user.NameID, "dummy");
                    if (ret == null)
                        throw new Exception("Критическая ошибка, пользователь не существует");
                    tmp_user = ret.Item1;
                    undistributed = ret.Item2;
                    Console.WriteLine("Расписание было успешно импортировано, нераспределенные привычки:");
                    if (undistributed.Count == 0)
                        Console.WriteLine("Все привычки были распределены успешно");
                    else
                        Console.Write(undistributed);
                    Console.WriteLine();
                    Console.Write(tmp_user);
                    break;
                case 3:
                    return;
            }
        }
    }
    private static User? LogIn(ITaskTracker task_service)
    {
        string? user_name, password;
        Console.WriteLine("Введите имя пользователя:");
        user_name = Console.ReadLine();
        if (user_name == null)
        {
            Console.WriteLine("Пустая строка имени пользователя");
            return null;
        }
        Console.WriteLine("Введите пароль:");
        password = Console.ReadLine();
        if (password == null)
        {
            Console.WriteLine("Пустая строка пароля");
            return null;
        }
        User? user = task_service.LogIn(user_name, password);
        if (user == null)
        {
            Console.WriteLine("Неверные логин или пароль");
            return null;
        }
        else
            return user;
    }
    private static User? CreateUser(ITaskTracker task_service)
    {
        string? user_name, phone_string, password;
        PhoneNumber phone_number;
        Console.WriteLine("Введите имя пользователя:");
        user_name = Console.ReadLine();
        if (user_name == null)
        {
            Console.WriteLine("Пустая строка имени пользователя");
            return null;
        }
        Console.WriteLine("Введите номер телефона:");
        phone_string = Console.ReadLine();
        if (phone_string == null)
        {
            Console.WriteLine("Пустая строка телефона");
            return null;
        }
        try
        {
            phone_number = new PhoneNumber(phone_string);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
        Console.WriteLine("Введите пароль:");
        password = Console.ReadLine();
        if (password == null)
        {
            Console.WriteLine("Пустая строка пароля");
            return null;
        }
        User? user = task_service.CreateUser(user_name, phone_number, password);
        if (user == null)
        {
            Console.WriteLine("Пользователь с заданным логином уже существует");
            return null;
        }
        else
            return user;
    }
    private static void LogInCycle(ITaskTracker task_service)
    {
        int opt;
        User? user;
        while (true)
        {
            Console.WriteLine("1) Создать аккаунт\n2) Войти в аккаунт\n3) Выйти из программы\n");
            if (!Int32.TryParse(Console.ReadLine(), out opt))
            {
                Console.WriteLine("Введите целое число от 1 до 3 включительно");
                continue;
            }
            switch (opt)
            {
                case 1:
                    user = CreateUser(task_service);
                    if (user == null)
                        break;
                    Console.Write(user);
                    break;
                case 2:
                    user = LogIn(task_service);
                    if (user == null)
                        break;
                    Console.Write(user);
                    break;
                case 3:
                    return;
            }
        }
    }
    static void Main()
    {
        //var serviceProvider = new ServiceCollection()
        //    .AddSingleton<IEventRepo, DummyEventRepo>()
        //    .AddSingleton<IHabitRepo, DummyHabitRepo>()
        //    .AddSingleton<IMessageRepo, DummyMessageRepo>()
        //    .AddSingleton<ISettingsRepo, DummySettingsRepo>()
        //    .AddSingleton<IUserRepo, DummyUserRepo>()
        //    .AddTransient<IShedLoad, DummyShedAdapter>()
        //    .AddTransient<ITaskTracker, TaskTracker>()
        //    .AddTransient<IHabitDistributor, HabitDistributor>()
        //    .BuildServiceProvider();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IEventRepo, PostgresEventRepo>()
            .AddSingleton<IHabitRepo, PostgresHabitRepo>()
            .AddSingleton<ISettingsRepo, PostgresSettingsRepo>()
            .AddSingleton<IUserRepo, PostgresUserRepo>()
            .AddDbContext<PostgresDBContext>(options => options.UseNpgsql("Host=localhost;Port=5432;Database=habits_db;Username=postgres;Password=postgres"))
            .AddTransient<IShedLoad, DummyShedAdapter>()
            .AddTransient<ITaskTracker, TaskTracker>()
            .AddTransient<IHabitDistributor, HabitDistributor>()
            .BuildServiceProvider();

        var taskService = serviceProvider.GetRequiredService<ITaskTracker>();
        LogInCycle(taskService);
    }
}*/
var serviceProvider = new ServiceCollection()
            .AddSingleton<IEventRepo, PostgresEventRepo>()
            .AddSingleton<IHabitRepo, PostgresHabitRepo>()
            //.AddSingleton<ISettingsRepo, PostgresSettingsRepo>()
            .AddSingleton<IUserRepo, PostgresUserRepo>()
            .AddDbContext<PostgresDBContext>(options => options.UseNpgsql("Host=localhost;Port=5432;Database=habits_db;Username=postgres;Password=postgres"))
            .AddTransient<ISheduleLoad, DummyShedAdapter>()
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
    Console.WriteLine("Пользователь уже существует");

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест создания такого же пользователя");
User? invalid_user = TaskService.CreateUser("kulikov_egor", new PhoneNumber("+79161648345"), "12345");
if (invalid_user == null)
    Console.WriteLine("OK");
else
{
    Console.WriteLine("Что то пошло не так 1");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест входа в аккаунт с несуществующим именем пользователя");
invalid_user = TaskService.LogIn("kuli_egor", "12345");
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
Console.WriteLine("Тест входа в аккаунт с существующим именем пользователя");
valid_user = TaskService.LogIn("kulikov_egor", "12345");
if (valid_user == null)
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
var ans = TaskService.AddHabit(valid_user.NameID, "Тестовая привычка0", 90, 1, TimeOption.NoMatter, []);
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
    foreach (var habit in ans.Item2)
        Console.WriteLine("Habit {0} wasn't distributed for {1} times", habit.Name, habit.CountInWeek);
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
ans = TaskService.AddHabit(valid_user.NameID, "Тестовая привычка11", 60, 4, TimeOption.Fixed, times);
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
    var undistributed = ans.Item2;
    foreach (var habit in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", habit.Name, habit.CountInWeek);
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
ans = TaskService.AddHabit(valid_user.NameID, "Тестовая привычка5", 20, 7, TimeOption.Preffered, times);
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
    var undistributed = ans.Item2;
    foreach (var habit in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", habit.Name, habit.CountInWeek);
}
else
{
    Console.WriteLine("Что то пошло не так 5");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест загрузки расписания");
ans = TaskService.ImportNewShedule(valid_user.NameID, "dummmy");
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
}
else
{
    Console.WriteLine("Что то пошло не так 2");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("Тест загрузки расписания");
ans = TaskService.ImportNewShedule(valid_user.NameID, "dummmy");
if (ans != null)
{
    valid_user = ans.Item1;
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
ans = TaskService.AddHabit(valid_user.NameID, "Тестовая привычка", 90, 1, TimeOption.NoMatter, []);
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
ans = TaskService.AddHabit(valid_user.NameID, "Тестовая привычка2", 900, 1, TimeOption.NoMatter, []);
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
    var undistributed = ans.Item2;
    foreach (var habit in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", habit.Name, habit.CountInWeek);
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
ans = TaskService.AddHabit(valid_user.NameID, "Тестовая привычка1", 10, 4, TimeOption.NoMatter, []);
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
    var undistributed = ans.Item2;
    foreach (var habit in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", habit.Name, habit.CountInWeek);
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
valid_user.Settings.NotifyOn = !valid_user.Settings.NotifyOn;
valid_user = TaskService.ChangeSettings(valid_user.Settings);
if (valid_user != null)
    Console.Write(valid_user);
else
{
    Console.WriteLine("Что то пошло не так 6");
    return;
}

Console.WriteLine("_______________________________________________________________________________");
Console.WriteLine("\nТест удаления привычки с именем Тестовая привычка3");
ans = TaskService.DeleteHabit(valid_user.NameID, "Тестовая привычка3");
if (ans != null)
{
    valid_user = ans.Item1;
    Console.Write(valid_user);
    var undistributed = ans.Item2;
    foreach (var habit in undistributed)
        Console.WriteLine("Habit {0} wasn't diistributed for {1} times", habit.Name, habit.CountInWeek);
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