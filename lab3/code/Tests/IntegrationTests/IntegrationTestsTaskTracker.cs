using Allure.Xunit.Attributes;
using Domain;
using Domain.InPorts;
using Domain.Models;
using Domain.OutPorts;
using LoadAdapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Storage.PostgresStorageAdapters;
using System.ComponentModel;
using Types;

namespace Tests.IntegrationTests;

public class IntegrationTestsTaskTracker
{
    private static ServiceProvider Setup()
    {
        return new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddProvider(NullLoggerProvider.Instance);
                })
                .AddSingleton<IEventRepo, PostgresEventRepo>()
                .AddSingleton<IHabitRepo, PostgresHabitRepo>()
                .AddSingleton<IUserRepo, PostgresUserRepo>()
                .AddSingleton<ITaskTrackerContext, PostgresDBContext>()
                .AddDbContext<PostgresDBContext>(options =>
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"))
                .AddTransient<ISheduleLoad, ShedAdapter>()
                .AddTransient<ITaskTracker, TaskTracker>()
                .AddTransient<IHabitDistributor, HabitDistributor>()
                .BuildServiceProvider();
    }

    [Fact]
    [Category("Integration")]
    [AllureFeature("TaskTracker")]
    [AllureStory("Синхронные методы")]
    [AllureDescription("Тест создания пользователя с корректными данными")]
    public void CreateUserWithValidData()
    {
        using var serviceProvider = Setup();
        var taskTracker = serviceProvider.GetRequiredService<ITaskTracker>();
        var userName = "test";
        var phoneNumber = new PhoneNumber("+71111111111");
        var password = "123";

        var result = taskTracker.CreateUser(userName, phoneNumber, password);

        Assert.NotNull(result);
        Assert.Equal(userName, result.NameID);
        Assert.Equal(phoneNumber, result.Number);
        Assert.True(result.Settings.NotifyOn);
        Assert.Equal(userName, result.Settings.UserNameID);
    }
}