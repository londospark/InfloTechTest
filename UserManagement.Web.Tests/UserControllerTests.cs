using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;
using UserManagement.Shared.DTOs;
using UserManagement.Web.Controllers;

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

    [Theory]
    [InlineData("", "Doe", "jane.doe@example.com", true, "Forename")]
    [InlineData("Jane", "", "jane.doe@example.com", true, "Surname")]
    [InlineData("Jane", "Doe", "", true, "Email")]
    public async Task Create_WithMissingFields_ReturnsBadRequest(string forename, string surname, string email, bool isActive, string expectedErrorField)
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var req = new CreateUserRequestDto(forename, surname, email, new(1992, 5, 10), isActive);
        var action = await controller.Create(req);
        var objectResult = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        objectResult.Value.Should().BeOfType<ValidationProblemDetails>()
            .Which.Errors.Should().ContainKey(expectedErrorField);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsBadRequest()
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var req = new CreateUserRequestDto("Jane", "Doe", "dup@example.com", new(1992, 5, 10), true);
        await controller.Create(req);
        var req2 = new CreateUserRequestDto("John", "Smith", "dup@example.com", new(1990, 1, 1), true);
        var action = await controller.Create(req2);
        var objectResult = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        objectResult.Value.Should().BeOfType<ValidationProblemDetails>()
            .Which.Errors.Should().ContainKey(nameof(CreateUserRequestDto.Email));
    }

    [Theory]
    [InlineData("A", "Doe", "a@b.com", true)]
    [InlineData("JaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJane", "Doe", "a@b.com", true)]
    public async Task Create_WithMinMaxFieldLengths_Succeeds(string forename, string surname, string email, bool isActive)
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var req = new CreateUserRequestDto(forename, surname, email, new(1992, 5, 10), isActive);
        var action = await controller.Create(req);
        var created = action.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task List_WhenNoUsers_ReturnsEmptyList()
    {
        var ctx = CreateContext();
        // Remove all users to ensure empty DB
        ctx.Users!.RemoveRange(ctx.Users!);
        ctx.SaveChanges();
        var controller = CreateController(ctx);
        var result = await controller.List();
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<UserListDto>().Which;
        dto.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_WhenValidRequest_UpdatesUser()
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var users = SetupUsers(ctx);
        var user = users.First();
        var req = new CreateUserRequestDto("Updated", "User", user.Email, user.DateOfBirth, false);
        var result = await controller.Update(user.Id, req);
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<UserListItemDto>().Which;
        dto.Forename.Should().Be("Updated");
        dto.IsActive.Should().BeFalse();
        ctx.Users!.Find(user.Id)!.Forename.Should().Be("Updated");
    }

    [Fact]
    public async Task Update_WhenUserNotFound_ReturnsNotFound()
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var req = new CreateUserRequestDto("Updated", "User", "notfound@example.com", new(1990, 1, 1), true);
        var result = await controller.Update(999, req);
        result.Result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Update_WhenInvalidRequest_ReturnsBadRequest()
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var users = SetupUsers(ctx);
        var user = users.First();
        var req = new CreateUserRequestDto("", "User", user.Email, user.DateOfBirth, true);
        var result = await controller.Update(user.Id, req);
        var objectResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        objectResult.Value.Should().BeOfType<ValidationProblemDetails>()
            .Which.Errors.Should().ContainKey(nameof(CreateUserRequestDto.Forename));
    }

    [Fact]
    public async Task Delete_WithRelatedLogs_DeletesUserAndLogs()
    {
        var ctx = CreateContext();
        var controller = CreateController(ctx);
        var users = SetupUsers(ctx);
        var user = users.First();
        // Add a log for the user
        ctx.UserLogs!.Add(new UserLog { UserId = user.Id, Message = "Test log", CreatedAt = DateTime.UtcNow });
        ctx.SaveChanges();
        var result = await controller.Delete(user.Id);
        result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
        ctx.Users!.Any(x => x.Id == user.Id).Should().BeFalse();
        // Logs should remain for audit trail
        ctx.UserLogs!.Any(l => l.UserId == user.Id).Should().BeTrue();
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
        // Use SQLite in-memory with a unique name per test to ensure isolation
        var dbName = $"DataSource=file:memdb_{Guid.NewGuid()}?mode=memory&cache=shared";
        var connection = new SqliteConnection(dbName);
        connection.Open();

        var opts = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new DataContext(opts);
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
        private readonly System.Collections.Concurrent.ConcurrentBag<(LogLevel, string)> _entries = [];
        public ILogger<T> AsILogger() => new Adapter(this);
        public bool LogContains(LogLevel level, string contains) => _entries.Any(e => e.Item1 == level && e.Item2.Contains(contains));
        public void Add(LogLevel level, string message) => _entries.Add((level, message));

        private sealed class Adapter(MockLogger<T> parent)
            : ILogger, ILogger<T>
        {
            private readonly MockLogger<T> _parent = parent;
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => _parent.Add(logLevel, formatter(state, exception));
            private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
        }
    }
}
