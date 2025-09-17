using Domain;
using Domain.InPorts;
using Domain.OutPorts;
using LoadAdapters;
using Microsoft.Extensions.DependencyInjection;
using Storage.EfAdapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace HabitTrackerGUI
{
    static class Program
    {
        [STAThread]
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
                    .AddSingleton<IEventRepo, EfEventRepo>()
                    .AddSingleton<IHabitRepo, EfHabitRepo>()
                    .AddSingleton<IUserRepo, EfUserRepo>()
                    .AddSingleton<ITaskTrackerContext, PostgresDBContext>()
                    .AddDbContext<PostgresDBContext>(options =>
                        options.UseNpgsql(configuration.GetConnectionString("PostgresConnection")))
                    .AddTransient<ISheduleLoad, ShedAdapter>()
                    .AddTransient<ITaskTracker, TaskTracker>()
                    .AddTransient<IHabitDistributor, HabitDistributor>()
                    .BuildServiceProvider();

                var taskService = serviceProvider.GetRequiredService<ITaskTracker>();
                Application.Run(new MainForm(taskService));
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
    }
}