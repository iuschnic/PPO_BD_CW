using Allure.Xunit.Attributes;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Storage.EfAdapters;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Tests.E2ETests;

public class ResponseAnalyzer(Dictionary<string, string> requestResponse)
{
    private readonly Dictionary<string, string> _requestResponse = requestResponse;
    public bool CheckResponse(string request, string response)
    {
        if (response.Contains(_requestResponse[request]))
            return true;
        return false;
    }
}

public class TaskTrackerE2ETests : IAsyncLifetime
{
    private readonly string _csprojPath;
    private readonly string _projectDirectory;
    private readonly string? _connString;
    private readonly EfDbContext _dbContext;

    public async Task InitializeAsync()
    {
        await CleanDatabaseAsync();
    }
    public async Task DisposeAsync()
    {
        await CleanDatabaseAsync();
        await _dbContext.DisposeAsync();
    }
    private async Task CleanDatabaseAsync()
    {
        if (_dbContext == null) return;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        await _dbContext.SettingsTimes.ExecuteDeleteAsync();
        await _dbContext.USettings.ExecuteDeleteAsync();
        await _dbContext.Events.ExecuteDeleteAsync();
        await _dbContext.ActualTimes.ExecuteDeleteAsync();
        await _dbContext.PrefFixedTimes.ExecuteDeleteAsync();
        await _dbContext.Habits.ExecuteDeleteAsync();
        await _dbContext.UserMessages.ExecuteDeleteAsync();
        await _dbContext.Messages.ExecuteDeleteAsync();
        await _dbContext.Users.ExecuteDeleteAsync();
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
    }

