using MessageSenderDomain.OutPorts;
using MessageSenderStorage.EfAdapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

class Program
{
    static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IMessageRepo, EfMessageRepo>()
            .AddSingleton<ISubscriberRepo, EfSubscriberRepo>()
            .AddDbContext<MessageSenderDBContext>(options =>
                options.UseNpgsql("Host=localhost;Port=5432;Database=messagesenderdb;Username=postgres;Password=postgres"))
            .AddSingleton<ITaskTrackerClient, TaskTrackerClient>
            .BuildServiceProvider();
        var bot = new MessageSender(
            "7665679478:AAHtpesgjfWihplWtkBB7Iuwot-6gCElWVY",
            serviceProvider.GetRequiredService<IMessageRepo>(),
            serviceProvider.GetRequiredService<ISubscriberRepo>(),
            serviceProvider.GetRequiredService<ITaskTrackerClient>());
        Console.CancelKeyPress += async (sender, e) =>
        {
            await bot.StopAsync();
            Environment.Exit(0);
        };
        await bot.StartAsync();
    }
}
