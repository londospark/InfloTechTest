using System;
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

    private sealed class FakeUsersClient : IUsersClient
    {
        public Task<UserListDto> GetUsersAsync(System.Threading.CancellationToken cancellationToken = default) 
            => Task.FromResult(new UserListDto(Array.Empty<UserListItemDto>()));
        
        public Task<UserListDto> GetUsersByActiveAsync(bool isActive, System.Threading.CancellationToken cancellationToken = default) 
            => Task.FromResult(new UserListDto(Array.Empty<UserListItemDto>()));
        
        public Task<UserListItemDto> GetUserAsync(long id, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(new UserListItemDto(id, "Test", "User", "test@example.com", true, DateTime.UtcNow.Date));
        
        public Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) 
            => Task.FromResult(new UserListItemDto(1, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        
        public Task DeleteUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) 
            => Task.CompletedTask;
        
        public Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) 
            => Task.FromResult(new UserListItemDto(id, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        
        public Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, System.Threading.CancellationToken cancellationToken = default)
        {
            var logs = new System.Collections.Generic.List<UserLogDto>
            {
                new UserLogDto(1, userId, "Test log message", DateTime.UtcNow.AddMinutes(-5))
            };
            return Task.FromResult(new PagedResultDto<UserLogDto>(logs, page, pageSize, 1));
        }
    }

    private sealed class FakeUsersClientEmpty : IUsersClient
    {
        public Task<UserListDto> GetUsersAsync(System.Threading.CancellationToken cancellationToken = default) 
            => Task.FromResult(new UserListDto(Array.Empty<UserListItemDto>()));
        
        public Task<UserListDto> GetUsersByActiveAsync(bool isActive, System.Threading.CancellationToken cancellationToken = default) 
            => Task.FromResult(new UserListDto(Array.Empty<UserListItemDto>()));
        
        public Task<UserListItemDto> GetUserAsync(long id, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(new UserListItemDto(id, "Test", "User", "test@example.com", true, DateTime.UtcNow.Date));
        
        public Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) 
            => Task.FromResult(new UserListItemDto(1, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        
        public Task DeleteUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) 
            => Task.CompletedTask;
        
        public Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) 
            => Task.FromResult(new UserListItemDto(id, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        
        public Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResultDto<UserLogDto>(new System.Collections.Generic.List<UserLogDto>(), page, pageSize, 0));
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
}
