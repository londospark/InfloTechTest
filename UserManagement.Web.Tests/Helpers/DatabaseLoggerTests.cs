using System;
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

namespace UserManagement.Web.Tests.Helpers
{
    public class DatabaseLoggerTests
    {
        private sealed class TestLogger : ILogger
        {
            public bool Logged { get; private set; }
            public LogLevel? LastLevel { get; private set; }
            public object? LastState { get; private set; }
            public Exception? LastException { get; private set; }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Logged = true;
                LastLevel = logLevel;
                LastState = state;
                LastException = exception;
            }
        }

        [Fact]
        public void NonPersistedLevel_ForwardsToForwardLogger_AndDoesNotCreateScope()
        {
            // Arrange
            var inner = new TestLogger();
            var innerFactory = new Mock<ILoggerFactory>();
            innerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(inner);

            var forward = new TestLogger();

            var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
            // Expect no calls to CreateScope for non-persisted level

            var logger = new DatabaseLogger<object>(innerFactory.Object, scopeFactory.Object, forward);

            // Act
            var state = new KeyValuePair<string, object?>[] { new("Some", "value") };
            logger.Log(LogLevel.Debug, new EventId(1), state, null, (s, e) => "debug-msg");

            // Assert
            forward.Logged.Should().BeTrue();
            inner.Logged.Should().BeFalse();
            scopeFactory.Verify(sf => sf.CreateScope(), Times.Never);
        }

        [Fact]
        public void PersistedLevelWithoutUserId_ForwardsInsteadOfPersisting()
        {
            // Arrange
            var inner = new TestLogger();
            var innerFactory = new Mock<ILoggerFactory>();
            innerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(inner);

            var forward = new TestLogger();

            var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);

            var logger = new DatabaseLogger<object>(innerFactory.Object, scopeFactory.Object, forward);

            // Act: Information is a persisted level but no user id present in state
            var state = new KeyValuePair<string, object?>[] { new("Other", 123) };
            logger.Log(LogLevel.Information, new EventId(2), state, null, (s, e) => "info-msg");

            // Assert: forwarded, not persisted
            forward.Logged.Should().BeTrue();
            inner.Logged.Should().BeFalse();
            scopeFactory.Verify(sf => sf.CreateScope(), Times.Never);
        }

        [Fact]
        public void PersistedLevelWithUserId_PersistsAndPublishesToHub()
        {
            // Arrange
            var inner = new TestLogger();
            var innerFactory = new Mock<ILoggerFactory>();
            innerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(inner);

            var scopeFactory = new Mock<IServiceScopeFactory>();

            // Mock scope and service provider
            var mockScope = new Mock<IServiceScope>();
            var mockProvider = new Mock<IServiceProvider>();
            mockScope.SetupGet(s => s.ServiceProvider).Returns(mockProvider.Object);
            scopeFactory.Setup(sf => sf.CreateScope()).Returns(mockScope.Object);

            // Capture UserLog passed to IUserLogService.AddAsync
            var userLogService = new Mock<IUserLogService>();
            UserLog? capturedLog = null;
            userLogService.Setup(s => s.AddAsync(It.IsAny<UserLog>()))
                .Callback<UserLog>(log => capturedLog = log)
                .Returns<UserLog>(log => Task.FromResult(log))
                .Verifiable();

            // Mock HubContext and client proxy
            var mockHubContext = new Mock<IHubContext<UserLogsHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            // SendCoreAsync is the actual method called, not the extension method SendAsync
            mockClientProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockHubContext.SetupGet(h => h.Clients).Returns(mockClients.Object);

            // Setup service provider to return IUserLogService and IHubContext<UserLogsHub>
            mockProvider.Setup(p => p.GetService(typeof(IUserLogService))).Returns(userLogService.Object);
            mockProvider.Setup(p => p.GetService(typeof(IHubContext<UserLogsHub>))).Returns(mockHubContext.Object);

            var logger = new DatabaseLogger<object>(innerFactory.Object, scopeFactory.Object, null);

            // Act: pass a state with UserId (long)
            var state = new KeyValuePair<string, object?>[] { new("UserId", 77L) };
            logger.Log(LogLevel.Information, new EventId(3), state, null, (s, e) => "persisted message");

            // Assert: inner logger was called and user log persisted
            inner.Logged.Should().BeTrue();
            userLogService.Verify(s => s.AddAsync(It.IsAny<UserLog>()), Times.Once);
            capturedLog.Should().NotBeNull();
            capturedLog!.UserId.Should().Be(77L);
            capturedLog.Message.Should().Contain("persisted message");

            // Hub publish should be invoked via SendCoreAsync
            mockClientProxy.Verify(p => p.SendCoreAsync("LogAdded", It.Is<object?[]>(o => o != null), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
