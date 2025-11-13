using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using UserManagement.Web.Hubs;

namespace UserManagement.Web.Tests.Hubs;

public class UserLogsHubTests
{
    [Fact]
    public async Task JoinUserGroup_Calls_AddToGroupAsync_WithCorrectConnectionAndGroup()
    {
        // Arrange
        var hub = new UserLogsHub();

        var mockContext = new Mock<HubCallerContext>();
        mockContext.SetupGet(c => c.ConnectionId).Returns("conn-123");

        var mockGroups = new Mock<IGroupManager>();
        mockGroups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Use reflection to set the non-public Context and Groups properties on Hub
        var hubType = typeof(Hub);
        var contextProp = hubType.GetProperty("Context", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;
        contextProp.SetValue(hub, mockContext.Object);

        var groupsProp = hubType.GetProperty("Groups", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;
        groupsProp.SetValue(hub, mockGroups.Object);

        // Act
        await hub.JoinUserGroup(42);

        // Assert
        mockGroups.Verify(g => g.AddToGroupAsync("conn-123", "user-42", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveUserGroup_Calls_RemoveFromGroupAsync_WithCorrectConnectionAndGroup()
    {
        // Arrange
        var hub = new UserLogsHub();

        var mockContext = new Mock<HubCallerContext>();
        mockContext.SetupGet(c => c.ConnectionId).Returns("conn-xyz");

        var mockGroups = new Mock<IGroupManager>();
        mockGroups.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Reflection set
        var hubType = typeof(Hub);
        var contextProp = hubType.GetProperty("Context", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;
        contextProp.SetValue(hub, mockContext.Object);

        var groupsProp = hubType.GetProperty("Groups", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;
        groupsProp.SetValue(hub, mockGroups.Object);

        // Act
        await hub.LeaveUserGroup(99);

        // Assert
        mockGroups.Verify(g => g.RemoveFromGroupAsync("conn-xyz", "user-99", It.IsAny<CancellationToken>()), Times.Once);
    }
}
