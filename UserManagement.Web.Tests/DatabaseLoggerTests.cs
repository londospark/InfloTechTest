using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;
using UserManagement.Web.Controllers;
using UserManagement.Web.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Web.Tests;

public class DatabaseLoggerTests
{
    [Fact]
    public void LogInformation_WithUserScope_PersistsLogToDatabase()
    {
        var logger = CreateLogger(out var userLogService, out var forwardLogger);
        var logs = CaptureLogs(userLogService);

        using (logger.BeginScope(new Dictionary<string, object?> { ["UserId"] = 42L }))
        {
            logger.LogInformation("User {UserId} retrieved", 42);
        }

        logs.Should().ContainSingle();
        logs[0].UserId.Should().Be(42);
        logs[0].Message.Should().Contain("User 42 retrieved");
        logs[0].CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Ensure forward logger did not receive this persisted entry
        forwardLogger.Verify(f => f.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    [Fact]
    public void LogDebug_ForwardsToForwardLogger()
    {
        var logger = CreateLogger(out var userLogService, out var forwardLogger);

        using (logger.BeginScope(new Dictionary<string, object?> { ["UserId"] = 99L }))
        {
            logger.LogDebug("Debug entry {Value}", 1);
        }

        // Debug is not persisted but should be forwarded
        forwardLogger.Verify(f => f.Log(It.Is<LogLevel>(l => l == LogLevel.Debug), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        userLogService.Verify(s => s.Add(It.IsAny<UserLog>()), Times.Never);
    }

    [Fact]
    public void LogInformation_WithoutScope_UsesUserIdFromStateWhenAvailable()
    {
        var logger = CreateLogger(out var userLogService, out var forwardLogger);
        var logs = CaptureLogs(userLogService);

        logger.LogInformation("Processed user {UserId}", 123);

        logs.Should().ContainSingle();
        logs[0].UserId.Should().Be(123);
    }

    [Fact]
    public void LogError_WithException_AppendsExceptionMessage()
    {
        var logger = CreateLogger(out var userLogService, out var forwardLogger);
        var logs = CaptureLogs(userLogService);
        var exception = new InvalidOperationException("boom");

        logger.LogError(exception, "Failed to update user {UserId}", 5);

        logs.Should().ContainSingle();
        logs[0].Message.Should().Contain("boom");
    }

    [Fact]
    public void NestedScopes_RestorePreviousUserId()
    {
        var logger = CreateLogger(out var userLogService, out var forwardLogger);
        var logs = CaptureLogs(userLogService);

        using (logger.BeginScope(new Dictionary<string, object?> { ["UserId"] = 1L }))
        {
            logger.LogInformation("Outer scope");

            using (logger.BeginScope(new Dictionary<string, object?> { ["UserId"] = 2L }))
            {
                logger.LogWarning("Inner scope");
            }

            logger.LogInformation("Outer scope again");
        }

        logs.Should().HaveCount(3);
        logs[0].UserId.Should().Be(1);
        logs[1].UserId.Should().Be(2);
        logs[2].UserId.Should().Be(1);
    }

    private static List<UserLog> CaptureLogs(Mock<IUserLogService> userLogService)
    {
        var logs = new List<UserLog>();
        userLogService.Setup(s => s.Add(It.IsAny<UserLog>())).Callback<UserLog>(logs.Add);
        return logs;
    }

    private static DatabaseLogger<UsersController> CreateLogger(out Mock<IUserLogService> userLogService, out Mock<ILogger> forwardLogger)
    {
        userLogService = new Mock<IUserLogService>();
        forwardLogger = new Mock<ILogger>();
        var factory = new FakeLoggerFactory();

        // Build a small ServiceProvider that contains the IUserLogService so DatabaseLogger can resolve it from scope
        var services = new ServiceCollection();
        services.AddSingleton(userLogService.Object);
        var sp = services.BuildServiceProvider();

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(sp);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        return new DatabaseLogger<UsersController>(factory, scopeFactory.Object, forwardLogger.Object);
    }

    private sealed class FakeLoggerFactory : ILoggerFactory
    {
        private readonly FakeInnerLogger _innerLogger = new();

        public ILogger CreateLogger(string categoryName) => _innerLogger;

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeInnerLogger : ILogger
    {
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // No-op: DatabaseLogger uses the formatter to persist messages.
        }
    }
}

