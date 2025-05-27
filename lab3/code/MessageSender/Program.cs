using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Domain.OutPorts;
using Microsoft.Extensions.DependencyInjection;
using Storage.PostgresStorageAdapters;
using Storage.StorageAdapters;
using StorageSubscribers;

public class Subscriber
{
    public long ChatId { get; set; }
    public string TaskTrackerLogin { get; set; }
    public string Password { get; set; }
    public string Username { get; set; }
    public DateTime SubscriptionDate { get; set; }
    public Subscriber(long chatId, string taskTrackerLogin, string password, string username, DateTime subscriptionDate)
    {
        ChatId = chatId;
        TaskTrackerLogin = taskTrackerLogin;
        Password = password;
        Username = username;
        SubscriptionDate = subscriptionDate;
    }
}

public interface ISubscribersRepo
{
    Subscriber? TryGetByChatID(long chat_id);
    Subscriber? TryGetByTaskTrackerLogin(string task_tracker_login);
    bool IfAnyChatID(long chat_id);
    bool TryAdd(Subscriber subscriber);
    bool TryRemoveByChatID(long chat_id);
}

public class SubscriptionBot
{
    private readonly ITelegramBotClient _botClient;
    private readonly IMessageRepo _messageRepo;
    private readonly IUserRepo _userRepo;
    private readonly ISubscribersRepo _subscribersRepo;
    private CancellationTokenSource _cts;

    private enum RegistrationState
    {
        None,
        AwaitingLogin,
        AwaitingPassword
    }

    private Dictionary<long, RegistrationState> _registrationStates = new();
    private Dictionary<long, string> _tempLogins = new();

    private const string WelcomeMessage = "Добро пожаловать! Введите ваш логин в системе TaskTracker:";
    private const string AskPasswordMessage = "Теперь введите ваш пароль в системе TaskTracker:";
    private const string RegistrationCompleteMessage = "Регистрация завершена! Вы подписаны на рассылку.";
    private const string GoodbyeMessage = "Вы отписались от рассылки";
    private const int timeout_send = 1;
    private const int timeout_generate = 30;
    private const int timeout_err = 1;

