using System;
using System.Threading.Tasks;
using UserManagement.Shared.DTOs;

namespace UserManagement.Blazor.Services;

public interface IUserLogsService : IAsyncDisposable
{
    event Action<UserLogDto>? LogReceived;
    Task StartAsync();
    Task StopAsync();
    Task JoinUserGroup(long userId);
    Task LeaveUserGroup(long userId);
}
