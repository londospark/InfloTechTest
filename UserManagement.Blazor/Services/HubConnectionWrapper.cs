using Microsoft.AspNetCore.SignalR.Client;

namespace UserManagement.Blazor.Services;

public sealed class HubConnectionWrapper(HubConnection inner) : IUserLogsHubConnection
{
    public HubConnectionState State => inner.State;

    public Task StartAsync() => inner.StartAsync();

    public Task StopAsync() => inner.StopAsync();

    public Task InvokeAsync(string method, params object[] args) => inner.InvokeAsync(method, args);

    public void On<T>(string methodName, Action<T> handler) => inner.On(methodName, handler);

    public ValueTask DisposeAsync() => inner.DisposeAsync();

    async ValueTask IAsyncDisposable.DisposeAsync() => await inner.DisposeAsync();
}
