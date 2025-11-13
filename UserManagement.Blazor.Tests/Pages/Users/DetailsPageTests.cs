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

public class DetailsPageTests : BunitContext
{
    private readonly Mock<IUsersClient> usersClient = new();

    private void Register()
    {
        Services.AddScoped(_ => this.usersClient.Object);
    }

    [Fact]
    public void ShowsLoadingInitially()
    {
        // Arrange
        Register();
        this.usersClient.Setup(c => c.GetUserAsync(It.IsAny<long>(), default)).Returns(async () =>
        {
            await Task.Delay(10);
            return new(1, "A", "B", "a@b.com", true, new(2000, 1, 1));
        });

        // Act
        var cut = Render<Details>(ps => ps.Add(p => p.id, 1));

        // Assert
        cut.Markup.Should().Contain("Loading...");
    }

    [Fact]
    public async Task RendersUserDetails_OnSuccess()
    {
        // Arrange
        Register();
        var dto = new UserListItemDto(5, "Jane", "Doe", "jane@example.com", false, new(1991, 3, 15));
        this.usersClient.Setup(c => c.GetUserAsync(5, default)).ReturnsAsync(dto);

        // Act
        var cut = Render<Details>(ps => ps.Add(p => p.id, 5));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Find("[data-testid='user-id']").TextContent.Should().Be("5");
        cut.Find("[data-testid='user-forename']").TextContent.Should().Be("Jane");
        cut.Find("[data-testid='user-surname']").TextContent.Should().Be("Doe");
        cut.Find("[data-testid='user-email']").TextContent.Should().Be("jane@example.com");
        cut.Find("[data-testid='user-dob']").TextContent.Should().Be("1991-03-15");
        cut.Find("[data-testid='user-active']").TextContent.Should().Be("No");
    }

    [Fact]
    public async Task ShowsError_OnFailure()
    {
        // Arrange
        Register();
        this.usersClient.Setup(c => c.GetUserAsync(9, default)).ThrowsAsync(new InvalidOperationException("Boom"));

        // Act
        var cut = Render<Details>(ps => ps.Add(p => p.id, 9));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("alert alert-danger");
        cut.Markup.Should().Contain("Boom");
    }
}