    public SubscriptionBot(string botToken, IMessageRepo messageRepo, IUserRepo userRepo, ISubscribersRepo subscriberRepo)
    {
        _botClient = new TelegramBotClient(botToken);
        _messageRepo = messageRepo;
        _userRepo = userRepo;
        _subscribersRepo = subscriberRepo;
    }

    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _cts.Token
        );

        _ = Task.Run(StartBroadcasting, _cts.Token);
        _ = Task.Run(StartCreatingMessages, _cts.Token);

        Console.WriteLine("Бот запущен. Нажмите Ctrl+C для остановки.");
        await Task.Delay(-1, _cts.Token);
    }

    private async Task StartBroadcasting()
    {
        while (!_cts.IsCancellationRequested)
        {
            var users_habits = _messageRepo.GetUsersToNotify();
            Console.WriteLine("StartBroadcasting");
            List<Domain.Models.Message> messages = [];
            try
            {
                int cnt = 0;
                var to_send = _messageRepo.GetMessagesToSend();
                List<Domain.Models.Message> sent_messages = [];
                foreach (var send in to_send)
                {
                    var subscriber = _subscribersRepo.TryGetByTaskTrackerLogin(send.UserNameID);

                    if (subscriber != null)
                    {
                        await _botClient.SendMessage(
                            chatId: subscriber.ChatId,
                            text: send.Text
                        );
                        cnt++;
                        sent_messages.Add(new Domain.Models.Message(send.Id, send.Text, DateTime.Now, send.TimeOutdated, true, send.UserNameID));
                    }
                }
                _messageRepo.MarkMessagesSent(sent_messages);
                Console.WriteLine($"{DateTime.Now}: Отправлено {cnt} сообщений");
                await Task.Delay(TimeSpan.FromMinutes(timeout_send), _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при рассылке: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(timeout_err), _cts.Token);
            }
        }
    }

    private async Task StartCreatingMessages()
    {
        while (!_cts.IsCancellationRequested)
        {
            Console.WriteLine("StartCreating");
            var users_habits = _messageRepo.GetUsersToNotify();
            List<Domain.Models.Message> messages = [];
            try
            {
                foreach (var user_habit in users_habits)
                {
                    var subscriber = _subscribersRepo.TryGetByTaskTrackerLogin(user_habit.UserName);
                    if (subscriber != null)
                    {
                        var text = $"Привет, {subscriber.Username}!\n" +
                                  $"Логин: {subscriber.TaskTrackerLogin}\n" +
                                  $"В ближайшие 30 минут нужно будет выполнить привычку: " +
                                  $"{user_habit.HabitName ?? "не указана"} ({user_habit.Start} - {user_habit.End})\n";
                        DateTime outdated = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                            user_habit.End.Hour, user_habit.End.Minute, user_habit.End.Second);

                        messages.Add(new Domain.Models.Message(Guid.NewGuid(), text, null, outdated, false, user_habit.UserName));
                    }
                }
                Console.WriteLine($"{DateTime.Now}: Создано {messages.Count} сообщений");
                _messageRepo.TryCreateMessages(messages);
                await Task.Delay(TimeSpan.FromMinutes(timeout_generate), _cts.Token);
            }
            catch (Exception ex)
            {
                await Task.Delay(TimeSpan.FromMinutes(timeout_err), _cts.Token);
            }
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Message is not { } message)
                return;

            var chatId = message.Chat.Id;
            var text = message.Text?.Trim() ?? string.Empty;

            if (text == "/start")
            {
                await HandleStartCommand(botClient, message);
            }
            else if (text == "/stop")
            {
                await HandleStopCommand(botClient, message);
            }
            else if (text != null && _registrationStates.TryGetValue(chatId, out var state))
            {
                if (state == RegistrationState.AwaitingLogin)
                {
                    var u = _userRepo.TryGet(text);
                    if (u == null)
                        await botClient.SendMessage(chatId, "К сожалению такого логина в системе не найдено, попробуйте еще раз.\n\n" + WelcomeMessage);
                    else
                    {
                        _tempLogins[chatId] = text;
                        _registrationStates[chatId] = RegistrationState.AwaitingPassword;
                        await botClient.SendMessage(chatId, AskPasswordMessage);
                    }
                }
                else if (state == RegistrationState.AwaitingPassword)
                {
                    var u = _userRepo.TryGet(_tempLogins[chatId]);
                    if (u == null)
                        await botClient.SendMessage(chatId, "Критическая ошибка, учетная запись не найдена.\n\n" + WelcomeMessage);
                    else if (u != null && u.PasswordHash != text)
                        await botClient.SendMessage(chatId, "Неправильный пароль, попробуйте еще раз.\n\n" + AskPasswordMessage);
                    else
                    {
                        var subscriber = new Subscriber(chatId, _tempLogins[chatId], text, 
                            message.From.Username ?? message.From.FirstName, DateTime.Now);
                        if (!_subscribersRepo.TryAdd(subscriber))
                            throw new Exception("Ошибка, пользователь существует");

                        _registrationStates.Remove(chatId);
                        _tempLogins.Remove(chatId);

                        Console.WriteLine($"Пользователь подписался: {subscriber.Username}, Логин: {subscriber.TaskTrackerLogin}");

                        await botClient.SendMessage(chatId, RegistrationCompleteMessage);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
        }
    }

    private async Task HandleStartCommand(ITelegramBotClient botClient, Message message)
    {
        var chatId = message.Chat.Id;
        if (_subscribersRepo.IfAnyChatID(chatId))
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Вы уже подписаны на рассылку"
            );
            return;
        }

        _registrationStates[chatId] = RegistrationState.AwaitingLogin;
        await botClient.SendMessage(chatId, WelcomeMessage);
    }

    private async Task HandleStopCommand(ITelegramBotClient botClient, Message message)
    {
        var chatId = message.Chat.Id;
        var subscriber = _subscribersRepo.TryGetByChatID(chatId);
        if (subscriber != null)
        {
            if (!_subscribersRepo.TryRemoveByChatID(chatId))
                throw new Exception("Ошибка, пользователь не существует");

            await botClient.SendMessage(
                chatId: chatId,
                text: GoodbyeMessage
            );

            Console.WriteLine($"Пользователь отписался: {subscriber.Username}, Логин: {subscriber.TaskTrackerLogin}");
        }
        else
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Вы не были подписаны"
            );
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        Console.WriteLine("Бот остановлен.");
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IMessageRepo, PostgresMessageRepo>()
            .AddSingleton<IUserRepo, PostgresUserRepo>()
            .AddSingleton<ISubscribersRepo, SQLiteSubscribersRepo>()
            .AddDbContext<PostgresDBContext>(options =>
                options.UseNpgsql("Host=localhost;Port=5432;Database=habitsdb;Username=postgres;Password=postgres"))
            .AddDbContext<SubscribersDBContext>(options =>
                options.UseSqlite("Data Source=subscribers.db"))
            .BuildServiceProvider();

        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SubscribersDBContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var bot = new SubscriptionBot(
            "7665679478:AAHtpesgjfWihplWtkBB7Iuwot-6gCElWVY",
            serviceProvider.GetRequiredService<IMessageRepo>(),
            serviceProvider.GetRequiredService<IUserRepo>(),
            serviceProvider.GetRequiredService<ISubscribersRepo>());

        Console.CancelKeyPress += async (sender, e) =>
        {
            await bot.StopAsync();
            Environment.Exit(0);
        };

        await bot.StartAsync();
    }
}