using Domain;
using Domain.InPorts;
using Domain.OutPorts;
using Storage.EfAdapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Serilog;
using LoadAdapters;

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
//builder.Services.AddScoped<IPasswordHasher<Employee>, PasswordHasher<Employee>>();
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    /*var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);*/

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