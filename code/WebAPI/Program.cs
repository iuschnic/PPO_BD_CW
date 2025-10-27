using Domain;
using Domain.InPorts;
using Domain.OutPorts;
using LoadAdapters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Serilog;
using Storage.EfAdapters;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

var threadPoolSettings = configuration.GetSection("ThreadPoolSettings");
ThreadPool.SetMinThreads(
    threadPoolSettings.GetValue<int>("MinWorkerThreads"),
    threadPoolSettings.GetValue<int>("MinCompletionPortThreads"));
ThreadPool.SetMaxThreads(
    threadPoolSettings.GetValue<int>("MaxWorkerThreads"),
    threadPoolSettings.GetValue<int>("MaxCompletionPortThreads"));

Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Error)
            .CreateLogger();

builder.Services.AddSingleton<IConfiguration>(configuration);
builder.Services.AddScoped<IEventRepo, EfEventRepo>();
builder.Services.AddScoped<IHabitRepo, EfHabitRepo>();
builder.Services.AddScoped<IUserRepo, EfUserRepo>();
builder.Services.AddScoped<ITaskTrackerContext, EfDbContext>();
builder.Services.AddDbContext<EfDbContext>(options =>
                    options.UseNpgsql(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                          ?? configuration.GetConnectionString("PostgresConnection")));
builder.Services.AddScoped<ISheduleLoad, ShedAdapter>();
builder.Services.AddScoped<ITaskTracker, TaskTracker>();
builder.Services.AddScoped<IHabitDistributor, HabitDistributor>();
builder.Services.AddScoped<IMessageSenderProvider, MessageSenderProvider>();
builder.Services.AddLogging(loggingBuilder =>
                 {
                     loggingBuilder.AddSerilog();
                 });
bool isBenchmark;
if (Environment.GetEnvironmentVariable("ENABLE_BENCHMARK") is string envVar &&
    bool.TryParse(envVar, out isBenchmark)) { }
else
{
    isBenchmark = configuration.GetValue<bool>("Enable_benchmark");
}
if (isBenchmark)
    builder.Services.AddTransient<IConfigureOptions<MvcOptions>, BenchmarkFormattersOptions>();
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskTracker API",
        Version = "v1",
        Description = "Асинхронный API для работы с привычками"
    });
    c.UseInlineDefinitionsForEnums();
    c.SchemaFilter<EnumSchemaFilter>();
    c.EnableAnnotations();
    c.OperationFilter<AsyncOperationFilter>();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting();
app.MapControllers();
app.UseCors("AllowAll");

app.Run();
public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();

            foreach (int value in Enum.GetValues(context.Type))
            {
                schema.Enum.Add(new OpenApiInteger(value));
            }

            schema.Type = "integer";
            schema.Format = "int32";

            schema.Description = "Допустимые значения: " +
                string.Join(", ", Enum.GetNames(context.Type)
                    .Select(name => $"{(int)Enum.Parse(context.Type, name)} - {name}"));
        }
    }
}

public class AsyncOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.ReturnType.IsGenericType &&
            context.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            operation.Summary += " (Асинхронный метод)";
        }
    }
}

public class BenchmarkInputFormatter : TextInputFormatter
{
    private readonly ILogger<BenchmarkInputFormatter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public BenchmarkInputFormatter(ILogger<BenchmarkInputFormatter> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        SupportedMediaTypes.Add("application/json");
        SupportedEncodings.Add(Encoding.UTF8);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context, Encoding encoding)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var reader = new StreamReader(context.HttpContext.Request.Body, encoding);
            var jsonString = await reader.ReadToEndAsync();

            var result = JsonSerializer.Deserialize(jsonString, context.ModelType, _jsonOptions);

            stopwatch.Stop();

            _logger.LogInformation(
                "[BENCHMARK_DESERIALIZE] Type: {ModelType}, TimeMs: {TimeMs}, SizeBytes: {Size}",
                context.ModelType.Name,
                stopwatch.ElapsedMilliseconds,
                encoding.GetByteCount(jsonString));

            return InputFormatterResult.Success(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Deserialization failed after {TimeMs}ms", stopwatch.ElapsedMilliseconds);
            return InputFormatterResult.Failure();
        }
    }
}

public class BenchmarkOutputFormatter : TextOutputFormatter
{
    private readonly ILogger<BenchmarkOutputFormatter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public BenchmarkOutputFormatter(ILogger<BenchmarkOutputFormatter> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        SupportedMediaTypes.Add("application/json");
        SupportedEncodings.Add(Encoding.UTF8);
    }

    public override async Task WriteResponseBodyAsync(
        OutputFormatterWriteContext context, Encoding encoding)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var jsonString = JsonSerializer.Serialize(context.Object, context.ObjectType, _jsonOptions);

            stopwatch.Stop();

            _logger.LogInformation(
                "[BENCHMARK_SERIALIZE] Type: {ObjectType}, TimeMs: {TimeMs}, SizeBytes: {Size}",
                context.ObjectType.Name,
                stopwatch.ElapsedMilliseconds,
                encoding.GetByteCount(jsonString));

            await context.HttpContext.Response.WriteAsync(jsonString, encoding);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Serialization failed after {TimeMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

// Создаем класс для конфигурации
public class BenchmarkFormattersOptions : IConfigureOptions<MvcOptions>
{
    private readonly ILogger<BenchmarkInputFormatter> _inputLogger;
    private readonly ILogger<BenchmarkOutputFormatter> _outputLogger;

    public BenchmarkFormattersOptions(
        ILogger<BenchmarkInputFormatter> inputLogger,
        ILogger<BenchmarkOutputFormatter> outputLogger)
    {
        _inputLogger = inputLogger;
        _outputLogger = outputLogger;
    }

    public void Configure(MvcOptions options)
    {
        options.InputFormatters.Insert(0, new BenchmarkInputFormatter(_inputLogger));
        options.OutputFormatters.Insert(0, new BenchmarkOutputFormatter(_outputLogger));
    }
}