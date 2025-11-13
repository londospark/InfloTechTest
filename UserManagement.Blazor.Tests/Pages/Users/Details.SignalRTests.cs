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

public class DetailsSignalRTests : BunitContext
{
    [Fact]
    public async Task Details_ReceivesLogPushes_AndUpdatesUi()
    {
        // Arrange
        var fake = new FakeUserLogsService();
        Services.AddSingleton<IUserLogsService>(fake);
        Services.AddScoped<IUsersClient>(_ => new FakeUsersClient());

        var cut = Render<Details>(ps => ps.Add(p => p.id, 1L));

        // Act: simulate a log push
        var dto = new UserLogDto(1, 1, "Hello from SignalR", DateTime.UtcNow);
        fake.Raise(dto);

        // Assert: UI contains message
        cut.Markup.Should().Contain("Hello from SignalR");
    }

    private sealed class FakeUserLogsService : IUserLogsService
    {
        public event Action<UserLogDto>? LogReceived;
        public Task StartAsync() => Task.CompletedTask;
        public Task StopAsync() => Task.CompletedTask;
        public Task JoinUserGroup(long userId) => Task.CompletedTask;
        public Task LeaveUserGroup(long userId) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
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
        public Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(new PagedResultDto<UserLogDto>(new System.Collections.Generic.List<UserLogDto>(), page, pageSize, 0));
    }
}
