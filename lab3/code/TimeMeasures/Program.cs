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
        taskService.ImportNewShedule(user_name, "dummy");
    }
}
