using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;
using UserManagement.Shared.DTOs;
using UserManagement.Web.Controllers;
using Microsoft.Data.Sqlite;

namespace UserManagement.Web.Tests;

public class UserControllerTests
{
    [Fact]
    public async Task List_WhenServiceReturnsUsers_ModelMustContainUsers()
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var users = SetupUsers(ctx);

        var result = await controller.List();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var dto = ok.Value.Should().BeOfType<UserListDto>().Which;
        // Ensure the returned collection includes the users we added
        dto.Items.Should().Contain(i => i.Email == users.First().Email);
    }

    [Fact]
    public async Task ListByActive_WhenFilteringActiveUsers_ModelMustContainOnlyActiveUsers()
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var users = SetupUsers(ctx, isActive: true);

        var result = await controller.ListByActive(isActive: true);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var dto = ok.Value.Should().BeOfType<UserListDto>().Which;
        // All returned items should be active and include our inserted user
        dto.Items.Should().NotBeEmpty();
        dto.Items.Should().OnlyContain(i => i.IsActive);
        dto.Items.Should().Contain(i => i.Email == users.First().Email);
    }

    [Fact]
    public async Task ListByActive_WhenFilteringInactiveUsers_ModelMustContainOnlyInactiveUsers()
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var users = SetupUsers(ctx, isActive: false);

        var result = await controller.ListByActive(isActive: false);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var dto = ok.Value.Should().BeOfType<UserListDto>().Which;
        dto.Items.Should().NotBeEmpty();
        dto.Items.Should().OnlyContain(i => !i.IsActive);
        dto.Items.Should().Contain(i => i.Email == users.First().Email);
    }

    [Fact]
    public async Task Create_WhenValidRequest_Returns201CreatedWithLocationAndBody()
    {
        var ctx = CreateContext();
        var logger = new MockLogger<UsersController>();
        var controller = CreateController(ctx, logger);
        var req = new CreateUserRequestDto(
            "Jane",
            "Doe",
            "jane.doe@example.com",
            new(1992, 5, 10),
            true
        );

        var action = await controller.Create(req);

        var created = action.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.ActionName.Should().Be(nameof(UsersController.GetById));
        var dto = created.Value.As<UserListItemDto>();
        dto.Forename.Should().Be("Jane");
        dto.Surname.Should().Be("Doe");
        dto.Email.Should().Be("jane.doe@example.com");
        dto.IsActive.Should().BeTrue();
        dto.DateOfBirth.Should().Be(new(1992, 5, 10));

        // Verify persisted in DB
        var persisted = ctx.Users!.FirstOrDefault(u => u.Email == req.Email);
        persisted.Should().NotBeNull();
        persisted!.Id.Should().BeGreaterThan(0);

        // And a log was written for creation
        logger.LogContains(LogLevel.Information, "Created user id").Should().BeTrue();
    }

    [Fact]
    public async Task Create_WhenInvalidRequest_ReturnsBadRequestAndDoesNotPersist()
    {
        var ctx = CreateContext();
        var logger = new MockLogger<UsersController>();
        var controller = CreateController(ctx, logger);
        var req = new CreateUserRequestDto(
            "",
            "Doe",
            "jane.doe@example.com",
            new(1992, 5, 10),
            true
        );

        var action = await controller.Create(req);

        var objectResult = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        objectResult.Value.Should().BeOfType<ValidationProblemDetails>();
        objectResult.StatusCode.Should().Be(400);

        ctx.Users!.Any(u => u.Email == req.Email).Should().BeFalse();
        logger.LogContains(LogLevel.Warning, "Create user validation failed").Should().BeTrue();
    }

    [Fact]
    public async Task Create_WhenDateOfBirthInFuture_ReturnsBadRequest()
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var future = DateTime.Now.AddDays(1);
        var req = new CreateUserRequestDto(
            "Jane",
            "Doe",
            "jane.doe@example.com",
            future,
            true
        );

        var action = await controller.Create(req);

        var objectResult = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        objectResult.Value.Should().BeOfType<ValidationProblemDetails>()
            .Which.Errors.Should().ContainKey(nameof(CreateUserRequestDto.DateOfBirth));

        ctx.Users!.Any(u => u.Email == req.Email).Should().BeFalse();
    }

    [Fact]
    public async Task GetById_WhenFound_Returns200WithUser()
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var users = SetupUsers(ctx);
        // Use the Id assigned by the database after SaveChanges
        ctx.SaveChanges();
        var assignedId = users.First().Id;

        var result = await controller.GetById(assignedId);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeOfType<UserListItemDto>()
            .Which.Id.Should().Be(assignedId);
    }

    [Fact]
    public async Task GetById_WhenMissing_Returns404()
    {
        var ctx = CreateContext();
        var logger = new MockLogger<UsersController>();
        var controller = CreateController(ctx, logger);
        SetupUsers(ctx);

        var result = await controller.GetById(999);

        result.Result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
        logger.LogContains(LogLevel.Warning, "User not found for id").Should().BeTrue();
    }

    [Fact]
    public async Task Delete_WhenFound_ReturnsNoContent()
    {
        var ctx = CreateContext();
        var logger = new MockLogger<UsersController>();
        var controller = CreateController(ctx, logger);
        var users = SetupUsers(ctx);
        // Persist users and use the real assigned id
        ctx.SaveChanges();
        var u = users.First();
        var id = u.Id;

        var result = await controller.Delete(id);

        result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
        ctx.Users!.Any(x => x.Id == id).Should().BeFalse();
        logger.LogContains(LogLevel.Information, "Deleting user id").Should().BeTrue();
        logger.LogContains(LogLevel.Information, "Deleted user id").Should().BeTrue();
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        SetupUsers(ctx);

        var result = await controller.Delete(999);

        result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
    }

    private User[] SetupUsers(DataContext ctx, string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true, DateTime? dateOfBirth = null)
    {
        dateOfBirth ??= new(1990, 1, 1);
        var users = new[]
        {
            new User
            {
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = isActive,
                DateOfBirth = dateOfBirth.Value
            }
        };

        ctx.Users!.AddRange(users);
        ctx.SaveChanges();
        return users;
    }

    private static DataContext CreateContext()
    {
        // Use SQLite in-memory to better emulate relational behavior
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var opts = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new DataContext(opts);
        // Ensure database created and seed data applied
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private UsersController CreateController(DataContext ctx, MockLogger<UsersController>? logger = null)
    {
        var userService = new UserService(ctx);
        var userLogService = new UserLogService(ctx);
        var log = logger?.AsILogger() ?? new NullLogger<UsersController>();
        return new UsersController(userService, userLogService, log);
    }

    // Lightweight test logger capturing messages
    private sealed class MockLogger<T>
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<(LogLevel, string)> _entries = new();
        public Microsoft.Extensions.Logging.ILogger<T> AsILogger() => new Adapter(this);
        public bool LogContains(LogLevel level, string contains) => _entries.Any(e => e.Item1 == level && e.Item2.Contains(contains));
        public void Add(LogLevel level, string message) => _entries.Add((level, message));

        private sealed class Adapter(MockLogger<T> parent)
            : Microsoft.Extensions.Logging.ILogger, Microsoft.Extensions.Logging.ILogger<T>
        {
            private readonly MockLogger<T> _parent = parent;
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                _parent.Add(logLevel, formatter(state, exception));
            }
            private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() {} }
        }
    }
}
