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
    private readonly Mock<IUsersClient> usersClient = new();

    private void RegisterServices()
    {
        Services.AddScoped(_ => this.usersClient.Object);
    }

    [Fact]
    public void RendersLoadingInitially()
    {
        // Arrange
        RegisterServices();
        this.usersClient
            .Setup(c => c.GetUsersAsync(default))
            .Returns(async () =>
            {
                await Task.Delay(10);
                return new([]);
            });

        // Act
        var cut = Render<List>();

        // Assert: initial Loading... shown before task completes
        cut.Markup.Should().Contain("Loading...");
    }

    [Fact]
    public async Task RendersUsersTable_WhenUsersReturned_IncludingDateOfBirth()
    {
        // Arrange
        RegisterServices();
        var dob = new DateTime(1990, 2, 1);
        var users = new UserListDto([
            new(1, "John", "Doe", "john@example.com", true, dob)
        ]);
        this.usersClient
            .Setup(c => c.GetUsersAsync(default))
            .ReturnsAsync(users);

        // Act
        var cut = Render<List>();
        await cut.InvokeAsync(() => Task.CompletedTask); // allow render cycle

        // Assert
        cut.Markup.Should().Contain("Users");
        cut.Markup.Should().Contain("john@example.com");
        cut.FindAll("table tbody tr").Count.Should().Be(1);
        cut.Markup.Should().Contain(dob.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task ShowsEmptyMessage_WhenNoUsers()
    {
        // Arrange
        RegisterServices();
        this.usersClient
            .Setup(c => c.GetUsersAsync(default))
            .ReturnsAsync(new UserListDto([]));

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
        this.usersClient
            .Setup(c => c.GetUsersAsync(default))
            .ReturnsAsync(new UserListDto([]));
        this.usersClient
            .Setup(c => c.GetUsersByActiveAsync(true, default))
            .ReturnsAsync(new UserListDto([]));

        // Act
        var cut = Render<List>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Change select to Active
        var select = cut.Find("select#userFilter");
        select.Change("Active");

        // Assert
        this.usersClient.Verify(c => c.GetUsersByActiveAsync(true, default), Times.Once);
    }

    [Fact]
    public async Task ErrorFromService_ShowsAlert()
    {
        // Arrange
        RegisterServices();
        this.usersClient
            .Setup(c => c.GetUsersAsync(default))
            .ThrowsAsync(new InvalidOperationException("Boom"));

        // Act
        var cut = Render<List>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("alert alert-danger");
        cut.Markup.Should().Contain("Boom");
    }

    [Fact]
    public async Task Delete_WhenConfirmed_CallsClientAndRefreshes()
    {
        // Arrange
        RegisterServices();
        // initial list has one user with id 1
        var users = new UserListDto([ new(1, "John", "Doe", "john@example.com", true, new DateTime(1990,1,1)) ]);
        this.usersClient.Setup(c => c.GetUsersAsync(default)).ReturnsAsync(users);
        // After deletion, subsequent load returns empty
        this.usersClient.Setup(c => c.GetUsersByActiveAsync(It.IsAny<bool>(), default)).ReturnsAsync(new UserListDto([]));
        this.usersClient.Setup(c => c.DeleteUserAsync(1, default)).Returns(Task.CompletedTask);

        // Act
        var cut = Render<List>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Click delete then confirm in modal
        cut.Find("button[data-testid='delete-1']").Click();
        cut.Find("button[data-testid='confirm-delete']").Click();

        // Assert
        this.usersClient.Verify(c => c.DeleteUserAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenCancelled_DoesNotCallClient()
    {
        // Arrange
        RegisterServices();
        var users = new UserListDto([ new(1, "John", "Doe", "john@example.com", true, new DateTime(1990,1,1)) ]);
        this.usersClient.Setup(c => c.GetUsersAsync(default)).ReturnsAsync(users);

        // Act
        var cut = Render<List>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Click delete then cancel in modal
        cut.Find("button[data-testid='delete-1']").Click();
        cut.Find("button[data-testid='cancel-delete']").Click();

        // Assert
        this.usersClient.Verify(c => c.DeleteUserAsync(It.IsAny<long>(), default), Times.Never);
    }

    [Fact]
    public async Task ConfirmDialog_ShouldRenderAboveBackdrop_AndExposeClickableButtonsStyles()
    {
        // Arrange
        RegisterServices();
        var users = new UserListDto([ new(1, "John", "Doe", "john@example.com", true, new DateTime(1990,1,1)) ]);
        this.usersClient.Setup(c => c.GetUsersAsync(default)).ReturnsAsync(users);

        // Act
        var cut = Render<List>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Open confirm dialog
        cut.Find("button[data-testid='delete-1']").Click();

        // Assert desired layering and clickability indicators (expected correct behavior)
        var modal = cut.Find(".modal");
        var backdrop = cut.Find(".modal-backdrop");

        // Expect explicit z-index styles to ensure modal is on top of the shaded overlay
        modal.GetAttribute("style").Should().Contain("z-index: 1050");
        backdrop.GetAttribute("style").Should().Contain("z-index: 1040");

        // Expect the backdrop element to be rendered before the modal content in DOM
        var markup = cut.Markup;
        markup.IndexOf("modal-backdrop").Should().BeLessThan(markup.IndexOf("modal-dialog"));

        // And the backdrop should not intercept clicks
        backdrop.GetAttribute("style").Should().Contain("pointer-events: none");
    }
}
