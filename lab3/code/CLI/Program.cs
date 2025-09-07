using Domain;
using Domain.InPorts;
using Domain.OutPorts;
using Domain.Models;
using LoadAdapters;
using Microsoft.Extensions.DependencyInjection;
using Storage.PostgresStorageAdapters;
using Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
class Program
{
    private static Habit? ParseHabit(ITaskTracker task_service, string user_name)
    {
        Guid g = Guid.NewGuid();
        Console.WriteLine("Введите название привычки:");
        string? name = Console.ReadLine();
        if (name == null)
        {
            Console.WriteLine("Ошибка ввода");
            return null;
        }
        Console.WriteLine("Введите положительное число - сколько минут нужно тратить на привычку:");
        if (!Int32.TryParse(Console.ReadLine(), out int mins) || mins <= 0)
        {
            Console.WriteLine("Ошибка ввода");
            return null;
        }
        Console.WriteLine("Введите положительное число от 0 до 2 включительно - тип привычки:\n" +
            "0) безразличное время выполнения\n1) предпочтительное время выполнения\n" +
            "2) фиксированное время выполнения");
        if (!Int32.TryParse(Console.ReadLine(), out int opt) || opt < 0 || opt > 2)
        {
            Console.WriteLine("Ошибка ввода");
            return null;
        }
        List<PrefFixedTime> timings = [];
        if (opt == ((int)TimeOption.Preffered) || opt == ((int)TimeOption.Fixed))
        {
            Console.WriteLine("Введите временные интервалы для привычки по одному в строке (hh:mm hh:mm):");
            while (true)
            {
                var input = Console.ReadLine();
                if (input == null)
                {
                    Console.WriteLine("Ошибка ввода");
                    return null;
                }
                string[] line = input.Split(" ");
                if (line.Length != 2)
                {
                    break;
                }
                TimeOnly start, end;
                try
                {
                    start = TimeOnly.Parse(line[0]);
                    end = TimeOnly.Parse(line[1]);
                }
                catch (Exception)
                {
                    break;
                }
                timings.Add(new PrefFixedTime(Guid.NewGuid(), start, end, g));
            }
            if (timings.Count == 0)
            {
                Console.WriteLine("Привычка с фиксированным или предпочтительным временем должна иметь хотя бы один временной интервал");
                return null;
            }
        }

        Console.WriteLine("Введите положительное число от 1 до 7 - сколько дней в неделю нужно выполнять привычку:");
        if (!Int32.TryParse(Console.ReadLine(), out int ndays) || ndays < 1 || ndays > 7)
        {
            Console.WriteLine("Ошибка ввода");
            return null;
        }
        Habit habit = new Habit(g, name, mins, (TimeOption)opt, user_name, [], timings, ndays);
        return habit;
    }
    private static void LoggedCycle(ITaskTracker task_service, User user)
    {
        while (true)
        {
            Console.WriteLine("\n1) Импортировать новое расписание\n2) Добавить привычку\n3) Удалить привычку\n" +
                "4) Удалить все привычки\n5) Разрешить уведомления\n6) Запретить уведомления\n" +
                "7) Изменить запрещенное время посылки уведомлений\n8) Выйти из учетной записи\n9) Удалить учетную запись\n");
            List<Habit>? undistributed;
            if (!Int32.TryParse(Console.ReadLine(), out int opt) || opt < 1 || opt > 9)
            {
                Console.WriteLine("\nВведите целое число от 1 до 8 включительно\n");
                continue;
            }
            Tuple<User, List<Habit>>? ret;
            switch (opt)
            {
                case 1:
                    Console.WriteLine("\nВведите название файла\n");
                    var path = Console.ReadLine();
                    if (path == null)
                    {
                        Console.WriteLine("\nОшибка ввода\n");
                        break;
                    }
                    try
                    {
                        ret = task_service.ImportNewShedule(user.NameID, path);
                        if (ret == null)
                        {
                            Console.WriteLine("\nКритическая ошибка в базе данных\n");
                            return;
                        }
                        user = ret.Item1;
                        undistributed = ret.Item2;
                        Console.WriteLine("\nРасписание было успешно импортировано, нераспределенные привычки:\n");
                        if (undistributed.Count == 0)
                            Console.WriteLine("\nВсе привычки были распределены успешно\n");
                        else
                            foreach (var u in undistributed)
                                Console.Write(u);
                        Console.WriteLine();
                        Console.Write(user);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                case 2:
                    var habit = ParseHabit(task_service, user.NameID);
                    if (habit == null)
                        break;
                    try
                    {
                        ret = task_service.AddHabit(habit);
                        if (ret == null)
                        {
                            Console.WriteLine("\nКритическая ошибка в базе данных\n");
                            return;
                        }
                        user = ret.Item1;
                        undistributed = ret.Item2;
                        Console.WriteLine("\nПривычка была успешно добавлена, нераспределенные привычки:\n");
                        if (undistributed.Count == 0)
                            Console.WriteLine("\nВсе привычки были распределены успешно\n");
                        else
                            foreach (var u in undistributed)
                                Console.Write(u);
                        Console.WriteLine();
                        Console.Write(user);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                case 3:
                    Console.WriteLine("\nВведите название удаляемой привычки:\n");
                    string? name = Console.ReadLine();
                    if (name == null)
                    {
                        Console.WriteLine("\nОшибка ввода\n");
                        break;
                    }
                    try
                    {
                        ret = task_service.DeleteHabit(user.NameID, name);
                        if (ret == null)
                        {
                            Console.WriteLine("\nКритическая ошибка в базе данных\n");
                            return;
                        }
                        user = ret.Item1;
                        undistributed = ret.Item2;
                        Console.WriteLine("\nПривычка была удалена\n");
                        if (undistributed.Count == 0)
                            Console.WriteLine("\nВсе привычки были распределены успешно\n");
                        else
                            Console.Write(undistributed);
                        Console.WriteLine();
                        Console.Write(user);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                case 4:

                    try
                    {
                        ret = task_service.DeleteHabits(user.NameID);
                        if (ret == null)
                        {
                            Console.WriteLine("\nКритическая ошибка в базе данных\n");
                            return;
                        }
                        user = ret.Item1;
                        Console.WriteLine("\nПривычки были успешно удалены\n");
                        Console.WriteLine();
                        Console.Write(user);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                case 5:

                    try
                    {
                        var settings = new UserSettings(user.Settings.Id, true, user.Settings.UserNameID, user.Settings.SettingsTimes);
                        var tmpuser = task_service.ChangeSettings(settings);
                        if (tmpuser == null)
                        {
                            Console.WriteLine("\nКритическая ошибка в базе данных\n");
                            return;
                        }
                        user = tmpuser;
                        Console.WriteLine("\nУведомления разрешены\n");
                        Console.WriteLine();
                        Console.Write(user);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                case 6:

                    try
                    {
                        var settings = new UserSettings(user.Settings.Id, false, user.Settings.UserNameID, user.Settings.SettingsTimes);
                        var tmpuser = task_service.ChangeSettings(settings);
                        if (tmpuser == null)
                        {
                            Console.WriteLine("\nКритическая ошибка в базе данных\n");
                            return;
                        }
                        user = tmpuser;
                        Console.WriteLine("\nУведомления запрещены\n");
                        Console.WriteLine();
                        Console.Write(user);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                case 7:

                    try
                    {
                        List<SettingsTime> timings = [];
                        Console.WriteLine("\nВведите новые временные интервалы запрета уведомлений по одному в строке (hh:mm hh:mm):\n");
                        while (true)
                        {
                            var input = Console.ReadLine();
                            if (input == null)
                            {
                                Console.WriteLine("\nОшибка ввода\n");
                                break;
                            }
                            string[] line = input.Split(" ");
                            if (line.Length != 2)
                            {
                                break;
                            }
                            TimeOnly start, end;
                            try
                            {
                                start = TimeOnly.Parse(line[0]);
                                end = TimeOnly.Parse(line[1]);
                            }
                            catch (Exception)
                            {
                                break;
                            }
                            timings.Add(new SettingsTime(Guid.NewGuid(), start, end, user.Settings.Id));
                        }
                        var settings = new UserSettings(user.Settings.Id, user.Settings.NotifyOn, user.Settings.UserNameID, timings);
                        var tmpuser = task_service.ChangeSettings(settings);
                        if (tmpuser == null)
                        {
                            Console.WriteLine("\nКритическая ошибка в базе данных\n");
                            return;
                        }
                        user = tmpuser;
                        Console.WriteLine("\nЗапрещенное время посылки уведомлений изменено\n");
                        Console.WriteLine();
                        Console.Write(user);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                case 8:
                    return;
                case 9:
                    Console.WriteLine("\nВы действительно хотите удалить свою учетную запись (y/n)?\n");
                    var choice = Console.ReadLine();
                    if (choice == null)
                    {
                        Console.WriteLine("\nОшибка ввода\n");
                        break;
                    }
                    if (choice == "n")
                        break;
                    else if (choice == "y")
                    {
                        try
                        {
                            task_service.DeleteUser(user.NameID);
                            Console.WriteLine("\nУчетная запись успешно удалена\n");
                            return;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\nКритическая ошибка в базе данных\n");
                        }
                    }
                    else
                        Console.WriteLine("Введите y/n");
                    break;

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
            Console.WriteLine("Ошибка ввода");
            return null;
        }
        Console.WriteLine("Введите пароль:");
        password = Console.ReadLine();
        if (password == null)
        {
            Console.WriteLine("Ошибка ввода");
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
            Console.WriteLine("Ошибка ввода");
            return null;
        }
        Console.WriteLine("Введите номер телефона:");
        phone_string = Console.ReadLine();
        if (phone_string == null)
        {
            Console.WriteLine("Ошибка ввода");
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
            Console.WriteLine("Ошибка ввода");
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
            if (!Int32.TryParse(Console.ReadLine(), out opt) || opt < 0 || opt > 3)
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
                    LoggedCycle(task_service, user);
                    break;
                case 3:
                    Log.CloseAndFlush();
                    return;
            }
        }
    }
    static void Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Error)
            .CreateLogger();
        try
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog();
                })
                .AddSingleton<IEventRepo, PostgresEventRepo>()
                .AddSingleton<IHabitRepo, PostgresHabitRepo>()
                .AddSingleton<IUserRepo, PostgresUserRepo>()
                .AddDbContext<PostgresDBContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("PostgresConnection")))
                .AddTransient<ISheduleLoad, ShedAdapter>()
                .AddTransient<ITaskTracker, TaskTracker>()
                .AddTransient<IHabitDistributor, HabitDistributor>()
                .BuildServiceProvider();

            //Log.Information("Приложение запущено");
            var taskService = serviceProvider.GetRequiredService<ITaskTracker>();
            LogInCycle(taskService);
            //Log.Information("Приложение остановлено");
            //Log.CloseAndFlush();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Приложение завершилось с неизвестной ошибкой");
        }
        finally
        {
            //Log.Information("Приложение остановлено");
            Log.CloseAndFlush();
        }
    }
}