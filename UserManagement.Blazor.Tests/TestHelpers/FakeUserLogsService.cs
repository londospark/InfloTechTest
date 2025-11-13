using System;
using System.Threading.Tasks;
using UserManagement.Blazor.Services;
using UserManagement.Shared.DTOs;

namespace UserManagement.Blazor.Tests.TestHelpers;

/// <summary>
/// Base fake implementation of IUserLogsService for testing
/// </summary>
public class FakeUserLogsService : IUserLogsService, IDisposable
{
    public event Action<UserLogDto>? LogReceived;

    public virtual Task StartAsync() => Task.CompletedTask;
    public virtual Task StopAsync() => Task.CompletedTask;
    public virtual Task JoinUserGroup(long userId) => Task.CompletedTask;
    public virtual Task LeaveUserGroup(long userId) => Task.CompletedTask;
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public virtual void Dispose() { }

    /// <summary>
    /// Simulates receiving a log message via SignalR
    /// </summary>
    public void Raise(UserLogDto dto) => LogReceived?.Invoke(dto);
}

/// <summary>
/// Fake IUserLogsService that tracks lifecycle calls for testing
/// </summary>
public sealed class FakeUserLogsServiceWithTracking : FakeUserLogsService
{
    public bool Started { get; private set; }
    public bool Stopped { get; private set; }
    public long JoinedUser { get; private set; }
    public long LeftUser { get; private set; }

    public override Task StartAsync()
    {
        Started = true;
        return Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        Stopped = true;
        return Task.CompletedTask;
    }

    public override Task JoinUserGroup(long userId)
    {
        JoinedUser = userId;
        return Task.CompletedTask;
    }

    public override Task LeaveUserGroup(long userId)
    {
        LeftUser = userId;
        return Task.CompletedTask;
    }

    public void Reset()
    {
        Started = false;
        Stopped = false;
        JoinedUser = 0;
        LeftUser = 0;
    }
}
