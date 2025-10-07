using MessageSenderDomain.OutPorts;
using MessageSenderStorage.EfAdapters;
using MessageSenderTaskTrackerClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using MessageSenderDomain.OutPorts;
using MessageSenderStorage.EfAdapters;
using MessageSenderTaskTrackerClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        // Создаем ServiceCollection для DI
        var services = new ServiceCollection();

        ConfigureServices(services);

        var serviceProvider = services.BuildServiceProvider();

        var bot = new MessageSender(
            "7665679478:AAHtpesgjfWihplWtkBB7Iuwot-6gCElWVY",
            serviceProvider.GetRequiredService<IMessageRepo>(),
            serviceProvider.GetRequiredService<ISubscriberRepo>(),
            serviceProvider.GetRequiredService<ITaskTrackerClient>());

        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("Shutting down...");
            await bot.StopAsync();
            Environment.Exit(0);
        };

        Console.WriteLine("Starting MessageSender bot...");
        await bot.StartAsync();
    }

    static void ConfigureServices(IServiceCollection services)
    {
        // Репозитории и DbContext
        services.AddSingleton<IMessageRepo, EfMessageRepo>();
        services.AddSingleton<ISubscriberRepo, EfSubscriberRepo>();
        services.AddDbContext<MessageSenderDBContext>(options =>
            options.UseNpgsql("Host=localhost;Port=5432;Database=messagesenderdb;Username=postgres;Password=postgres"));

        // HttpClient для TaskTrackerClient
        services.AddHttpClient<ITaskTrackerClient, TaskTrackerClient>((provider, client) =>
        {
            client.BaseAddress = new Uri("https://localhost:7000");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("X-Microservice-Auth", "microservice-secret-key-2024");
        });
    }
}
