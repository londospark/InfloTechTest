using Microsoft.AspNetCore.SignalR.Client;

namespace UserManagement.Blazor.Services;

public interface IUserLogsHubConnection : IAsyncDisposable
{
    HubConnectionState State { get; }
    Task StartAsync();
    Task StopAsync();
    Task InvokeAsync(string method, params object[] args);
    void On<T>(string methodName, Action<T> handler);
}
