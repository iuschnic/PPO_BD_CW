using MessageSenderDomain.Models;
using MessageSenderDomain.OutPorts;
using System.Net;
using System.Text;

using DomainMessage = MessageSenderDomain.Models.Message;

public class MessageSenderArgs(string baseUrl)
{
    public string BaseUrl { get; set; } = baseUrl;
}

public class MessageSender
{
    private readonly IBotClient _botClient;
    private readonly IMessageRepo _messageRepo;
    private readonly ISubscriberRepo _subscribersRepo;
    private readonly ISenderTaskTrackerClient _taskTrackerClient;
    private readonly CancellationTokenSource _cts = new();
    private readonly HttpListener _httpListener;
    private readonly MessageSenderArgs _args;

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

    public MessageSender(IBotClient botClient, IMessageRepo messageRepo,
        ISubscriberRepo subscriberRepo, ISenderTaskTrackerClient taskTrackerClient,
        MessageSenderArgs args)
    {
        _botClient = botClient;
        _messageRepo = messageRepo;
        _subscribersRepo = subscriberRepo;
        _taskTrackerClient = taskTrackerClient;
        _args = args;
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"{args.BaseUrl}/");
    }

    public async Task StartAsync()
    {
        await _botClient.StartReceivingAsync(HandleUpdateAsync, _cts.Token);

        _ = Task.Run(StartBroadcasting, _cts.Token);
        _ = Task.Run(StartCreatingMessages, _cts.Token);
        _ = Task.Run(StartHttpListener, _cts.Token);

        Console.WriteLine("The bot is on. Press Ctrl+C to stop it.");
        await Task.Delay(-1, _cts.Token);
    }

    private async Task StartHttpListener()
    {
        try
        {
            _httpListener.Start();
            Console.WriteLine($"HTTP Listener started on {_args.BaseUrl}");

            while (!_cts.IsCancellationRequested && _httpListener.IsListening)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    _ = Task.Run(() => ProcessHttpRequest(context), _cts.Token);
                }
                catch (HttpListenerException) when (_cts.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in HTTP listener: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start HTTP listener: {ex.Message}");
        }
        finally
        {
            _httpListener.Stop();
        }
    }

    private async Task ProcessHttpRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/delete_account")
            {
                await HandleDeleteAccountRequest(request, response);
            }
            else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/send_two_factor")
            {
                await HandleSendTwoFactorRequest(request, response);
            }
            else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/check_exists")
            {
                await HandleCheckExistsRequest(request, response);
            }
            else
            {
                response.StatusCode = 404;
                await SendResponse(response, "Not Found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing HTTP request: {ex.Message}");
            response.StatusCode = 500;
            await SendResponse(response, "Internal Server Error");
        }
        finally
        {
            response.Close();
        }
    }

    private async Task HandleDeleteAccountRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        string taskTrackerLogin = await GetRequestBody(request);

        if (string.IsNullOrEmpty(taskTrackerLogin))
        {
            response.StatusCode = 400;
            await SendResponse(response, "Task tracker login is required");
            return;
        }

        Console.WriteLine($"Обработка запроса DELETE /delete_account для пользователя: {taskTrackerLogin}");
        await HandleDeleteAccount(taskTrackerLogin);
        response.StatusCode = 200;
        await SendResponse(response, "");
    }

    private async Task HandleCheckExistsRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        string taskTrackerLogin = request.QueryString["login"];

        if (string.IsNullOrEmpty(taskTrackerLogin))
        {
            response.StatusCode = 400;
            await SendResponse(response, "Query parameter 'login' is required");
            return;
        }

        Console.WriteLine($"Обработка запроса GET /check_exists для пользователя: {taskTrackerLogin}");
        bool exists = _subscribersRepo.IfAnyTaskTrackerLogin(taskTrackerLogin);
        response.StatusCode = 200;
        await SendResponse(response, exists.ToString().ToLower());
    }

    private async Task HandleSendTwoFactorRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        string requestBody = await GetRequestBody(request);

        if (string.IsNullOrEmpty(requestBody))
        {
            response.StatusCode = 400;
            await SendResponse(response, "Request body is required");
            return;
        }

        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);

            if (data == null || !data.ContainsKey("login") || !data.ContainsKey("message"))
            {
                response.StatusCode = 400;
                await SendResponse(response, "JSON with 'login' and 'message' fields is required");
                return;
            }

            string taskTrackerLogin = data["login"];
            string message = "Ваш код двухфакторной аутентификации: " + data["message"];

            Console.WriteLine($"Обработка запроса POST /send_two_factor для пользователя: {taskTrackerLogin}");

            var subscriber = _subscribersRepo.TryGetByTaskTrackerLogin(taskTrackerLogin);
            if (subscriber != null)
            {
                await _botClient.SendMessageAsync(
                    chatId: subscriber.Id,
                    text: message
                );
                response.StatusCode = 200;
                await SendResponse(response, "Message sent");
                Console.WriteLine($"Two-factor message sent to user {taskTrackerLogin}: {message}");
            }
            else
            {
                response.StatusCode = 404;
                await SendResponse(response, "User not found");
                Console.WriteLine($"User with login {taskTrackerLogin} not found for two-factor message");
            }
        }
        catch (System.Text.Json.JsonException)
        {
            response.StatusCode = 400;
            await SendResponse(response, "Invalid JSON format");
        }
    }

    private async Task<string> GetRequestBody(HttpListenerRequest request)
    {
        if (request.HasEntityBody)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            return await reader.ReadToEndAsync();
        }
        return string.Empty;
    }

    private async Task HandleDeleteAccount(string taskTrackerLogin)
    {
        try
        {
            var subscriber = _subscribersRepo.TryGetByTaskTrackerLogin(taskTrackerLogin);
            if (subscriber != null)
            {
                if (!_subscribersRepo.TryRemoveByChatID(subscriber.Id))
                {
                    Console.WriteLine($"Ошибка при удалении пользователя: {taskTrackerLogin}");
                    return;
                }

                await _botClient.SendMessageAsync(
                    chatId: subscriber.Id,
                    text: GoodbyeMessage
                );

                Console.WriteLine($"User unsubscribed via HTTP: {subscriber.Username}, " +
                    $"Логин: {subscriber.TaskTrackerLogin}");
            }
            else
            {
                Console.WriteLine($"Пользователь с логином {taskTrackerLogin} не найден для удаления");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleDeleteAccount for {taskTrackerLogin}: {ex.Message}");
        }
    }

    private async Task SendResponse(HttpListenerResponse response, string message)
    {
        try
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending HTTP response: {ex.Message}");
        }
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
                    await _botClient.SendMessageAsync(
                        chatId: send.SubscriberID ?? 0,
                        text: send.Text
                    );
                    cnt++;
                    sentMessages.Add(new DomainMessage(send.Id, send.Text, send.TimeSent,
                        send.TimeOutdated, send.WasSent, send.TaskTrackerLogin, send.SubscriberID));
                }
                _messageRepo.MarkMessagesSent(sentMessages);
                Console.WriteLine($"{DateTime.Now}: Sent {cnt} messages");
                await Task.Delay(TimeSpan.FromMinutes(timeout_send), _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting: {ex.Message}");
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
                Console.WriteLine($"{DateTime.Now}: Generated {messages.Count} messages");
                _messageRepo.TryCreateMessages(messages);
                await Task.Delay(TimeSpan.FromMinutes(timeout_generate), _cts.Token);
            }
            catch
            {
                await Task.Delay(TimeSpan.FromMinutes(timeout_err), _cts.Token);
            }
        }
    }

    private async Task HandleUpdateAsync(IBotUpdate update)
    {
        try
        {
            var chatId = update.ChatId;
            var text = update.Text;

            if (text == "/start")
            {
                await HandleStartCommand(update);
            }
            else if (text == "/stop")
            {
                await HandleStopCommand(update);
            }
            else if (text != null && _registrationStates.TryGetValue(chatId, out var state))
            {
                if (state == RegistrationState.AwaitingLogin)
                {
                    _tempLogins[chatId] = text;
                    _registrationStates[chatId] = RegistrationState.AwaitingPassword;
                    await _botClient.SendMessageAsync(chatId, AskPasswordMessage);
                }
                else if (state == RegistrationState.AwaitingPassword)
                {
                    if (!await _taskTrackerClient.TryLogInAsync(_tempLogins[chatId], text))
                        await _botClient.SendMessageAsync(chatId, "Ошибка авторизации, попробуйте ввести пароль еще раз.\n\n");
                    else
                    {
                        var subscriber = new Subscriber(chatId, _tempLogins[chatId], text,
                            update.Username, DateTime.Now);
                        if (!_subscribersRepo.TryAdd(subscriber))
                            throw new Exception("Ошибка, пользователь существует");

                        _registrationStates.Remove(chatId);
                        _tempLogins.Remove(chatId);

                        Console.WriteLine($"Пользователь с chatId {chatId} подписался: {subscriber.Username}, Логин: {subscriber.TaskTrackerLogin}");

                        await _botClient.SendMessageAsync(chatId, RegistrationCompleteMessage);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error serving message: {ex.Message}");
        }
    }

    private async Task HandleStartCommand(IBotUpdate message)
    {
        var chatId = message.ChatId;
        if (_subscribersRepo.IfAnyChatID(chatId))
        {
            await _botClient.SendMessageAsync(
                chatId: chatId,
                text: "Вы уже подписаны на рассылку"
            );
            return;
        }

        _registrationStates[chatId] = RegistrationState.AwaitingLogin;
        await _botClient.SendMessageAsync(chatId, WelcomeMessage);
    }

    private async Task HandleStopCommand(IBotUpdate message)
    {
        var chatId = message.ChatId;
        var subscriber = _subscribersRepo.TryGetByChatID(chatId);
        if (subscriber != null)
        {
            var ans = await _taskTrackerClient.TryUnableTwoFactor(subscriber.TaskTrackerLogin);

            if (!_subscribersRepo.TryRemoveByChatID(chatId))
                throw new Exception("Ошибка, пользователь не существует");

            await _botClient.SendMessageAsync(
                chatId: chatId,
                text: GoodbyeMessage
            );

            Console.WriteLine($"User unsubscribed: {subscriber.Username}, " +
                $"Логин: {subscriber.TaskTrackerLogin}");
        }
        else
        {
            await _botClient.SendMessageAsync(
                chatId: chatId,
                text: "Вы не были подписаны"
            );
        }
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        _httpListener?.Stop();
        Console.WriteLine("The bot is offline.");
    }
}