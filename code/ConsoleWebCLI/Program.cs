using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using MessageSenderTaskTrackerClient;
class Program
{
    private static Habit? ParseHabit(string user_name)
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
    private async static Task LoggedCycle(IPublicTaskTrackerClient task_service, User user)
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
                        if (!File.Exists(path))
                        {
                            Console.WriteLine($"\nФайл '{path}' не найден\n");
                            break;
                        }
                        var extension = Path.GetExtension(path).ToLowerInvariant();
                        if (extension != ".csv" && extension != ".ics")
                        {
                            Console.WriteLine($"\nНеподдерживаемый формат файла: {extension}. Поддерживаются только .csv и .ics\n");
                            break;
                        }
                        using var stream = File.OpenRead(path);

                        ret = await task_service.ImportNewScheduleAsync(user.NameID, stream, extension);
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
                    var habit = ParseHabit(user.NameID);
                    if (habit == null)
                        break;
                    try
                    {
                        ret = await task_service.AddHabitAsync(habit);
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
                        ret = await task_service.DeleteHabitAsync(user.NameID, name);
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
                        ret = await task_service.DeleteHabitsAsync(user.NameID);
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
                        user = await task_service.ChangeSettingsAsync(null, true, user.NameID);
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
                        user = await task_service.ChangeSettingsAsync(null, false, user.NameID);
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
                        List<Tuple<TimeOnly, TimeOnly>> timings = [];
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
                            timings.Add(Tuple.Create(start, end));
                        }
                        user = await task_service.ChangeSettingsAsync(timings, null, user.NameID);
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
                            await task_service.DeleteUserAsync(user.NameID);
                            Console.WriteLine("\nУчетная запись успешно удалена\n");
                            return;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    else
                        Console.WriteLine("Введите y/n");
                    break;

            }
        }
    }
    private async static Task<User?> LogIn(IPublicTaskTrackerClient task_service)
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
        try
        {
            var user = await task_service.LogInAsync(user_name, password);
            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }
    private async static Task<User?> CreateUser(IPublicTaskTrackerClient task_service)
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
        try
        {
            var user = await task_service.CreateUserAsync(user_name, phone_number, password);
            return user;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }
    private async static Task LogInCycle(IPublicTaskTrackerClient task_service)
    {
        int opt;
        User? user;
        while (true)
        {
            Console.WriteLine("1) Создать аккаунт\n2) Войти в аккаунт\n3) Выйти из программы\n");
            if (!Int32.TryParse(Console.ReadLine(), out opt) || opt < 0 || opt > 3)
                continue;
            switch (opt)
            {
                case 1:
                    user = await CreateUser(task_service);
                    if (user == null)
                        break;
                    Console.Write(user);
                    break;
                case 2:
                    user = await LogIn(task_service);
                    if (user == null)
                        break;
                    Console.Write(user);
                    await LoggedCycle(task_service, user);
                    break;
                case 3:
                    Log.CloseAndFlush();
                    return;
            }
        }
    }
    static async Task Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        var baseUrl = configuration.GetValue<string>("BaseUrl");
        if (baseUrl == null)
        {
            Console.WriteLine("Ошибка чтения конфигурации");
            return;
        }
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Error)
            .CreateLogger();
        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services, baseUrl, configuration);
            var serviceProvider = services.BuildServiceProvider();
            var taskService = serviceProvider.GetRequiredService<IPublicTaskTrackerClient>();
            await LogInCycle(taskService);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Приложение завершилось с неизвестной ошибкой");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
    static void ConfigureServices(IServiceCollection services, string baseUrl, IConfigurationRoot configuration)
    {
        services.AddSingleton<IConfiguration>(configuration)
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog();
                });
        services.AddHttpClient<IPublicTaskTrackerClient, WebPublicTaskTrackerClient>((provider, client) =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
    }
}