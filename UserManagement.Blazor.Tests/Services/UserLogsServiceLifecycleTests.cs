using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using UserManagement.Blazor.Services;
using UserManagement.Shared.DTOs;
using Xunit;

namespace UserManagement.Blazor.Tests.Services;

public class UserLogsServiceLifecycleTests
{
    private sealed class FakeConnection : IUserLogsHubConnection
    {
        public HubConnectionState State { get; set; } = HubConnectionState.Disconnected;

        public bool Started { get; private set; }
        public bool Stopped { get; private set; }
        public bool Disposed { get; private set; }

        public Task StartAsync()
        {
            Started = true;
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Stopped = true;
            State = HubConnectionState.Disconnected;
            return Task.CompletedTask;
        }

        public Task InvokeAsync(string method, params object[] args)
        {
            // record invocation or no-op
            return Task.CompletedTask;
        }

        public void On<T>(string methodName, Action<T> handler)
        {
            // store handler and invoke when needed
            _on = (o) => handler((T)o!);
        }

        private Action<object?>? _on;

        public void Raise(object? payload) => _on?.Invoke(payload);

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }

        async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();
    }

    [Fact]
    public async Task Lifecycle_StartsAndStopsAndJoinsLeavesAndDispose()
    {
        var conn = new FakeConnection();
        var svc = new UserLogsService(conn);

        await svc.StartAsync();
        conn.Started.Should().BeTrue();

        await svc.JoinUserGroup(5);
        await svc.LeaveUserGroup(5);

        await svc.StopAsync();
        conn.Stopped.Should().BeTrue();

        await svc.DisposeAsync();
        conn.Disposed.Should().BeTrue();
    }

    [Fact]
    public void LogReceived_HandlerInvokedWhenConnectionRaises()
    {
        var conn = new FakeConnection();
        var svc = new UserLogsService(conn);

        UserLogDto? received = null;
        svc.LogReceived += (l) => received = l;

        var dto = new UserLogDto(1, 1, "m", DateTime.UtcNow);
        conn.Raise(dto);

        received.Should().NotBeNull();
        received!.Message.Should().Be("m");
    }
}
