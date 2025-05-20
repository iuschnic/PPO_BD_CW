using Domain;
using Domain.InPorts;
using Domain.OutPorts;
using Domain.Models;
using LoadAdapters;
using Microsoft.Extensions.DependencyInjection;
using Storage.PostgresStorageAdapters;
using Types;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Npgsql;


class Program
{
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
        string user_name = "Measures";
        taskService.CreateUser(user_name, new PhoneNumber("+79161648345"), "1");
        var ans = taskService.ImportNewSheduleForMeasures(user_name);
        if (ans == null)
        {
            Console.WriteLine("ERROR import shedule\n");
            return;
        }

        // Файлы для записи результатов
        string file1 = "c#_func.dat";
        string file2 = "sql_func.dat";

        // Очищаем файлы перед записью
        File.WriteAllText(file1, "");
        File.WriteAllText(file2, "");

        // Количество повторений для каждого n
        int repetitions = 500;

        // Диапазон значений n
        int minN = 2;
        int maxN = 120; //в сутках 24 часа, 4 из них заняты событиями, берем привычки по 10 минут каждая -> максимум 120 привычек уместится в сутках
        taskService.AddHabit(new Habit(Guid.NewGuid(), "1", 10, TimeOption.NoMatter, user_name, [], [], 7));
        Console.WriteLine("Starting benchmarks...");
        ans = taskService.DeleteHabits(user_name);
        if (ans == null)
        {
            Console.WriteLine("ERROR delete habits\n");
            return;
        }

        for (int n = minN; n <= maxN; n+=2)
        {
            taskService.AddHabit(new Habit(Guid.NewGuid(), n.ToString(), 10, TimeOption.NoMatter, user_name, [], [], 7));
            Console.WriteLine($"Testing n = {n}");

            double totalTime1 = 0;
            double totalTime2 = 0;

            for (int i = 0; i < repetitions; i++)
            {
                // Замер времени для первой функции
                var sw1 = Stopwatch.StartNew();
                var ret = taskService.TryRedistributeNMTimeHabits(user_name);
                sw1.Stop();
                totalTime1 += sw1.Elapsed.TotalMilliseconds;
                if (ret == null)
                {
                    Console.WriteLine("ERROR distribute c#\n");
                    return;
                }

                // Замер времени для второй функции
                var sw2 = Stopwatch.StartNew();
                ret = taskService.TryRedistributeNMTimeHabitsDB(user_name);
                if (ret == null)
                {
                    Console.WriteLine("ERROR distribute sql\n");
                    return;
                }
                sw2.Stop();
                totalTime2 += sw2.Elapsed.TotalMilliseconds;
            }

            // Вычисляем среднее время
            double avgTime1 = totalTime1 / repetitions;
            double avgTime2 = totalTime2 / repetitions;

            // Записываем результаты в файлы
            File.AppendAllText(file1, $"{n} {avgTime1}\n");
            File.AppendAllText(file2, $"{n} {avgTime2}\n");
        }

        Console.WriteLine("Benchmarks completed. Results saved to:");
        Console.WriteLine($"- {file1}");
        Console.WriteLine($"- {file2}");

    }
}
