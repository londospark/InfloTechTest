using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;
using UserManagement.Web.Controllers;
using UserManagement.Web.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Web.Tests;

public class DatabaseLoggerControllerIntegrationTests
{
    [Fact]
    public void List_DoesNotPersist_UserLogs()
    {
        var dataContext = new Mock<Data.IDataContext>();
        var users = new[] { new User { Id = 1, Forename = "A", Surname = "B", Email = "a@b.com", IsActive = true, DateOfBirth = DateTime.UtcNow.AddYears(-30) } }.AsQueryable();
        dataContext.Setup(dc => dc.GetAll<User>()).Returns(users);

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

        var controller = new UsersController(new UserService(dataContext.Object), userLogService.Object, dbLogger);

        var result = controller.List();

        // List should not create any persisted user logs
        userLogService.Verify(s => s.Add(It.IsAny<UserLog>()), Times.Never);
    }

    [Fact]
    public void Create_Persists_UserLog_WithAssignedId()
    {
        var dataContext = new Mock<Data.IDataContext>();
        var userLogService = new Mock<Services.Interfaces.IUserLogService>();

        // Arrange create to assign an id like the real DB would
        dataContext.Setup(dc => dc.Create(It.IsAny<User>())).Callback<User>(u => u.Id = 101);

        var factory = new FakeLoggerFactory();

        var services = new ServiceCollection();
        services.AddSingleton(userLogService.Object);
        var sp = services.BuildServiceProvider();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(sp);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var dbLogger = new DatabaseLogger<UsersController>(factory, scopeFactory.Object, null);

        var controller = new UsersController(new UserService(dataContext.Object), userLogService.Object, dbLogger);

        var req = new Shared.DTOs.CreateUserRequestDto("X", "Y", "x@y.com", new DateTime(1990,1,1), true);

        var action = controller.Create(req);

        // Expect at least one persisted log containing the created user's id
        userLogService.Verify(s => s.Add(It.Is<UserLog>(l => l.UserId == 101 && l.Message.Contains("Created user id"))), Times.AtLeastOnce);
    }

    [Fact]
    public void GetById_Persists_Logs_WhenUserExists()
    {
        var dataContext = new Mock<Data.IDataContext>();
        var userLogService = new Mock<Services.Interfaces.IUserLogService>();

        var users = new[] { new User { Id = 202, Forename = "F", Surname = "S", Email = "f@s.com", IsActive = true, DateOfBirth = DateTime.UtcNow.AddYears(-20) } }.AsQueryable();
        dataContext.Setup(dc => dc.GetAll<User>()).Returns(users);

        var factory = new FakeLoggerFactory();

        var services = new ServiceCollection();
        services.AddSingleton(userLogService.Object);
        var sp = services.BuildServiceProvider();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(sp);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var dbLogger = new DatabaseLogger<UsersController>(factory, scopeFactory.Object, null);

        var controller = new UsersController(new UserService(dataContext.Object), userLogService.Object, dbLogger);

        var result = controller.GetById(202);

        userLogService.Verify(s => s.Add(It.Is<UserLog>(l => l.UserId == 202)), Times.AtLeastOnce);
    }

    [Fact]
    public void Delete_Persists_Logs_WhenUserDeleted()
    {
        var dataContext = new Mock<Data.IDataContext>();
        var userLogService = new Mock<Services.Interfaces.IUserLogService>();

        var users = new[] { new User { Id = 303, Forename = "D", Surname = "E", Email = "d@e.com", IsActive = true, DateOfBirth = DateTime.UtcNow.AddYears(-25) } }.AsQueryable();
        dataContext.Setup(dc => dc.GetAll<User>()).Returns(users);
        dataContext.Setup(dc => dc.Delete(It.IsAny<User>())).Callback<User>(u => { /* deleted */ });

        var factory = new FakeLoggerFactory();

        var services = new ServiceCollection();
        services.AddSingleton(userLogService.Object);
        var sp = services.BuildServiceProvider();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(sp);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var dbLogger = new DatabaseLogger<UsersController>(factory, scopeFactory.Object, null);

        var controller = new UsersController(new UserService(dataContext.Object), userLogService.Object, dbLogger);

        var result = controller.Delete(303);

        userLogService.Verify(s => s.Add(It.Is<UserLog>(l => l.UserId == 303)), Times.AtLeastOnce);
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
