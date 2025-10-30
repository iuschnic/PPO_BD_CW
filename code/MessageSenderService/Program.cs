using MessageSenderDomain.OutPorts;
using MessageSenderStorage.EfAdapters;
using MessageSenderTaskTrackerClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        var secretConfiguration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.secrets.json", optional: false, reloadOnChange: true)
            .Build();
        var baseUrl = Environment.GetEnvironmentVariable("BASE_URL")
            ?? configuration.GetValue<string>("BaseUrl");
        var connString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? configuration.GetConnectionString("PostgresConnection");
        var secretKey = secretConfiguration.GetValue<string>("SecretKey");
        var botToken = secretConfiguration.GetValue<string>("BotToken");
        if (baseUrl == null || connString == null || secretKey == null || botToken == null)
        {
            Console.WriteLine("Ошибка чтения конфигурации");
            return;
        }
        var services = new ServiceCollection();

        ConfigureServices(services, baseUrl, connString, secretKey);

        var serviceProvider = services.BuildServiceProvider();

        var bot = new MessageSender(
            botToken,
            serviceProvider.GetRequiredService<IMessageRepo>(),
            serviceProvider.GetRequiredService<ISubscriberRepo>(),
            serviceProvider.GetRequiredService<ISenderTaskTrackerClient>());

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

    static void ConfigureServices(IServiceCollection services, string baseUrl, string connString, string secretKey)
    {
        services.AddSingleton<IMessageRepo, EfMessageRepo>();
        services.AddSingleton<ISubscriberRepo, EfSubscriberRepo>();
        services.AddDbContext<MessageSenderDBContext>(options =>
            options.UseNpgsql(connString));
        services.AddHttpClient<ISenderTaskTrackerClient, WebSenderTaskTrackerClient>((provider, client) =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("X-Microservice-Auth", secretKey);
        });
    }
}
