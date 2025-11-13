using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;
using UserManagement.Web.Controllers;
using UserManagement.Web.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Web.Tests;

public class DatabaseLoggerControllerIntegrationTests
{
    [Fact]
    public async Task List_DoesNotPersist_UserLogs()
    {
        // Use real SQLite in-memory DataContext
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var opts = new DbContextOptionsBuilder<Data.DataContext>()
            .UseSqlite(connection)
            .Options;
        await using var ctx = new Data.DataContext(opts);
        ctx.Database.EnsureCreated();

        // Seed a user that List will return
        var user = new User { Forename = "A", Surname = "B", Email = "a@b.com", IsActive = true, DateOfBirth = DateTime.UtcNow.AddYears(-30) };
        ctx.Users!.Add(user);
        ctx.SaveChanges();

        var userLogService = new Mock<Services.Interfaces.IUserLogService>();

        var factory = new FakeLoggerFactory();

        // create scope factory that resolves IUserLogService
        var services = new ServiceCollection();
        services.AddSingleton(userLogService.Object);
        var sp = services.BuildServiceProvider();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(sp);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var dbLogger = new DatabaseLogger<UsersController>(factory, scopeFactory.Object, null);

        var controller = new UsersController(new UserService(ctx), userLogService.Object, dbLogger);

        var result = await controller.List();

        // List should not create any persisted user logs
        userLogService.Verify(s => s.AddAsync(It.IsAny<UserLog>()), Times.Never);
    }

    [Fact]
    public async Task Create_Persists_UserLog_WithAssignedId()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var opts = new DbContextOptionsBuilder<Data.DataContext>()
            .UseSqlite(connection)
            .Options;
        await using var ctx = new Data.DataContext(opts);
        ctx.Database.EnsureCreated();

        var userLogService = new Mock<Services.Interfaces.IUserLogService>();

        var logs = new System.Collections.Generic.List<UserLog>();
        userLogService.Setup(s => s.AddAsync(It.IsAny<UserLog>())).Callback<UserLog>(logs.Add).ReturnsAsync((UserLog l) => l);

        var factory = new FakeLoggerFactory();

        var services = new ServiceCollection();
        services.AddSingleton(userLogService.Object);
        var sp = services.BuildServiceProvider();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(sp);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var dbLogger = new DatabaseLogger<UsersController>(factory, scopeFactory.Object, null);

        var controller = new UsersController(new UserService(ctx), userLogService.Object, dbLogger);

        var req = new Shared.DTOs.CreateUserRequestDto("X", "Y", "x@y.com", new DateTime(1990,1,1), true);

        var action = await controller.Create(req);

        // Expect at least one persisted log containing the created user's id
        logs.Should().NotBeEmpty();
        logs.Should().Contain(l => l.Message.Contains("Created user id") && l.UserId > 0);
    }

    [Fact]
    public async Task GetById_Persists_Logs_WhenUserExists()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var opts = new DbContextOptionsBuilder<Data.DataContext>()
            .UseSqlite(connection)
            .Options;
        await using var ctx = new Data.DataContext(opts);
        ctx.Database.EnsureCreated();

        var user = new User { Forename = "F", Surname = "S", Email = "f@s.com", IsActive = true, DateOfBirth = DateTime.UtcNow.AddYears(-20) };
        ctx.Users!.Add(user);
        ctx.SaveChanges();
        var id = user.Id;

        var userLogService = new Mock<Services.Interfaces.IUserLogService>();
        var logs = new System.Collections.Generic.List<UserLog>();
        userLogService.Setup(s => s.AddAsync(It.IsAny<UserLog>())).Callback<UserLog>(logs.Add).ReturnsAsync((UserLog l) => l);

        var factory = new FakeLoggerFactory();

        var services = new ServiceCollection();
        services.AddSingleton(userLogService.Object);
        var sp = services.BuildServiceProvider();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(sp);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var dbLogger = new DatabaseLogger<UsersController>(factory, scopeFactory.Object, null);

        var controller = new UsersController(new UserService(ctx), userLogService.Object, dbLogger);

        var result = await controller.GetById(id);

        logs.Should().Contain(l => l.UserId == id);
    }

    [Fact]
    public async Task Delete_Persists_Logs_WhenUserDeleted()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var opts = new DbContextOptionsBuilder<Data.DataContext>()
            .UseSqlite(connection)
            .Options;
        await using var ctx = new Data.DataContext(opts);
        ctx.Database.EnsureCreated();

        var user = new User { Forename = "D", Surname = "E", Email = "d@e.com", IsActive = true, DateOfBirth = DateTime.UtcNow.AddYears(-25) };
        ctx.Users!.Add(user);
        ctx.SaveChanges();
        var id = user.Id;

        var userLogService = new Mock<Services.Interfaces.IUserLogService>();
        var logs = new System.Collections.Generic.List<UserLog>();
        userLogService.Setup(s => s.AddAsync(It.IsAny<UserLog>())).Callback<UserLog>(logs.Add).ReturnsAsync((UserLog l) => l);

        var factory = new FakeLoggerFactory();

        var services = new ServiceCollection();
        services.AddSingleton(userLogService.Object);
        var sp = services.BuildServiceProvider();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(sp);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var dbLogger = new DatabaseLogger<UsersController>(factory, scopeFactory.Object, null);

        var controller = new UsersController(new UserService(ctx), userLogService.Object, dbLogger);

        var result = await controller.Delete(id);

        logs.Should().Contain(l => l.UserId == id);
    }

    private sealed class FakeLoggerFactory : ILoggerFactory
    {
        private readonly FakeInnerLogger _innerLogger = new();

        public ILogger CreateLogger(string categoryName) => _innerLogger;

        public void AddProvider(ILoggerProvider provider) { }

        public void Dispose() { }
    }

    private sealed class FakeInnerLogger : ILogger
    {
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // No-op: DatabaseLogger persists using formatter output
        }
    }
}
