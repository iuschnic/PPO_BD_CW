using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MessageSenderDomain.Models;
using MessageSenderDomain.OutPorts;

using TelegramMessage = Telegram.Bot.Types.Message;
using DomainMessage = MessageSenderDomain.Models.Message;

public class MessageSender
{
    private readonly ITelegramBotClient _botClient;
    private readonly IMessageRepo _messageRepo;
    private readonly ISubscriberRepo _subscribersRepo;
    private readonly ISenderTaskTrackerClient _taskTrackerClient;
    private readonly CancellationTokenSource _cts = new();

    private enum RegistrationState
    {
        None,
        AwaitingLogin,
        AwaitingPassword
    }

    private Dictionary<long, RegistrationState> _registrationStates = [];
    private Dictionary<long, string> _tempLogins = [];

    private const string WelcomeMessage = "Добро пожаловать! Введите ваш логин в системе TaskTracker:";
    private const string AskPasswordMessage = "Теперь введите ваш пароль в системе TaskTracker:";
    private const string RegistrationCompleteMessage = "Регистрация завершена! Вы подписаны на рассылку.";
    private const string GoodbyeMessage = "Вы отписались от рассылки.";
    private const int timeout_send = 1;
    private const int timeout_generate = 30;
    private const int timeout_err = 1;

    public MessageSender(string botToken, IMessageRepo messageRepo, 
        ISubscriberRepo subscriberRepo, ISenderTaskTrackerClient taskTrackerClient)
    {
        _botClient = new TelegramBotClient(botToken);
        _messageRepo = messageRepo;
        _subscribersRepo = subscriberRepo;
        _taskTrackerClient = taskTrackerClient;
    }

    public async Task StartAsync()
    {
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
            Console.WriteLine("StartBroadcasting");
            try
            {
                int cnt = 0;
                var toSend = _messageRepo.TryGetMessagesToSend() ?? throw new Exception("Ошибка получения сообщений из базы данных");
                List<DomainMessage> sentMessages = [];
                foreach (var send in toSend)
                {
                    await _botClient.SendMessage(
                        chatId: send.SubscriberID,
                        text: send.Text
                    );
                    cnt++;
                    sentMessages.Add(new DomainMessage(send.Id, send.Text, send.TimeSent,
                        send.TimeOutdated, send.WasSent, send.TaskTrackerLogin, send.SubscriberID));
                }
                _messageRepo.MarkMessagesSent(sentMessages);
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
            var usersHabits = await _taskTrackerClient.GetUsersToNotifyAsync();
            List<DomainMessage> messages = [];
            try
            {
                foreach (var userHabit in usersHabits)
                {
                    var subscriber = _subscribersRepo.TryGetByTaskTrackerLogin(userHabit.UserName);
                    if (subscriber != null)
                    {
                        var text = $"Привет, {subscriber.Username}!\n" +
                                  $"Логин: {subscriber.TaskTrackerLogin}\n" +
                                  $"В ближайшие 30 минут нужно будет выполнить привычку: " +
                                  $"{userHabit.HabitName ?? "не указана"} ({userHabit.Start} - {userHabit.End})\n";
                        DateTime outdated = new(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                            userHabit.End.Hour, userHabit.End.Minute, userHabit.End.Second);

                        messages.Add(new DomainMessage(Guid.NewGuid(), text, null, outdated,
                            false, userHabit.UserName, subscriber.Id));
                    }
                }
                Console.WriteLine($"{DateTime.Now}: Создано {messages.Count} сообщений");
                _messageRepo.TryCreateMessages(messages);
                await Task.Delay(TimeSpan.FromMinutes(timeout_generate), _cts.Token);
            }
            catch
            {
                await Task.Delay(TimeSpan.FromMinutes(timeout_err), _cts.Token);
            }
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient,
        Update update, CancellationToken cancellationToken)
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
                    _tempLogins[chatId] = text;
                    _registrationStates[chatId] = RegistrationState.AwaitingPassword;
                    await botClient.SendMessage(chatId, AskPasswordMessage);
                }
                else if (state == RegistrationState.AwaitingPassword)
                {
                    if (!await _taskTrackerClient.TryLogInAsync(_tempLogins[chatId], text))
                        await botClient.SendMessage(chatId, "Ошибка авторизации, попробуйте ввести пароль еще раз.\n\n");
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

    private async Task HandleStartCommand(ITelegramBotClient botClient, TelegramMessage message)
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

    private async Task HandleStopCommand(ITelegramBotClient botClient, TelegramMessage message)
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

            Console.WriteLine($"Пользователь отписался: {subscriber.Username}, " +
                $"Логин: {subscriber.TaskTrackerLogin}");
        }
        else
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "Вы не были подписаны"
            );
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient,
        Exception exception, CancellationToken cancellationToken)
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