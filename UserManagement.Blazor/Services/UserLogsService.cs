using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UserManagement.Shared.DTOs;

namespace UserManagement.Blazor.Services;

public sealed class UserLogsService : IUserLogsService
{
    private readonly HubConnection _connection;

    public event Action<UserLogDto>? LogReceived;

    public UserLogsService(HttpClient httpClient)
    {
        // Use the same base URL as the API client (where the SignalR hub is hosted)
        var apiBaseUrl = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? throw new InvalidOperationException("HttpClient BaseAddress not configured");
        var hubUrl = $"{apiBaseUrl}/hubs/userlogs";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<UserLogDto>("LogAdded", dto => LogReceived?.Invoke(dto));
    }

    public async Task StartAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
            await _connection.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_connection.State != HubConnectionState.Disconnected)
            await _connection.StopAsync();
    }

    public async Task JoinUserGroup(long userId) => await _connection.InvokeAsync("JoinUserGroup", userId);
    public async Task LeaveUserGroup(long userId) => await _connection.InvokeAsync("LeaveUserGroup", userId);

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
