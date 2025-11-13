using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace UserManagement.Web.Hubs;

/// <summary>
/// SignalR hub used to push user log updates to connected clients.
/// </summary>
public class UserLogsHub : Hub
{
    /// <summary>
    /// Instructs the hub to add the calling connection to the group for the specified user id.
    /// </summary>
    /// <param name="userId">The user identifier whose group to join.</param>
    public Task JoinUserGroup(long userId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

    /// <summary>
    /// Removes the calling connection from the group for the specified user id.
    /// </summary>
    /// <param name="userId">The user identifier whose group to leave.</param>
    public Task LeaveUserGroup(long userId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
}
