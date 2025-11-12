using System;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using UserManagement.Blazor.Pages.Users;
using UserManagement.Blazor.Services;
using UserManagement.Shared.DTOs;
using Xunit;

namespace UserManagement.Blazor.Tests.Pages.Users;

public class ListPageTests : BunitContext
{
    private readonly Mock<IUsersClient> _usersClient = new();

    private void RegisterServices()
    {
        Services.AddScoped(_ => _usersClient.Object);
    }

    [Fact]
    public void RendersLoadingInitially()
    {
        // Arrange
        RegisterServices();
        _usersClient
            .Setup(c => c.GetUsersAsync(default))
            .Returns(async () =>
            {
                await Task.Delay(10);
                return new UserListDto(Array.Empty<UserListItemDto>());
            });

        // Act
        var cut = Render<List>();

        // Assert: initial Loading... shown before task completes
        cut.Markup.Should().Contain("Loading...");
    }

    [Fact]
    public async Task RendersUsersTable_WhenUsersReturned()
    {
        // Arrange
        RegisterServices();
        var users = new UserListDto(new[]
        {
            new UserListItemDto(1, "John", "Doe", "john@example.com", true)
        });
        _usersClient
            .Setup(c => c.GetUsersAsync(default))
            .ReturnsAsync(users);

        // Act
        var cut = Render<List>();
        await cut.InvokeAsync(() => Task.CompletedTask); // allow render cycle

        // Assert
        cut.Markup.Should().Contain("Users");
        cut.Markup.Should().Contain("john@example.com");
        cut.FindAll("table tbody tr").Count.Should().Be(1);
    }

    [Fact]
    public async Task ShowsEmptyMessage_WhenNoUsers()
    {
        // Arrange
        RegisterServices();
        _usersClient
            .Setup(c => c.GetUsersAsync(default))
            .ReturnsAsync(new UserListDto(Array.Empty<UserListItemDto>()));

        // Act
        var cut = Render<List>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("No users found.");
    }

    [Fact]
    public async Task FilterActive_InvokesGetUsersByActive()
    {
        // Arrange
        RegisterServices();
        _usersClient
            .Setup(c => c.GetUsersAsync(default))
            .ReturnsAsync(new UserListDto(Array.Empty<UserListItemDto>()));
        _usersClient
            .Setup(c => c.GetUsersByActiveAsync(true, default))
            .ReturnsAsync(new UserListDto(Array.Empty<UserListItemDto>()));

        // Act
        var cut = Render<List>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Change select to Active
        var select = cut.Find("select#userFilter");
        select.Change("Active");

        // Assert
        _usersClient.Verify(c => c.GetUsersByActiveAsync(true, default), Times.Once);
    }

    [Fact]
    public async Task ErrorFromService_ShowsAlert()
    {
        // Arrange
        RegisterServices();
        _usersClient
            .Setup(c => c.GetUsersAsync(default))
            .ThrowsAsync(new InvalidOperationException("Boom"));

        // Act
        var cut = Render<List>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("alert alert-danger");
        cut.Markup.Should().Contain("Boom");
    }
}
