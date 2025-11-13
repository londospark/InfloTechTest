using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;
using UserManagement.Web.Helpers;
using UserManagement.Web.Hubs;

namespace UserManagement.Web.Tests;

public class DatabaseLoggerSignalRTests
{
    [Fact]
    public void PersistedLog_IsForwardedToHubGroup()
    {
        // Arrange
        var userLogService = new Mock<IUserLogService>();
        userLogService.Setup(s => s.AddAsync(It.IsAny<UserLog>()))
            .Callback<UserLog>(l => l.Id = 99)
            .ReturnsAsync((UserLog l) => l);

        var clientProxy = new Mock<IClientProxy>();
        clientProxy.Setup(p => p.SendCoreAsync(
            It.Is<string>(m => m == "LogAdded"),
            It.Is<object?[]>(args => args != null && args.Length == 1),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.Is<string>(g => g == "user-42"))).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<UserLogsHub>>();
        hubContext.SetupGet(h => h.Clients).Returns(clients.Object);

        // Build service provider that returns IUserLogService and IHubContext
        var services = new ServiceCollection();
        services.AddSingleton(userLogService.Object);
        services.AddSingleton(hubContext.Object);
        var sp = services.BuildServiceProvider();

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(sp);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var innerLogger = new Mock<ILogger>();
        innerLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(innerLogger.Object);

        var dbLogger = new DatabaseLogger<Web.Controllers.UsersController>(loggerFactory.Object, scopeFactory.Object, null);

        // Act: log under a user scope so it persists
        using (dbLogger.BeginScope(new Dictionary<string, object?> { ["UserId"] = 42L }))
        {
            dbLogger.LogInformation("Test persist and push");
        }

        // Assert
        userLogService.Verify(s => s.AddAsync(It.Is<UserLog>(l => l.UserId == 42 && l.Message.Contains("Test persist and push"))), Times.Once);
        clientProxy.Verify();
    }
}
