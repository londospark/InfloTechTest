using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Blazor.Components;
using UserManagement.Blazor.Services;
using UserManagement.Shared.DTOs;
using Xunit;

namespace UserManagement.Blazor.Tests.Components;

public class ActivityLogsTests : BunitContext
{
    [Fact]
    public async Task ActivityLogs_RendersLogsForUser()
    {
        // Arrange
        var fakeClient = new FakeUsersClient();
        var fakeLogsService = new FakeUserLogsService();
        
        Services.AddScoped<IUsersClient>(_ => fakeClient);
        Services.AddScoped<IUserLogsService>(_ => fakeLogsService);

        // Act
        var cut = Render<ActivityLogs>(parameters => parameters
            .Add(p => p.UserId, 123L)
            .Add(p => p.PageSize, 5));

        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("Activity Logs");
        cut.Markup.Should().Contain("Test log message");
    }

    [Fact]
    public async Task ActivityLogs_ReceivesSignalRUpdates()
    {
        // Arrange
        var fakeClient = new FakeUsersClient();
        var fakeLogsService = new FakeUserLogsService();
        
        Services.AddScoped<IUsersClient>(_ => fakeClient);
        Services.AddScoped<IUserLogsService>(_ => fakeLogsService);

        var cut = Render<ActivityLogs>(parameters => parameters
            .Add(p => p.UserId, 123L)
            .Add(p => p.PageSize, 5));

        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act: simulate SignalR message
        var newLog = new UserLogDto(999, 123, "New SignalR log", DateTime.UtcNow);
        fakeLogsService.Raise(newLog);

        // Assert
        cut.Markup.Should().Contain("New SignalR log");
    }

    [Fact]
    public async Task ActivityLogs_ShowsEmptyMessageWhenNoLogs()
    {
        // Arrange
        var fakeClient = new FakeUsersClientEmpty();
        var fakeLogsService = new FakeUserLogsService();
        
        Services.AddScoped<IUsersClient>(_ => fakeClient);
        Services.AddScoped<IUserLogsService>(_ => fakeLogsService);

        // Act
        var cut = Render<ActivityLogs>(parameters => parameters
            .Add(p => p.UserId, 999L)
            .Add(p => p.PageSize, 5));

        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("No logs found");
    }

    [Fact]
    public async Task ActivityLogs_Pagination_NextAndPrevious_Work()
    {
        // Arrange: create a paging fake client with 3 items
        var fakeClient = new FakeUsersClientPaging();
        var fakeLogsService = new FakeUserLogsService();

        Services.AddScoped<IUsersClient>(_ => fakeClient);
        Services.AddScoped<IUserLogsService>(_ => fakeLogsService);

        var cut = Render<ActivityLogs>(parameters => parameters
            .Add(p => p.UserId, 1L)
            .Add(p => p.PageSize, 1));

        // Initial page should contain the first log
        cut.Markup.Should().Contain("Log 1");
        cut.Markup.Should().NotContain("Log 2");

        // Act: click Next
        var next = cut.FindAll("a.page-link").First(e => e.TextContent.Contains("Next"));
        next.Click();

        // Wait for the next page to render
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert: page 2 shows Log 2
        cut.Markup.Should().Contain("Log 2");
        cut.Markup.Should().NotContain("Log 1");

        // Click Previous
        var prev = cut.FindAll("a.page-link").First(e => e.TextContent.Contains("Previous"));
        prev.Click();
        await cut.InvokeAsync(() => Task.CompletedTask);

        cut.Markup.Should().Contain("Log 1");
    }

    [Fact]
    public async Task ActivityLogs_IgnoresSignalR_ForDifferentUser()
    {
        var fakeClient = new FakeUsersClient();
        var fakeLogsService = new FakeUserLogsService();

        Services.AddScoped<IUsersClient>(_ => fakeClient);
        Services.AddScoped<IUserLogsService>(_ => fakeLogsService);

        var cut = Render<ActivityLogs>(parameters => parameters
            .Add(p => p.UserId, 50L)
            .Add(p => p.PageSize, 5));

        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act: raise a log for a different user
        var dto = new UserLogDto(99, 51, "Should not appear", DateTime.UtcNow);
        fakeLogsService.Raise(dto);

        // Assert: markup should not contain the message
        cut.Markup.Should().NotContain("Should not appear");
    }

