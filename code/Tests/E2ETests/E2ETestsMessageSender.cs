using MessageSenderDomain.OutPorts;
using MessageSenderStorage.EfAdapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text;
using Telegram.Bot.Types;

namespace Tests.E2ETests;

public class TelegramBotE2E : IAsyncLifetime
{
    private TelegramBotAdapter _botClient;
    private TesterBot _testerBot;
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
            .Build();

        var secret_configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("secret_tests_settings.json", optional: false, reloadOnChange: true)
            .Build();

        string? connString;
        if ((connString = Environment.GetEnvironmentVariable("DB_MESSAGE_SENDER_CONNECTION_STRING")
                          ?? configuration.GetConnectionString("E2EMessageSenderTestsConnection")) == null)
            throw new InvalidDataException("Не найдена строка подключения к тестовой базе данных");

        if ((_testToken = Environment.GetEnvironmentVariable("TEST_TOKEN")
                          ?? secret_configuration.GetValue<string>("TestToken")) == null)
            throw new InvalidDataException("Не найдена строка токена тестируемого бота");

        if ((_testerToken = Environment.GetEnvironmentVariable("TESTER_TOKEN")
                          ?? secret_configuration.GetValue<string>("TesterToken")) == null)
            throw new InvalidDataException("Не найдена строка токена тестирующего бота");

        string? testChatId;
        if ((testChatId = Environment.GetEnvironmentVariable("TEST_CHAT_ID")
                          ?? secret_configuration.GetValue<string>("TestChatId")) == null)
            throw new InvalidDataException("Не найден Id чата для тестов");
        if (!long.TryParse(testChatId, out _testChatId))
            throw new InvalidDataException("Неверный Id чата для тестов");

        string? testerChatId;
        if ((testerChatId = Environment.GetEnvironmentVariable("TESTER_CHAT_ID")
                          ?? secret_configuration.GetValue<string>("TesterChatId")) == null)
            throw new InvalidDataException("Не найден Id чата для тестов");
        if (!long.TryParse(testerChatId, out _testerChatId))
            throw new InvalidDataException("Неверный Id чата для тестов");

        var serviceProvider = new ServiceCollection()
                .AddDbContext<MessageSenderDBContext>(options =>
                    options.UseNpgsql(connString))
                .BuildServiceProvider();

        _dbContext = serviceProvider.GetRequiredService<MessageSenderDBContext>();
        _botClient = new TelegramBotAdapter(new TelegramBotAdapterArgs(_testToken, _testChatId));
        _testerBot = new TesterBot(_testerToken, _testerChatId);

        var messageRepo = new EfMessageRepo(_dbContext);
        var subscriberRepo = new EfSubscriberRepo(_dbContext);
        var mocktaskTrackerClient = new Mock<ISenderTaskTrackerClient>();
        mocktaskTrackerClient.Setup(client => client.GetUsersToNotifyAsync())
            .ReturnsAsync([]);
        mocktaskTrackerClient.Setup(client => client.TryLogInAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _messageSender = new MessageSender(_botClient, messageRepo, subscriberRepo, mocktaskTrackerClient.Object);
    }

    public async Task InitializeAsync()
    {
        await CleanDatabaseAsync();
        await _testerBot.StartListeningAsync();
        _ = Task.Run(() => _messageSender.StartAsync());
        await Task.Delay(3000);
    }

    public async Task DisposeAsync()
    {
        await _testerBot.StopListeningAsync();
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
    public async Task FullRegistrationFlow_WithTestEnvironment_Success()
    {
        var testLogin = "test_user";
        var testPassword = "test_password";
        await _testerBot.SendAndWaitResponseAsync("/start");
        await _testerBot.SendAndWaitResponseAsync(testLogin);
        await _testerBot.SendAndWaitResponseAsync(testPassword);
        await _testerBot.SendAndWaitResponseAsync("/stop");
        var responses = _testerBot.GetMessages();
        Assert.True(responses.Count == 4, $"Expected at least 4 responses, got {responses.Count}");
        foreach (var r in responses)
            Assert.NotNull(r);
        Assert.Contains("Добро пожаловать!", responses[0]);
        Assert.Contains("Теперь введите ваш пароль", responses[0]);
        Assert.Contains("Регистрация завершена!", responses[0]);
        Assert.Contains("Вы отписались", responses[0]);
    }
}