using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Storage.EfAdapters;
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

        if ((_connString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                          ?? configuration.GetConnectionString("E2ETestsConnection")) == null)
            throw new InvalidDataException("Не найдена строка подключения к тестовой базе данных");
        Console.WriteLine("CONN: " + _connString);
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
        Console.WriteLine("Dir found");
    }
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
                    Debug.WriteLine($"[APP] {e.Data}");
                }
            };
            Console.WriteLine("Starting process...");
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
                Console.WriteLine("command: " + step.Command);
                await process.StandardInput.WriteLineAsync(step.Command);
                await process.StandardInput.FlushAsync();
                Assert.True(await WaitForOutput(output, step.Expected, stepCts.Token,
                    TimeSpan.FromMilliseconds(step.Timeout)));
            }
            Console.WriteLine("everything ok");
            process.StandardInput.Close();
            process.CancelOutputRead();
            process.CancelErrorRead();
            if (!process.HasExited)
            {
                Console.WriteLine("Waiting for process exit...");
                if (process.WaitForExit(10000)) // 30 секунд в миллисекундах
                {
                    Console.WriteLine($"Process exited with code: {process.ExitCode}");
                }
                else
                {
                    Console.WriteLine("Process did not exit within timeout, but main test logic completed");
                    await KillProcessAndChildrenSafeAsync(process);
                    return;
                }
            }
            else
            {
                Console.WriteLine($"Process already exited with code: {process.ExitCode}");
            }

            // Проверяем ExitCode только если процесс завершился
            if (process.HasExited && process.ExitCode != 0)
            {
                Console.WriteLine($"Non-zero exit code: {process.ExitCode}, but main test passed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TEST FAILED: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            Console.WriteLine("everything ok3");
            await KillProcessAndChildrenSafeAsync(process);
            globalCts.Dispose();
            Console.WriteLine("everything ok4");
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