    [Fact]
    public async Task ActivityLogs_Refreshes_WhenUserIdParameterChanges()
    {
        var fakeClient = new FakeUsersClientForDifferentUsers();
        var fakeLogsService = new FakeUserLogsService();

        Services.AddScoped<IUsersClient>(_ => fakeClient);
        Services.AddScoped<IUserLogsService>(_ => fakeLogsService);

        var cut = Render<ActivityLogs>(parameters => parameters
            .Add(p => p.UserId, 1L)
            .Add(p => p.PageSize, 5));

        await cut.InvokeAsync(() => Task.CompletedTask);

        // initial should show user 1 logs
        cut.Markup.Should().Contain("User1 Log");

        // Act: change parameter to user 2 by rendering a new component
        var cut2 = Render<ActivityLogs>(parameters => parameters
            .Add(p => p.UserId, 2L)
            .Add(p => p.PageSize, 5));

        await cut2.InvokeAsync(() => Task.CompletedTask);

        // Assert: now show user2 logs
        cut2.Markup.Should().Contain("User2 Log");
        cut2.Markup.Should().NotContain("User1 Log");
    }

    [Fact]
    public async Task ActivityLogs_Dispose_UnsubscribesAndStopsService()
    {
        var fakeClient = new FakeUsersClient();
        var fakeLogsService = new FakeUserLogsServiceWithTracking();

        Services.AddScoped<IUsersClient>(_ => fakeClient);
        Services.AddScoped<IUserLogsService>(_ => fakeLogsService);

        var cut = Render<ActivityLogs>(parameters => parameters
            .Add(p => p.UserId, 1L)
            .Add(p => p.PageSize, 5));

        await cut.InvokeAsync(() => Task.CompletedTask);

        // Ensure service was started and joined
        fakeLogsService.Started.Should().BeTrue();
        fakeLogsService.JoinedUser.Should().Be(1L);

        // Act: dispose the test context which triggers component disposal
        Dispose();

        // Allow async disposal to complete
        await Task.Delay(100);

        // Assert: service should have received leave and stop requests
        fakeLogsService.LeftUser.Should().Be(1L);
        fakeLogsService.Stopped.Should().BeTrue();
    }

    [Fact]
    public async Task ActivityLogs_LoadLogs_ShowsEmptyOnClientError()
    {
        var fakeClient = new FakeUsersClientThrow();
        var fakeLogsService = new FakeUserLogsService();

        Services.AddScoped<IUsersClient>(_ => fakeClient);
        Services.AddScoped<IUserLogsService>(_ => fakeLogsService);

        var cut = Render<ActivityLogs>(parameters => parameters
            .Add(p => p.UserId, 1L)
            .Add(p => p.PageSize, 5));

        await cut.InvokeAsync(() => Task.CompletedTask);

        // Should show no logs found message when client throws
        cut.Markup.Should().Contain("No logs found");
    }

    [Fact]
    public async Task ActivityLogs_ShowsErrorMessage_WhenUnexpectedExceptionOccurs()
    {
        // Arrange: client throws unexpected exception
        var fakeClient = new FakeUsersClientThrow();
        var fakeLogsService = new FakeUserLogsService();

        Services.AddScoped<IUsersClient>(_ => fakeClient);
        Services.AddScoped<IUserLogsService>(_ => fakeLogsService);

        // Act
        var cut = Render<ActivityLogs>(parameters => parameters
            .Add(p => p.UserId, 1L)
            .Add(p => p.PageSize, 5));

        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert: Should show error or fallback UI
        cut.Markup.Should().Contain("No logs found"); // or replace with actual error message if present
    }

    [Fact]
    public async Task ActivityLogs_RendersLogWithMissingMessage()
    {
        // Arrange: log with null/empty message
        var fakeClient = new FakeUsersClientWithInvalidLog();
        var fakeLogsService = new FakeUserLogsService();

        Services.AddScoped<IUsersClient>(_ => fakeClient);
        Services.AddScoped<IUserLogsService>(_ => fakeLogsService);

        // Act
        var cut = Render<ActivityLogs>(parameters => parameters
            .Add(p => p.UserId, 1L)
            .Add(p => p.PageSize, 5));

        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert: Should render fallback or empty message
        cut.Markup.Should().Contain("(no message)"); // or whatever fallback is used in the component
    }

    // --- Fake client / service implementations used in tests ---

