using System;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Blazor.Pages.Users;
using UserManagement.Blazor.Services;
using UserManagement.Shared.DTOs;
using Xunit;

namespace UserManagement.Blazor.Tests.Pages.Users;

public class DetailsLifecycleTests : BunitContext
{
    [Fact]
    public async Task Initialization_CallsStartAndJoin()
    {
        // Arrange
        var fake = new SpyUserLogsService();
        Services.AddSingleton<IUserLogsService>(fake);
        Services.AddScoped<IUsersClient>(_ => new FakeUsersClient());

        // Act
        var cut = Render<Details>(ps => ps.Add(p => p.id, 11L));
        // allow lifecycle to run
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        fake.StartCalled.Should().BeTrue();
        fake.JoinedUserId.Should().Be(11);
    }

    private sealed class SpyUserLogsService : IUserLogsService
    {
        public event Action<UserLogDto>? LogReceived;
        public bool StartCalled { get; private set; }
        public long? JoinedUserId { get; private set; }

        public Task StartAsync() { StartCalled = true; return Task.CompletedTask; }
        public Task StopAsync() => Task.CompletedTask;
        public Task JoinUserGroup(long userId) { JoinedUserId = userId; return Task.CompletedTask; }
        public Task LeaveUserGroup(long userId) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        // helper to raise events
        public void Raise(UserLogDto dto) => LogReceived?.Invoke(dto);
    }

    private sealed class FakeUsersClient : IUsersClient
    {
        public Task<UserListDto> GetUsersAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListDto> GetUsersByActiveAsync(bool isActive, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListDto([]));
        public Task<UserListItemDto> GetUserAsync(long id, System.Threading.CancellationToken cancellationToken = default)
        {
            var dto = new UserListItemDto(id, "Test", "User", "test@example.com", true, DateTime.UtcNow.Date);
            return Task.FromResult(dto);
        }
        public Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(1, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        public Task DeleteUserAsync(long id, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new UserListItemDto(id, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));
        public Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResultDto<UserLogDto>(new System.Collections.Generic.List<UserLogDto>(), page, pageSize, 0));
        }
    }
}
