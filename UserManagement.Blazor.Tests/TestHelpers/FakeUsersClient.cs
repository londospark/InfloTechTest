using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Blazor.Services;
using UserManagement.Shared.DTOs;

namespace UserManagement.Blazor.Tests.TestHelpers;

/// <summary>
/// Base fake implementation of IUsersClient for testing
/// </summary>
public class FakeUsersClient : IUsersClient
{
    public virtual Task<UserListDto> GetUsersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new UserListDto([]));

    public virtual Task<UserListDto> GetUsersByActiveAsync(bool isActive, CancellationToken cancellationToken = default) =>
        Task.FromResult(new UserListDto([]));

    public virtual Task<UserListItemDto> GetUserAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(new UserListItemDto(id, "Test", "User", "test@example.com", true, DateTime.UtcNow.Date));

    public virtual Task<UserListItemDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new UserListItemDto(1, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));

    public virtual Task DeleteUserAsync(long id, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public virtual Task<UserListItemDto> UpdateUserAsync(long id, CreateUserRequestDto request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new UserListItemDto(id, request.Forename, request.Surname, request.Email, request.IsActive, request.DateOfBirth));

    public virtual Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResultDto<UserLogDto>(
            new List<UserLogDto> { new(1, userId, "Test log message", DateTime.UtcNow.AddMinutes(-5)) },
            page,
            pageSize,
            1));
}

/// <summary>
/// Fake IUsersClient that returns empty results
/// </summary>
public sealed class FakeUsersClientEmpty : FakeUsersClient
{
    public override Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResultDto<UserLogDto>([], page, pageSize, 0));
}

/// <summary>
/// Fake IUsersClient that supports pagination testing
/// </summary>
public sealed class FakeUsersClientPaging(List<UserLogDto>? logs = null) : FakeUsersClient
{
    private readonly List<UserLogDto> _allLogs = logs ??
        [
            new(1, 1, "Log 1", DateTime.UtcNow.AddMinutes(-3)),
            new(2, 1, "Log 2", DateTime.UtcNow.AddMinutes(-2)),
            new(3, 1, "Log 3", DateTime.UtcNow.AddMinutes(-1))
        ];

    public override Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var items = _allLogs
            .Where(l => l.UserId == userId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PagedResultDto<UserLogDto>(items, page, pageSize, _allLogs.Count(l => l.UserId == userId)));
    }
}

/// <summary>
/// Fake IUsersClient that returns different logs for different users
/// </summary>
public sealed class FakeUsersClientForDifferentUsers : FakeUsersClient
{
    private readonly Dictionary<long, List<UserLogDto>> _logsByUser;

    public FakeUsersClientForDifferentUsers() => _logsByUser = new Dictionary<long, List<UserLogDto>>
    {
        [1] = [new UserLogDto(1, 1, "User1 Log", DateTime.UtcNow.AddMinutes(-5))],
        [2] = [new UserLogDto(2, 2, "User2 Log", DateTime.UtcNow.AddMinutes(-4))]
    };

    public override Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var logs = _logsByUser.GetValueOrDefault(userId, []);
        var items = logs.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResultDto<UserLogDto>(items, page, pageSize, logs.Count));
    }
}

/// <summary>
/// Fake IUsersClient that throws exceptions
/// </summary>
public sealed class FakeUsersClientThrow : FakeUsersClient
{
    public override Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Simulated client failure");
}

/// <summary>
/// Fake IUsersClient that returns logs with invalid/missing data
/// </summary>
public sealed class FakeUsersClientWithInvalidLog : FakeUsersClient
{
    public override Task<PagedResultDto<UserLogDto>> GetUserLogsAsync(long userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var logs = new List<UserLogDto>
        {
            new UserLogDto(1, userId, null, DateTime.UtcNow.AddMinutes(-5))
        };
        return Task.FromResult(new PagedResultDto<UserLogDto>(logs, page, pageSize, 1));
    }
}

/// <summary>
/// Stub HTTP message handler for testing HTTP clients
/// </summary>
public sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(handler(request));
}

/// <summary>
/// Helper methods for creating test HTTP responses
/// </summary>
public static class HttpTestHelpers
{
    public static HttpResponseMessage CreateJsonResponse<T>(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(data)
        };
        return response;
    }

    public static HttpResponseMessage CreateEmptyResponse(HttpStatusCode statusCode = HttpStatusCode.NoContent) => new HttpResponseMessage(statusCode);

    public static HttpResponseMessage CreateNullResponse(HttpStatusCode statusCode = HttpStatusCode.OK) => new HttpResponseMessage(statusCode)
    {
        Content = new StringContent("null")
    };
}
