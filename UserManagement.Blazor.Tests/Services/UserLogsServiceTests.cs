using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using UserManagement.Blazor.Services;
using UserManagement.Shared.DTOs;
using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using Moq;

namespace UserManagement.Blazor.Tests.Services;

public class UserLogsServiceTests
{
    [Fact]
    public void Constructor_WithNoBaseAddress_ThrowsInvalidOperationException()
    {
        // Arrange
        var http = new HttpClient(); // no BaseAddress configured

        // Act
        Action act = () => new UserLogsService(http);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("HttpClient BaseAddress not configured");
    }

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new UserLogsService((IUserLogsHubConnection)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyConnected_DoesNotCallStart()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        mockConn.SetupGet(c => c.State).Returns(HubConnectionState.Connected);
        var svc = new UserLogsService(mockConn.Object);

        // Act
        await svc.StartAsync();

        // Assert
        mockConn.Verify(c => c.StartAsync(), Times.Never);
    }

    [Fact]
    public async Task StopAsync_WhenAlreadyDisconnected_DoesNotCallStop()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        mockConn.SetupGet(c => c.State).Returns(HubConnectionState.Disconnected);
        var svc = new UserLogsService(mockConn.Object);

        // Act
        await svc.StopAsync();

        // Assert
        mockConn.Verify(c => c.StopAsync(), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WhenDisconnected_CallsStart()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        mockConn.SetupGet(c => c.State).Returns(HubConnectionState.Disconnected);
        mockConn.Setup(c => c.StartAsync()).Returns(Task.CompletedTask).Verifiable();
        var svc = new UserLogsService(mockConn.Object);

        // Act
        await svc.StartAsync();

        // Assert
        mockConn.Verify(c => c.StartAsync(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhenConnected_CallsStop()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        mockConn.SetupGet(c => c.State).Returns(HubConnectionState.Connected);
        mockConn.Setup(c => c.StopAsync()).Returns(Task.CompletedTask).Verifiable();
        var svc = new UserLogsService(mockConn.Object);

        // Act
        await svc.StopAsync();

        // Assert
        mockConn.Verify(c => c.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task JoinUserGroup_InvokesCorrectMethod()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        mockConn.Setup(c => c.InvokeAsync("JoinUserGroup", 42L)).Returns(Task.CompletedTask).Verifiable();
        var svc = new UserLogsService(mockConn.Object);

        // Act
        await svc.JoinUserGroup(42L);

        // Assert
        mockConn.Verify(c => c.InvokeAsync("JoinUserGroup", 42L), Times.Once);
    }

    [Fact]
    public async Task LeaveUserGroup_InvokesCorrectMethod()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        mockConn.Setup(c => c.InvokeAsync("LeaveUserGroup", 42L)).Returns(Task.CompletedTask).Verifiable();
        var svc = new UserLogsService(mockConn.Object);

        // Act
        await svc.LeaveUserGroup(42L);

        // Assert
        mockConn.Verify(c => c.InvokeAsync("LeaveUserGroup", 42L), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_CallsDisposeOnConnection()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        mockConn.Setup(c => c.DisposeAsync()).Returns(ValueTask.CompletedTask).Verifiable();
        var svc = new UserLogsService(mockConn.Object);

        // Act
        await svc.DisposeAsync();

        // Assert
        mockConn.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Fact]
    public void LogReceived_MultipleSubscribers_AllInvoked()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        Action<UserLogDto>? handler = null;
        mockConn.Setup(c => c.On<UserLogDto>("LogAdded", It.IsAny<Action<UserLogDto>>()))
            .Callback<string, Action<UserLogDto>>((_, h) => handler = h);
        var svc = new UserLogsService(mockConn.Object);
        int count = 0;
        svc.LogReceived += _ => count++;
        svc.LogReceived += _ => count++;

        // Act
        handler!(new UserLogDto(1, 2, "msg", DateTime.UtcNow));

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void LogReceived_HandlerException_DoesNotCrash()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        Action<UserLogDto>? handler = null;
        mockConn.Setup(c => c.On<UserLogDto>("LogAdded", It.IsAny<Action<UserLogDto>>()))
            .Callback<string, Action<UserLogDto>>((_, h) => handler = h);
        var svc = new UserLogsService(mockConn.Object);
        svc.LogReceived += _ => throw new Exception("fail");
        svc.LogReceived += _ => { }; // second handler

        // Act
        Action act = () => handler!(new UserLogDto(1, 2, "msg", DateTime.UtcNow));

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void LogReceived_NullHandler_DoesNotThrow()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        Action<UserLogDto>? handler = null;
        mockConn.Setup(c => c.On<UserLogDto>("LogAdded", It.IsAny<Action<UserLogDto>>()))
            .Callback<string, Action<UserLogDto>>((_, h) => handler = h);
        var svc = new UserLogsService(mockConn.Object);

        // Act
        // No subscribers
        Action act = () => handler!(new UserLogDto(1, 2, "msg", DateTime.UtcNow));

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task StartAsync_WhenConnectionThrows_PropagatesException()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        mockConn.SetupGet(c => c.State).Returns(HubConnectionState.Disconnected);
        mockConn.Setup(c => c.StartAsync()).ThrowsAsync(new InvalidOperationException("fail"));
        var svc = new UserLogsService(mockConn.Object);
        Func<Task> act = async () => await svc.StartAsync();

        // Act
        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("fail");
    }

    [Fact]
    public async Task StopAsync_WhenConnectionThrows_PropagatesException()
    {
        // Arrange
        var mockConn = new Mock<IUserLogsHubConnection>();
        mockConn.SetupGet(c => c.State).Returns(HubConnectionState.Connected);
        mockConn.Setup(c => c.StopAsync()).ThrowsAsync(new InvalidOperationException("fail"));
        var svc = new UserLogsService(mockConn.Object);
        Func<Task> act = async () => await svc.StopAsync();

        // Act
        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("fail");
    }
}
