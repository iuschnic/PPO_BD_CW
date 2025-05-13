using Domain;
using Domain.InPorts;
using Domain.OutPorts;
using Domain.Models;
using LoadAdapters;
using Microsoft.Extensions.DependencyInjection;
using Storage.PostgresStorageAdapters;
using Types;
using Microsoft.EntityFrameworkCore;


class Program
{
    private static User? AddHabit(ITaskTracker task_service, User user)
    {
        
    }
    private static void LoggedCycle(ITaskTracker task_service, User user)
    {
        while (true)
        {
            Console.WriteLine("1) Импортировать новое расписание\n2) Добавить привычку\n3) Удалить привычку\n" +
                "4) Удалить все привычки\n5) Разрешить/Запретить уведомления\n" +
                "6) Добавить запрещенное время посылки уведомлений\n7) Выйти из учетной записи\n");
            List<Habit>? undistributed;
            if (!Int32.TryParse(Console.ReadLine(), out int opt))
            {
                Console.WriteLine("Введите целое число от 1 до 7 включительно");
                continue;
            }
            switch (opt)
            {
                case 1:
                    var ret = task_service.ImportNewShedule(user.NameID, "dummy");
                    if (ret == null)
                        throw new Exception("Критическая ошибка, пользователь не существует");
                    user = ret.Item1;
                    undistributed = ret.Item2;
                    Console.WriteLine("Расписание было успешно импортировано, нераспределенные привычки:");
                    if (undistributed.Count == 0)
                        Console.WriteLine("Все привычки были распределены успешно");
                    else
                        Console.Write(undistributed);
                    Console.WriteLine();
                    Console.Write(user);
                    break;
                case 2:
                    ret = task_service.AddHabit(user.NameID, "dummy");
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
                default:
                    Console.WriteLine("\nВведите целое число от 1 до 3 включительно");
                    break;
            }
        }
    }
    static void Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IEventRepo, PostgresEventRepo>()
            .AddSingleton<IHabitRepo, PostgresHabitRepo>()
            .AddSingleton<IUserRepo, PostgresUserRepo>()
            .AddDbContext<PostgresDBContext>(options => options.UseNpgsql("Host=localhost;Port=5432;Database=habits_db;Username=postgres;Password=postgres"))
            .AddTransient<ISheduleLoad, DummyShedAdapter>()
            .AddTransient<ITaskTracker, TaskTracker>()
            .AddTransient<IHabitDistributor, HabitDistributor>()
            .BuildServiceProvider();

        var taskService = serviceProvider.GetRequiredService<ITaskTracker>();
        LogInCycle(taskService);
    }
}