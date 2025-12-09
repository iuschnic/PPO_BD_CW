using Domain;
using Domain.OutPorts;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit2;
using MessageSenderBotAdapters;
using MessageSenderDomain.OutPorts;
using MessageSenderStorage.EfAdapters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PublicTaskTrackerClient;
using Storage.EfAdapters;
using System.Text;
using System.Text.RegularExpressions;
using Tests.E2ETests;

namespace Tests.BDDTests;

[FeatureDescription(
@"As a user
I want to have two-factor
auth for my account to be
secured")]
[Label("Story-2")]
public partial class TwoFactorAuthFeature
{
    [Scenario]
    [Label("Two-Factor")]
    public async Task TwoFactorAuth()
    {
        string userName = "test_bdd_user";
        await Runner.RunScenarioAsync(
            _ => GivenUserHasCreatedAccountInTaskTracker(userName),
            _ => AndUserHasCreatedAccountInMessageSender(userName),
            _ => UserRequestsTwoFactorAuthEnabled(userName),
            _ => AndThenUserMakesAnUnsuccessfulAttemptToLogInWithoutTwoFactor(userName),
            _ => AndThenUserAwaitsForTwoFactorCodeFromMessageSender(),
            _ => AndThenHeSuccessfullyLogsInWithValidPasswordAndTwoFactorCode(userName));
    }
}

public partial class TwoFactorAuthFeature : FeatureFixture, IAsyncLifetime
{

    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _testToken;
    private readonly string _testerToken;
    private readonly long _testChatId;
    private readonly long _testerChatId;
    private readonly IPublicTaskTrackerClient _taskTrackerClient;
    private TesterTelegram _tester;
    private readonly MessageSenderDBContext _dbContextMessageSender;
    private readonly EfDbContext _dbContextTaskTracker;
    private MessageSender _messageSender;
    private readonly int _port = 5234;
    private readonly string _url = $"http://localhost:5234";
    private string _password = "test_password";
    private string _twoFactor = "";
    private Types.PhoneNumber _phone = new Types.PhoneNumber("+79999999999");


    public TwoFactorAuthFeature()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("tests_settings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<TelegramBotE2E>()
            .Build();
        string? connStringTaskTracker;
        if ((connStringTaskTracker = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                          ?? configuration.GetConnectionString("E2ETestsConnection")) == null)
            throw new InvalidDataException("Не найдена строка подключения к тестовой базе данных");
        var factory = new WebApplicationFactory<Program>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EfDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);
                services.AddDbContext<EfDbContext>(options =>
                {
                    options.UseNpgsql(connStringTaskTracker);
                });
                builder.UseUrls(_url);
                builder.ConfigureKestrel(options =>
                {
                    options.ListenLocalhost(_port);
                });

                var descriptor2 = services.SingleOrDefault(
                    d => d.ServiceType == typeof(TaskTrackerArgs));
                if (descriptor2 != null)
                    services.Remove(descriptor2);
                services.AddSingleton(new TaskTrackerArgs(5, 120, 1));
            });
        });

        // Конфигурация клиента основного сервиса TaskTracker WebAPI
        _taskTrackerClient = new WebPublicTaskTrackerClient(_factory.CreateClient());


        string? connStringMessageSender;
        if ((connStringMessageSender = Environment.GetEnvironmentVariable("DB_MESSAGE_SENDER_CONNECTION_STRING")
                          ?? configuration.GetConnectionString("E2EMessageSenderTestsConnection")) == null)
            throw new InvalidDataException("Не найдена строка подключения к тестовой базе данных");

        if ((_testToken = Environment.GetEnvironmentVariable("TEST_TOKEN")
                          ?? configuration.GetValue<string>("TestToken")) == null)
            throw new InvalidDataException("Не найдена строка токена тестируемого бота");

        if ((_testerToken = Environment.GetEnvironmentVariable("TESTER_TOKEN")
                          ?? configuration.GetValue<string>("TesterToken")) == null)
            throw new InvalidDataException("Не найдена строка токена тестирующего бота");

        string? testChatId;
        if ((testChatId = Environment.GetEnvironmentVariable("TEST_CHAT_ID")
                          ?? configuration.GetValue<string>("TestChatId")) == null)
            throw new InvalidDataException("Не найден Id чата для тестов");
        if (!long.TryParse(testChatId, out _testChatId))
            throw new InvalidDataException("Неверный Id чата для тестов");

        string? testerChatId;
        if ((testerChatId = Environment.GetEnvironmentVariable("TESTER_CHAT_ID")
                          ?? configuration.GetValue<string>("TesterChatId")) == null)
            throw new InvalidDataException("Не найден Id чата для тестов");
        if (!long.TryParse(testerChatId, out _testerChatId))
            throw new InvalidDataException("Неверный Id чата для тестов");

        var serviceProvider = new ServiceCollection()
                .AddDbContext<MessageSenderDBContext>(options =>
                    options.UseNpgsql(connStringMessageSender))
                .AddDbContext<EfDbContext>(options =>
                    options.UseNpgsql(connStringTaskTracker))
                .BuildServiceProvider();

        _dbContextMessageSender = serviceProvider.GetRequiredService<MessageSenderDBContext>();
        _dbContextTaskTracker = serviceProvider.GetRequiredService<EfDbContext>();

        var messageRepo = new EfMessageRepo(_dbContextMessageSender);
        var subscriberRepo = new EfSubscriberRepo(_dbContextMessageSender);

        var mockTaskTrackerClient = new Mock<ISenderTaskTrackerClient>();
        mockTaskTrackerClient.Setup(client => client.GetUsersToNotifyAsync())
            .ReturnsAsync([]);
        mockTaskTrackerClient.Setup(client => client.TryLogInAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        mockTaskTrackerClient.Setup(client => client.TryUnableTwoFactor(It.IsAny<string>()))
            .ReturnsAsync(true);
        var mockMessageSenderClient = new Mock<IMessageSenderClient>();

        var httpListenerBaseUrl = Environment.GetEnvironmentVariable("HTTP_LISTENER_BASE_URL")
                ?? configuration.GetValue<string>("HttpListenerBaseUrl");
        if (httpListenerBaseUrl == null)
        {
            Console.WriteLine("Ошибка чтения конфигурации");
            return;
        }
        // Конфигурация эмулятора клиента сервиса MessageSender
        _tester = new TesterTelegram(_testerToken, _testerChatId);
        // Концигурация сервиса MessageSender
        var botClient = new TelegramBotAdapter(new TelegramBotAdapterArgs(_testToken, _testChatId));
        _messageSender = new MessageSender(botClient, messageRepo, subscriberRepo, mockTaskTrackerClient.Object,
            new MessageSenderArgs(httpListenerBaseUrl));
        /* Итого на выходе:
         * _taskTrackerClient - клиент для основного приложения по WebAPI, под капотом запускает сам WebAPI
         * _messageSender - второй микросервис
         * _tester - клиент второго микросервиса
         */
    }

    public async Task InitializeAsync()
    {
        await CleanDatabasesAsync();
        await _tester.StartListeningAsync();
        _ = Task.Run(() => _messageSender.StartAsync());
        await Task.Delay(3000);
    }

    public async Task DisposeAsync()
    {
        await _tester.StopListeningAsync();
        await _factory.DisposeAsync();
        await CleanDatabasesAsync();
        await _dbContextTaskTracker.DisposeAsync();
        await _dbContextMessageSender.DisposeAsync();
    }

    private async Task CleanDatabasesAsync()
    {
        if (_dbContextTaskTracker == null || _dbContextMessageSender == null) return;
        _dbContextTaskTracker.ChangeTracker.AutoDetectChangesEnabled = false;
        await _dbContextTaskTracker.Events.ExecuteDeleteAsync();
        await _dbContextTaskTracker.ActualTimes.ExecuteDeleteAsync();
        await _dbContextTaskTracker.PrefFixedTimes.ExecuteDeleteAsync();
        await _dbContextTaskTracker.Habits.ExecuteDeleteAsync();
        await _dbContextTaskTracker.SettingsTimes.ExecuteDeleteAsync();
        await _dbContextTaskTracker.USettings.ExecuteDeleteAsync();
        await _dbContextTaskTracker.Users.ExecuteDeleteAsync();
        await _dbContextTaskTracker.SaveChangesAsync();
        _dbContextTaskTracker.ChangeTracker.Clear();

        _dbContextMessageSender.ChangeTracker.AutoDetectChangesEnabled = false;
        await _dbContextMessageSender.SubscriberMessage.ExecuteDeleteAsync();
        await _dbContextMessageSender.Subscribers.ExecuteDeleteAsync();
        await _dbContextMessageSender.Messages.ExecuteDeleteAsync();
        await _dbContextMessageSender.SaveChangesAsync();
        _dbContextMessageSender.ChangeTracker.Clear();
    }

    private async Task GivenUserHasCreatedAccountInTaskTracker(string userName)
    {
        var response = await _taskTrackerClient.CreateUserAsync(userName, _phone, _password);
        Assert.NotNull(response);
    }
    private async Task AndUserHasCreatedAccountInMessageSender(string userName)
    {
        await _tester.SendAndWaitResponseAsync("/start");
        await _tester.SendAndWaitResponseAsync(userName);
        await _tester.SendAndWaitResponseAsync(_password);
    }
    private async Task UserRequestsTwoFactorAuthEnabled(string userName)
    {
        await _taskTrackerClient.ChangeTwoFactorAuthAsync(userName, true);
    }

    private async Task AndThenUserMakesAnUnsuccessfulAttemptToLogInWithoutTwoFactor(string userName)
    {
        try
        {
            var response = await _taskTrackerClient.LogInAsync(userName, _password);
        }
        catch (Exception ex)
        {
            Assert.Contains(ex.Message, "Ошибка входа");
        }
    }
    private async Task AndThenUserAwaitsForTwoFactorCodeFromMessageSender()
    {
        await _tester.WaitResponseAsync();
        string ans = _tester.GetLastMessage();
        string pattern = @"\d+";
        Regex regex = new Regex(pattern);
        var matches = regex.Matches(ans);
        _twoFactor = matches[0].Value;
    }
    private async Task AndThenHeSuccessfullyLogsInWithValidPasswordAndTwoFactorCode(string userName)
    {
        var response = await _taskTrackerClient.LogInAsync(userName, _password, _twoFactor);
        Assert.NotNull(response);
    }
}
