using MessageSenderDomain.OutPorts;
using MessageSenderStorage.EfAdapters;
using MessageSenderTaskTrackerClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MessageSenderBotAdapters;

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

        var services = new ServiceCollection();
        bool parsed = bool.TryParse(Environment.GetEnvironmentVariable("USE_MOCK"), out bool useMock);
        if (!parsed)
            useMock = configuration.GetValue<bool>("UseMock");
        if (!useMock)
        {
            var baseUrl = Environment.GetEnvironmentVariable("BASE_URL")
                ?? configuration.GetValue<string>("BaseUrl");
            var connString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                ?? configuration.GetConnectionString("PostgresConnection");
            var secretKey = Environment.GetEnvironmentVariable("SECRET_KEY")
                ?? secretConfiguration.GetValue<string>("SecretKey");
            var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN")
                ?? secretConfiguration.GetValue<string>("BotToken");
            if (baseUrl == null || connString == null || secretKey == null || botToken == null)
            {
                Console.WriteLine("Ошибка чтения конфигурации");
                return;
            }
            ConfigureServices(services, baseUrl, connString, secretKey, botToken);
        }
        else
        {
            var baseUrl = Environment.GetEnvironmentVariable("BASE_URL")
                ?? configuration.GetValue<string>("BaseUrl");
            var mockBaseUrl = Environment.GetEnvironmentVariable("MOCK_BASE_URL")
                ?? configuration.GetValue<string>("MockBaseUrl");
            var connString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                ?? configuration.GetConnectionString("PostgresConnection");
            var secretKey = Environment.GetEnvironmentVariable("SECRET_KEY")
                ?? secretConfiguration.GetValue<string>("SecretKey");
            var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN")
                ?? secretConfiguration.GetValue<string>("BotToken");
            if (baseUrl == null || connString == null || secretKey == null || botToken == null)
            {
                Console.WriteLine("Ошибка чтения конфигурации");
                return;
            }
            ConfigureServicesMock(services, baseUrl, connString, mockBaseUrl, secretKey);
        }

        var serviceProvider = services.BuildServiceProvider();

        var bot = new MessageSender(
            serviceProvider.GetRequiredService<IBotClient>(),
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

    static void ConfigureServices(IServiceCollection services, string baseUrl, string connString,
        string secretKey, string botToken)
    {
        var botArgs = new TelegramBotAdapterArgs(botToken);
        services.AddSingleton(botArgs);
        services.AddSingleton<IMessageRepo, EfMessageRepo>();
        services.AddSingleton<ISubscriberRepo, EfSubscriberRepo>();
        services.AddSingleton<IBotClient, TelegramBotAdapter>();
        services.AddDbContext<MessageSenderDBContext>(options =>
            options.UseNpgsql(connString));
        services.AddHttpClient<ISenderTaskTrackerClient, WebSenderTaskTrackerClient>((provider, client) =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("X-Microservice-Auth", secretKey);
        });
    }
    static void ConfigureServicesMock(IServiceCollection services, string baseUrl, string connString,
        string mockBaseUrl, string secretKey)
    {
        var mockArgs = new MockWebBotAdapterArgs(mockBaseUrl);
        services.AddSingleton(mockArgs);
        services.AddSingleton<IMessageRepo, EfMessageRepo>();
        services.AddSingleton<ISubscriberRepo, EfSubscriberRepo>();
        services.AddSingleton<IBotClient, MockWebBotAdapter>();
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