    public TaskTrackerE2ETests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("tests_settings.json", optional: false, reloadOnChange: true)
            .Build();
        if ((_connString = configuration.GetConnectionString("E2ETestsConnection")) == null)
            throw new InvalidDataException("Не найдена строка подключения к тестовой базе данных");
        var serviceProvider = new ServiceCollection()
                .AddDbContext<EfDbContext>(options =>
                    options.UseNpgsql(_connString))
                .BuildServiceProvider();
        _dbContext = serviceProvider.GetRequiredService<EfDbContext>();
        var solutionDirectory = Directory.GetCurrentDirectory();
        while (solutionDirectory != null && Directory.GetFiles(solutionDirectory, "*.sln").Length == 0)
            solutionDirectory = Directory.GetParent(solutionDirectory)?.FullName;
        if (solutionDirectory == null)
            throw new InvalidOperationException("Solution directory not found");
        _projectDirectory = Path.Combine(solutionDirectory, "CLI");
        _csprojPath = Path.Combine(_projectDirectory, "CLI.csproj");
        if (!File.Exists(_csprojPath))
            throw new FileNotFoundException($"Project file not found: {_csprojPath}");
    }
    /*
    [Fact]
    [Trait("Category", "E2E")]
    [AllureFeature("TaskTracker")]
    [AllureStory("E2E тестирование")]
    [AllureDescription("Тест создания аккаунта и входа в него")]
    public async Task TestApp2()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var commandResponse = new List<Tuple<string, string>>
        {
            new("1", "Введите имя пользователя"),                     // зарегистрировать аккаунт
            new("kulik", "Введите номер телефона"),                   // логин нового аккаунта
            new("+71111111111", "Введите пароль"),                    // номер телефона
            new("password", "USER"),                                  // пароль
            new("2", "Введите имя пользователя"),                       // войти в аккаунт
            new("kulik", "Введите пароль"),                             // логин
            new("password", "USER"),                                    // пароль
            new("2", "Введите название привычки"),                      // добавить привычку
            new("name", "сколько минут нужно тратить на привычку"),     // имя привычки
            new("30", "тип привычки"),                                  // время на привычку
            new("0", "сколько дней в неделю нужно выполнять привычку"), // безразличное время
            new("6", "Привычка была успешно добавлена"),                // дней в неделю
            new("8", "Создать аккаунт"),                                // выйти из аккаунта
            new("3", "")                                                // выйти из приложения
        };
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{_csprojPath}\" -- --non-interactive",
                WorkingDirectory = _projectDirectory,
                EnvironmentVariables =
                {
                    ["DB_CONNECTION_STRING"] = _connString
                },
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8
            }
        };
        var outputBuilder = new StringBuilder();
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Даем приложению время на запуск
        await Task.Delay(10000);

        try
        {
            await using (var writer = process.StandardInput)
            {
                await WaitForResponse(outputBuilder, timeoutMs: 5000);
                outputBuilder.Clear();
                foreach (var pair in commandResponse)
                {
                    await writer.WriteLineAsync(pair.Item1);
                    await writer.FlushAsync();
                    Console.WriteLine($">>> Sent command: {pair.Item1}");
                    var response = await WaitForResponse(outputBuilder, timeoutMs: 5000);
                    Console.WriteLine($"<<< Response: {response}");
                    outputBuilder.Clear();
                    
                    Console.WriteLine($"pair: {pair.Item2}");
                    //Assert.Contains(response, pair.Item2);
                    Console.WriteLine("assertok");
                    await Task.Delay(3000);
                }
            }

            var completed = process.WaitForExit(5000);

            if (!completed)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("Process did not exit within 5 seconds after commands");
            }

            Assert.Equal(0, process.ExitCode);
        }
        finally
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
    }

    // Метод для ожидания ответа после команды
    private async Task<string> WaitForResponse(StringBuilder outputBuilder, int timeoutMs)
    {
        var startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
        {
            await Task.Delay(100);
            // Если появился новый вывод - возвращаем его
            if (outputBuilder.Length > 0)
                return outputBuilder.ToString().Trim();
        }
        throw new TimeoutException($"No response received within {timeoutMs}ms");
    }*/

    /*[Fact]
    [Trait("Category", "E2E")]
    [AllureFeature("TaskTracker")]
    [AllureStory("E2E тестирование")]
    [AllureDescription("Тест создания аккаунта и входа в него")]
    public async Task TestApp2_WithCliWrap()
    {
        Console.OutputEncoding = Encoding.UTF8;

        // Создаем последовательность ввода для приложения
        var inputCommands = new[]
        {
            "1\n",           // Создать аккаунт
            "1\n",           // Создать аккаунт
            "1\n",           // Создать аккаунт
            "1\n",           // Создать аккаунт
            "1\n",           // Создать аккаунт
            "kulik\n",       // Имя пользователя
            "+71111111111\n", // Номер телефона
            "password",    // Пароль
            "2",           // Войти в аккаунт (или добавить привычку - уточните логику)
            "kulik",       // Логин
            "password",    // Пароль
            "2",           // Добавить привычку
            "name",        // Название привычки
            "30",          // Минуты
            "0",           // Тип привычки
            "6",           // Дней в неделю
            "8",           // Выйти из аккаунта
            "3"            // Выйти из приложения
        };

        // Создаем входной поток с командами
        var inputStream = new MemoryStream();
        var inputWriter = new StreamWriter(inputStream, Encoding.UTF8);

        foreach (var command in inputCommands)
        {
            await inputWriter.WriteLineAsync(command);
        }
        await inputWriter.FlushAsync();
        inputStream.Position = 0;

        try
        {
            StringBuilder errorBuilder = new();
            StringBuilder outputBuilder = new();
            var command = Cli.Wrap("dotnet")
                .WithArguments(["run", "--project", _csprojPath, "--", "--non-interactive"])
                .WithWorkingDirectory(_projectDirectory)
                .WithEnvironmentVariables(new Dictionary<string, string?>
                {
                    ["DB_CONNECTION_STRING"] = _connString
                })
                .WithStandardInputPipe(PipeSource.FromStream(inputStream))
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(outputBuilder, Encoding.UTF8))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(errorBuilder, Encoding.UTF8));
                //.WithValidation(CommandResultValidation.None); // Отключаем проверку кода выхода

            Console.WriteLine("Starting application...");

            // Выполняем команду с таймаутом
            var result = await command.ExecuteBufferedAsync(Encoding.UTF8, new CancellationToken(true));

            Console.WriteLine("=== APPLICATION OUTPUT ===");
            Console.WriteLine(outputBuilder.ToString());

            if (errorBuilder.Length > 0)
            {
                Console.WriteLine("=== APPLICATION ERRORS ===");
                Console.WriteLine(errorBuilder.ToString());
            }

            Console.WriteLine($"=== EXIT CODE: {result.ExitCode} ===");

            // Проверяем ожидаемые результаты в выводе
            var output = outputBuilder.ToString();

            // Проверяем ключевые точки выполнения
            Assert.Contains("Введите имя пользователя", output);
            Assert.Contains("Введите номер телефона", output);
            Assert.Contains("Введите пароль", output);
            Assert.Contains("USER", output);
            Assert.Contains("Введите название привычки", output);
            Assert.Contains("Привычка была успешно добавлена", output);

            // Проверяем код выхода (0 - успешное завершение)
            Assert.Equal(0, result.ExitCode);
        }
        finally
        {
            await inputStream.DisposeAsync();
        }
    }*/


    [Fact]
    [Trait("Category", "E2E")]
    public async Task AddHabitE2ETest()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var process = new Process();
        var globalCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        try
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{_csprojPath}\" -- --non-interactive",
                WorkingDirectory = _projectDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            process.StartInfo.EnvironmentVariables["DB_CONNECTION_STRING"] = _connString;
            var output = new StringBuilder();
            var outputCompleted = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    outputCompleted.TrySetResult(true);
                else
                {
                    output.AppendLine(e.Data);
                    Console.WriteLine($"[APP] {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            Console.WriteLine("Waiting for application to start...");
            await Task.Delay(10000, globalCts.Token);
            /*
            1               Опция создания аккаунта
            kulik           Ввести имя пользователя
            +71111111111    Ввести номер телефона
            password        Ввести пароль
            2               Опция входа в аккаунт
            kulik           Ввести логин
            password        Ввести пароль
            2               Опция добавления привычки
            name            Ввести название привычки
            30              Ввести количество минут на привычку
            0               Ввести тип привычки
            6               Ввести сколько дней в неделю надо выполнять
            8               Выйти из аккаунта
            3               Выйти из приложения
            */
            var commands = new[]
            {
                new { Command = "1", Timeout = 10000, Expected = "Введите имя пользователя" },
                new { Command = "kulik", Timeout = 10000, Expected = "Введите номер телефона" },
                new { Command = "+71111111111", Timeout = 10000, Expected = "Введите пароль" },
                new { Command = "password", Timeout = 10000, Expected = "USER" },
                new { Command = "2", Timeout = 10000, Expected = "Введите имя пользователя" },
                new { Command = "kulik", Timeout = 10000, Expected = "Введите пароль" },
                new { Command = "password", Timeout = 10000, Expected = "USER" },
                new { Command = "2", Timeout = 10000, Expected = "Введите название привычки" },
                new { Command = "name", Timeout = 10000, Expected = "сколько минут нужно тратить на привычку" },
                new { Command = "30", Timeout = 10000, Expected = "тип привычки" },
                new { Command = "0", Timeout = 10000, Expected = "сколько дней в неделю нужно выполнять привычку" },
                new { Command = "6", Timeout = 10000, Expected = "Привычка была успешно добавлена" },
                new { Command = "8", Timeout = 10000, Expected = "Создать аккаунт" },
                new { Command = "3", Timeout = 10000, Expected = "" }
            };

            foreach (var step in commands)
            {
                var stepCts = CancellationTokenSource.CreateLinkedTokenSource(globalCts.Token);
                stepCts.CancelAfter(step.Timeout);
                await process.StandardInput.WriteLineAsync(step.Command);
                await process.StandardInput.FlushAsync();
                Assert.True(await WaitForOutput(output, step.Expected, stepCts.Token,
                    TimeSpan.FromMilliseconds(step.Timeout)));
            }
            process.StandardInput.Close();
            await process.WaitForExitAsync(globalCts.Token);
            await outputCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), globalCts.Token);
            Assert.Equal(0, process.ExitCode);
        }
        finally
        {
            await KillProcessAndChildrenSafeAsync(process);
            globalCts.Dispose();
        }
    }
    private async Task<bool> WaitForOutput(StringBuilder output, string expected,
        CancellationToken cancellationToken, TimeSpan timeOut)
    {
        if (string.IsNullOrEmpty(expected)) return true;

        var start = DateTime.Now;
        while (DateTime.Now - start < timeOut)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (output.ToString().Contains(expected))
                return true;
            await Task.Delay(500, cancellationToken);
        }
        return false;
    }
    private static async Task KillProcessAndChildrenSafeAsync(Process process)
    {
        try
        {
            if (process == null) return;

            if (!process.HasExited)
            {
                Console.WriteLine("Force killing process tree...");
                process.Kill(entireProcessTree: true);
                for (int i = 0; i < 10; i++)
                {
                    if (process.HasExited) break;
                    await Task.Delay(100);
                }
            }
            process.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during process cleanup: {ex.Message}");
        }
    }
}