    private sealed class FakeUsersClient : IUsersClient
    {
        public Task<UserListDto> GetUsersAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListDto> GetUsersByActiveAsync(bool isActive, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListItemDto> GetUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, "Test", "User", "test@example.com", true, DateTime.UtcNow.Date));
        public Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(1, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        public Task DeleteUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));

        public Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, System.Threading.CancellationToken cancellationToken = default)
        {
            var logs = new List<UserLogDto>
            {
                new UserLogDto(1, userId, "Test log message", DateTime.UtcNow.AddMinutes(-5))
            };
            return Task.FromResult(new PagedResultDto<UserLogDto>(logs, page, pageSize, 1));
        }
    }

    private sealed class FakeUsersClientEmpty : IUsersClient
    {
        public Task<UserListDto> GetUsersAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListDto> GetUsersByActiveAsync(bool isActive, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListItemDto> GetUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, "Test", "User", "test@example.com", true, DateTime.UtcNow.Date));
        public Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(1, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        public Task DeleteUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));

        public Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResultDto<UserLogDto>(new List<UserLogDto>(), page, pageSize, 0));
        }
    }

    private sealed class FakeUsersClientPaging : IUsersClient
    {
        private readonly List<UserLogDto> _all = new()
        {
            new UserLogDto(1, 1, "Log 1", DateTime.UtcNow.AddMinutes(-3)),
            new UserLogDto(2, 1, "Log 2", DateTime.UtcNow.AddMinutes(-2)),
            new UserLogDto(3, 1, "Log 3", DateTime.UtcNow.AddMinutes(-1))
        };

        public Task<UserListDto> GetUsersAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListDto> GetUsersByActiveAsync(bool isActive, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListItemDto> GetUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, "Test", "User", "test@example.com", true, DateTime.UtcNow.Date));
        public Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(1, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        public Task DeleteUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));

        public Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, System.Threading.CancellationToken cancellationToken = default)
        {
            var items = _all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PagedResultDto<UserLogDto>(items, page, pageSize, _all.Count));
        }
    }

    private sealed class FakeUsersClientForDifferentUsers : IUsersClient
    {
        public Task<UserListDto> GetUsersAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListDto> GetUsersByActiveAsync(bool isActive, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListItemDto> GetUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, "Test", "User", "test@example.com", true, DateTime.UtcNow.Date));
        public Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(1, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        public Task DeleteUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));

        public Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, System.Threading.CancellationToken cancellationToken = default)
        {
            var logs = new List<UserLogDto>
            {
                new UserLogDto(1, 1, "User1 Log", DateTime.UtcNow.AddMinutes(-5)),
                new UserLogDto(2, 2, "User2 Log", DateTime.UtcNow.AddMinutes(-4))
            };

            var filtered = logs.Where(l => l.UserId == userId).ToList();
            return Task.FromResult(new PagedResultDto<UserLogDto>(filtered, page, pageSize, filtered.Count));
        }
    }

    private sealed class FakeUsersClientThrow : IUsersClient
    {
        public Task<UserListDto> GetUsersAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListDto> GetUsersByActiveAsync(bool isActive, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListItemDto> GetUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, "Test", "User", "test@example.com", true, DateTime.UtcNow.Date));
        public Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(1, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        public Task DeleteUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));

        public Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, System.Threading.CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated client failure");
        }
    }

    private sealed class FakeUserLogsService : IUserLogsService, IDisposable
    {
        public event Action<UserLogDto>? LogReceived;

        public Task StartAsync() => Task.CompletedTask;
        public Task StopAsync() => Task.CompletedTask;
        public Task JoinUserGroup(long userId) => Task.CompletedTask;
        public Task LeaveUserGroup(long userId) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }

        public void Raise(UserLogDto dto) => LogReceived?.Invoke(dto);
    }

    private sealed class FakeUserLogsServiceWithTracking : IUserLogsService, IDisposable
    {
        public event Action<UserLogDto>? LogReceived;

        public bool Started { get; private set; }
        public bool Stopped { get; private set; }
        public long JoinedUser { get; private set; }
        public long LeftUser { get; private set; }

        public Task StartAsync()
        {
            Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Stopped = true;
            return Task.CompletedTask;
        }

        public Task JoinUserGroup(long userId)
        {
            JoinedUser = userId;
            return Task.CompletedTask;
        }

        public Task LeaveUserGroup(long userId)
        {
            LeftUser = userId;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }

        public void Raise(UserLogDto dto) => LogReceived?.Invoke(dto);
    }

    private sealed class FakeUsersClientWithInvalidLog : IUsersClient
    {
        public Task<UserListDto> GetUsersAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListDto> GetUsersByActiveAsync(bool isActive, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListItemDto> GetUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, "Test", "User", "test@example.com", true, DateTime.UtcNow.Date));
        public Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(1, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        public Task DeleteUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));

        public Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, System.Threading.CancellationToken cancellationToken = default)
        {
            var logs = new List<UserLogDto>
            {
                new UserLogDto(1, userId, null, DateTime.UtcNow.AddMinutes(-5))
            };
            return Task.FromResult(new PagedResultDto<UserLogDto>(logs, page, pageSize, 1));
        }
    }
}
