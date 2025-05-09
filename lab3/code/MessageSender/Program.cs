using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Domain.OutPorts;
using Microsoft.Extensions.DependencyInjection;
using Storage.PostgresStorageAdapters;
using Storage.StorageAdapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Domain.InPorts;

public class Subscriber
{
    public long ChatId { get; set; }
    public string TaskTrackerName { get; set; }
    public string Username { get; set; }
    public DateTime SubscriptionDate { get; set; }
}

public class SubscriptionBot
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<long, Subscriber> _subscribers = new();
    private CancellationTokenSource _cts;

    private const string WelcomeMessage = "Введите ваш user_name в системе TaskTracker для подписки:";
    private const string GoodbyeMessage = "Вы отписались от рассылки";
    private const string IdSavedMessage = "Ваш user_name в системе TaskTracker сохранён! Вы подписаны на рассылку.";
    private const int timeout = 1;
    private const int timeout_err = 1;

    private readonly IMessageRepo _messageRepo;

    public SubscriptionBot(string botToken, IMessageRepo messageRepo)
    {
        _botClient = new TelegramBotClient(botToken);
        _messageRepo = messageRepo;
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

        Console.WriteLine("Бот запущен. Нажмите Ctrl+C для остановки.");
        await Task.Delay(-1, _cts.Token);
    }

    private async Task StartBroadcasting()
    {
        while (!_cts.IsCancellationRequested)
        {
            var users_habits = _messageRepo.GetUsersToNotify();
            try
            {
                foreach (var user in users_habits)
                {
                    var sub = _subscribers.Values.FirstOrDefault(s => s.TaskTrackerName == user.Item1);
                    if (sub != null)
                        await _botClient.SendMessage(
                            chatId: sub.ChatId,
                            text: $"Привет, {sub.Username}! Примерно через полчаса по расписанию нужно выполнить привычку: {user.Item2 ?? "не указана"} (аккаунт {user.Item1 ?? "не указан"})"
                        );
                }

                Console.WriteLine($"{DateTime.Now}: Сообщение отправлено {_subscribers.Count} подписчикам");
                await Task.Delay(TimeSpan.FromMinutes(timeout), _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при рассылке: {ex.Message}");
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
            else if (_subscribers.ContainsKey(chatId) && string.IsNullOrEmpty(_subscribers[chatId].TaskTrackerName))
            {
                // Сохраняем введённый пользователем идентификатор
                _subscribers[chatId].TaskTrackerName = text;
                await botClient.SendMessage(
                    chatId: chatId,
                    text: IdSavedMessage
                );
                Console.WriteLine($"Пользователь {chatId} указал идентификатор: {text}");
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

        if (_subscribers.ContainsKey(chatId))
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Вы уже подписаны на рассылку"
            );
            return;
        }

        // Создаем запись о подписчике
        _subscribers[chatId] = new Subscriber
        {
            ChatId = chatId,
            Username = message.From.Username ?? message.From.FirstName,
            SubscriptionDate = DateTime.Now,
            TaskTrackerName = null // Пока идентификатор не указан
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: WelcomeMessage
        );
    }

    private async Task HandleStopCommand(ITelegramBotClient botClient, Message message)
    {
        var chatId = message.Chat.Id;

        if (_subscribers.Remove(chatId, out var subscriber))
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: GoodbyeMessage
            );

            Console.WriteLine($"Пользователь отписался: {subscriber.Username}, ID: {subscriber.TaskTrackerName}");
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
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error: [{apiRequestException.ErrorCode}] {apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        Console.WriteLine("Бот остановлен. Список подписчиков:");

        foreach (var subscriber in _subscribers.Values)
        {
            Console.WriteLine($"- {subscriber.Username}: {subscriber.TaskTrackerName} (chatId: {subscriber.ChatId})");
        }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IMessageRepo, PostgresMessageRepo>()
            .AddDbContext<PostgresDBContext>(options => options.UseNpgsql("Host=localhost;Port=5432;Database=habits_db;Username=postgres;Password=postgres;Encoding=UTF8"))
            .BuildServiceProvider();
        var messageRepo = serviceProvider.GetRequiredService<IMessageRepo>();
        var botToken = "7665679478:AAHtpesgjfWihplWtkBB7Iuwot-6gCElWVY";
        var bot = new SubscriptionBot(botToken, messageRepo);

        Console.CancelKeyPress += (sender, e) =>
        {
            bot.StopAsync().Wait();
            Environment.Exit(0);
        };

        try
        {
            await bot.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }
}