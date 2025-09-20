using CliWrap;
using CliWrap.Buffered;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Storage.EfAdapters;

namespace Tests.E2ETests;

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

    [Fact]
    public async Task TestApp()
    {
        /*
         Последовательность команд для E2E тестирования консольного интерфейса:
         1 - зарегистрировать аккаунт
         kulik - логин нового аккаунта
         +71111111111 - номер телефона
         password - пароль
         База данных чиста -> аккаунт создается
         2 - войти в аккаунт
         kulik - логин
         password - пароль
         Аккаунт был создан -> успешный вход
         8 - выйти из аккаунта
         3 - выйти из приложения
         */
        var inputCommands = """
        1
        kulik
        +71111111111
        password
        2
        kulik
        password
        8
        3
        """;
        var command = Cli.Wrap("dotnet")
            .WithArguments($"run --project \"{_csprojPath}\"")
            .WithEnvironmentVariables(env => env
                .Set("DB_CONNECTION_STRING", _connString)
            )
            .WithWorkingDirectory(_projectDirectory)
            .WithStandardInputPipe(PipeSource.FromString(inputCommands));
        var result = await command.ExecuteBufferedAsync();
        Assert.Equal(0, result.ExitCode);
    }
}