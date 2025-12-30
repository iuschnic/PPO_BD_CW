using MessageSenderDomain.OutPorts;
using MessageSenderStorage.EfAdapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text;
using MessageSenderBotAdapters;
using Domain.OutPorts;

namespace Tests.E2ETests;

public class TelegramBotE2E : IAsyncLifetime
{
    private TelegramBotAdapter _botClient;
    private TesterTelegram _tester;
    private long _testChatId;
    private long _testerChatId;
    private string? _testToken;
    private string? _testerToken;
    private MessageSender _messageSender;
    private readonly MessageSenderDBContext _dbContext;

    public TelegramBotE2E()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("tests_settings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<TelegramBotE2E>()
            .Build();

        string? connString;
        if ((connString = Environment.GetEnvironmentVariable("DB_MESSAGE_SENDER_CONNECTION_STRING")
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
                    options.UseNpgsql(connString))
                .BuildServiceProvider();

        _dbContext = serviceProvider.GetRequiredService<MessageSenderDBContext>();
        _botClient = new TelegramBotAdapter(new TelegramBotAdapterArgs(_testToken, _testChatId));
        _tester = new TesterTelegram(_testerToken, _testerChatId);

        var messageRepo = new EfMessageRepo(_dbContext);
        var subscriberRepo = new EfSubscriberRepo(_dbContext);
        var mocktaskTrackerClient = new Mock<ISenderTaskTrackerClient>();
        mocktaskTrackerClient.Setup(client => client.GetUsersToNotifyAsync())
            .ReturnsAsync([]);
        mocktaskTrackerClient.Setup(client => client.TryLogInAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        var mockMessageSenderClient = new Mock<IMessageSenderClient>();

        var httpListenerBaseUrl = Environment.GetEnvironmentVariable("HTTP_LISTENER_BASE_URL")
                ?? configuration.GetValue<string>("HttpListenerBaseUrl");
        if (httpListenerBaseUrl == null)
        {
            Console.WriteLine("Ошибка чтения конфигурации");
            return;
        }

        _messageSender = new MessageSender(_botClient, messageRepo, subscriberRepo, mocktaskTrackerClient.Object,
            new MessageSenderArgs(httpListenerBaseUrl));
    }

    public async Task InitializeAsync()
    {
        await CleanDatabaseAsync();
        await _tester.StartListeningAsync();
        _ = Task.Run(() => _messageSender.StartAsync());
        await Task.Delay(3000);
    }

    public async Task DisposeAsync()
    {
        await _tester.StopListeningAsync();
        await _messageSender.StopAsync();
        await CleanDatabaseAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task CleanDatabaseAsync()
    {
        if (_dbContext == null) return;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        await _dbContext.SubscriberMessage.ExecuteDeleteAsync();
        await _dbContext.Subscribers.ExecuteDeleteAsync();
        await _dbContext.Messages.ExecuteDeleteAsync();
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task FullRegistrationWithTestEnvironment()
    {
        var testLogin = "test_user1";
        var testPassword = "test_password1";
        await _tester.SendAndWaitResponseAsync("/start");
        await _tester.SendAndWaitResponseAsync(testLogin);
        await _tester.SendAndWaitResponseAsync(testPassword);
        await _tester.SendAndWaitResponseAsync("/stop");
        var responses = _tester.GetMessages();
        Assert.True(responses.Count == 4, $"Expected at least 4 responses, got {responses.Count}");
        foreach (var r in responses)
            Assert.NotNull(r);
        Assert.Contains("Добро пожаловать!", responses[0]);
        Assert.Contains("Теперь введите ваш пароль", responses[1]);
        Assert.Contains("Регистрация завершена!", responses[2]);
        Assert.Contains("Вы отписались", responses[3]);
    }
}

public class MockBotE2E : IAsyncLifetime
{
    private MockWebBotAdapter _botClient;

    private MessageSender _messageSender;
    private TesterMock _tester;
    private string _mockBaseUrl;
    private readonly MessageSenderDBContext _dbContext;

    public MockBotE2E()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("tests_settings.json", optional: false, reloadOnChange: true)
            .Build();

        string? connString;
        if ((connString = Environment.GetEnvironmentVariable("DB_MESSAGE_SENDER_CONNECTION_STRING")
                          ?? configuration.GetConnectionString("E2EMessageSenderTestsConnection")) == null)
            throw new InvalidDataException("Не найдена строка подключения к тестовой базе данных");

        string? mockBaseUrl;
        if ((mockBaseUrl = Environment.GetEnvironmentVariable("MOCK_BASE_URL")
                          ?? configuration.GetValue<string>("MockBaseUrl")) == null)
            throw new InvalidDataException("Не найдена строка BaseUrl для mock бота");
        _mockBaseUrl = mockBaseUrl;

        var serviceProvider = new ServiceCollection()
                .AddDbContext<MessageSenderDBContext>(options =>
                    options.UseNpgsql(connString))
                .BuildServiceProvider();

        _dbContext = serviceProvider.GetRequiredService<MessageSenderDBContext>();

        _botClient = new MockWebBotAdapter(new MockWebBotAdapterArgs(_mockBaseUrl));

        _tester = new TesterMock(_mockBaseUrl, 111, "tester");

        var messageRepo = new EfMessageRepo(_dbContext);
        var subscriberRepo = new EfSubscriberRepo(_dbContext);
        var mocktaskTrackerClient = new Mock<ISenderTaskTrackerClient>();
        mocktaskTrackerClient.Setup(client => client.GetUsersToNotifyAsync())
            .ReturnsAsync([]);
        mocktaskTrackerClient.Setup(client => client.TryLogInAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var httpListenerBaseUrl = Environment.GetEnvironmentVariable("HTTP_LISTENER_BASE_URL")
                ?? configuration.GetValue<string>("HttpListenerBaseUrl");
        if (httpListenerBaseUrl == null)
        {
            Console.WriteLine("Ошибка чтения конфигурации");
            return;
        }

        _messageSender = new MessageSender(_botClient, messageRepo, subscriberRepo, mocktaskTrackerClient.Object,
            new MessageSenderArgs(httpListenerBaseUrl));
    }

    public async Task InitializeAsync()
    {
        await CleanDatabaseAsync();
        _ = Task.Run(() => _messageSender.StartAsync());
        await Task.Delay(3000);
    }

    public async Task DisposeAsync()
    {
        await CleanDatabaseAsync();
        _botClient.Dispose();
        await _messageSender.StopAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task CleanDatabaseAsync()
    {
        if (_dbContext == null) return;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        await _dbContext.SubscriberMessage.ExecuteDeleteAsync();
        await _dbContext.Subscribers.ExecuteDeleteAsync();
        await _dbContext.Messages.ExecuteDeleteAsync();
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task FullRegistrationWithTestEnvironment()
    {
        var testLogin = "test_user2";
        var testPassword = "test_password2";
        await _tester.SendAndWaitResponseAsync("/start");
        await _tester.SendAndWaitResponseAsync(testLogin);
        await _tester.SendAndWaitResponseAsync(testPassword);
        await _tester.SendAndWaitResponseAsync("/stop");
        var responses = _tester.GetMessages();
        Assert.True(responses.Count == 4, $"Expected at least 4 responses, got {responses.Count}");
        foreach (var r in responses)
            Assert.NotNull(r);
        Assert.Contains("Добро пожаловать!", responses[0]);
        Assert.Contains("Теперь введите ваш пароль", responses[1]);
        Assert.Contains("Регистрация завершена!", responses[2]);
        Assert.Contains("Вы отписались", responses[3]);
    }
}