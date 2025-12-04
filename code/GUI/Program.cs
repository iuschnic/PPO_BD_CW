using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using PublicTaskTrackerClient;

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
                var baseUrl = configuration.GetValue<string>("BaseUrl");
                if (baseUrl == null)
                {
                    Console.WriteLine("Ошибка чтения конфигурации");
                    return;
                }
                var services = new ServiceCollection();
                ConfigureServices(services, baseUrl, configuration);
                var serviceProvider = services.BuildServiceProvider();
                var taskService = serviceProvider.GetRequiredService<IPublicTaskTrackerClient>();
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
}