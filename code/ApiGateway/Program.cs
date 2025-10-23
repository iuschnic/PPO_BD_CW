using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var secretConfiguration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.secrets.json", optional: false, reloadOnChange: true)
            .Build();
var secretKey = secretConfiguration.GetValue<string>("SecretKey");
if (secretKey == null)
{
    Console.WriteLine("Ошибка чтения конфигурации");
    return;
}    

builder.Services.AddOcelot(builder.Configuration)
    .AddDelegatingHandler<MicroserviceAuthHandler>();
builder.Services.AddSingleton(new MicroserviceAuthHandlerArgs
{
    SecretKey = secretKey
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
app.UseCors("AllowAll");
await app.UseOcelot();
app.Run();