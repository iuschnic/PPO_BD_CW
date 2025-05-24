using System;
using System.Windows.Forms;
using Domain.InPorts;
using Domain.OutPorts;
using LoadAdapters;
using Microsoft.Extensions.DependencyInjection;
using Storage.PostgresStorageAdapters;
using Microsoft.EntityFrameworkCore;
using Domain;

namespace HabitTrackerGUI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var serviceProvider = new ServiceCollection()
                .AddSingleton<IEventRepo, PostgresEventRepo>()
                .AddSingleton<IHabitRepo, PostgresHabitRepo>()
                .AddSingleton<IUserRepo, PostgresUserRepo>()
                .AddDbContext<PostgresDBContext>(options =>
                    options.UseNpgsql("Host=localhost;Port=5432;Database=habits_db;Username=postgres;Password=postgres"))
                .AddTransient<ISheduleLoad, DummyShedAdapter>()
                .AddTransient<ITaskTracker, TaskTracker>()
                .AddTransient<IHabitDistributor, HabitDistributor>()
                .BuildServiceProvider();

            var taskService = serviceProvider.GetRequiredService<ITaskTracker>();
            Application.Run(new MainForm(taskService));
        }
    }
}