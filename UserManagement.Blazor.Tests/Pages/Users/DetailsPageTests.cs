using System;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
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
    public async Task Save_CallsUpdateAndUpdatesUi()
    {
        // Arrange
        Register();
        var original = new UserListItemDto(7, "Jane", "Doe", "jane@example.com", false, new(1991, 3, 15));
        var updatedRequest = default(CreateUserRequestDto);

        this.usersClient.Setup(c => c.GetUserAsync(7, default)).ReturnsAsync(original);
        this.usersClient
            .Setup(c => c.UpdateUserAsync(7, It.IsAny<CreateUserRequestDto>(), default))
            .Callback<long, CreateUserRequestDto, System.Threading.CancellationToken>((_, req, _) => updatedRequest = req)
            .ReturnsAsync(new UserListItemDto(7, "Janet", "Smith", "janet@example.com", true, new(1990, 5, 20)));

        var cut = Render<Details>(ps => ps.Add(p => p.id, 7));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act - enter edit mode
        cut.Find("[data-testid='edit-user']").Click();

        // Change fields
        cut.Find("#forename").Change("Janet");
        cut.Find("#surname").Change("Smith");
        cut.Find("#email").Change("janet@example.com");
        // bUnit InputDate expects yyyy-MM-dd string when changing via markup
        cut.Find("#dob").Change("1990-05-20");
        // toggle active to true
        cut.Find("#isActive").Change(true);

        // Save
        cut.Find("[data-testid='save-user']").Click();

        // Assert - UpdateUserAsync was called with the edited values
        this.usersClient.Verify(c => c.UpdateUserAsync(7, It.Is<CreateUserRequestDto>(r =>
            r.Forename == "Janet" &&
            r.Surname == "Smith" &&
            r.Email == "janet@example.com" &&
            r.IsActive == true &&
            r.DateOfBirth == new DateTime(1990, 5, 20)
        ), default), Times.Once);

        // And UI reflects updated values and is back to read-only
        cut.Find("[data-testid='user-forename']").TextContent.Should().Be("Janet");
        cut.Find("[data-testid='user-surname']").TextContent.Should().Be("Smith");
        cut.Find("[data-testid='user-email']").TextContent.Should().Be("janet@example.com");
        cut.Find("[data-testid='user-dob']").TextContent.Should().Be(new DateTime(1990, 5, 20).ToString("d", System.Globalization.CultureInfo.CurrentCulture));
        cut.Find("[data-testid='user-active']").TextContent.Should().Be("Yes");
        cut.Markup.Should().Contain("data-testid=\"edit-user\"");
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
        cut.Find("[data-testid='user-dob']").TextContent.Should().Be(new DateTime(1991, 3, 15).ToString("d", System.Globalization.CultureInfo.CurrentCulture));
        cut.Find("[data-testid='user-active']").TextContent.Should().Be("No");
    }

    [Fact]
    public async Task QueryParam_edit_true_StartsInEditMode()
    {
        // Arrange
        Register();
        var dto = new UserListItemDto(7, "Jane", "Doe", "jane@example.com", false, new(1991, 3, 15));
        this.usersClient.Setup(c => c.GetUserAsync(7, default)).ReturnsAsync(dto);

        // Arrange navigation to include the query parameter (?edit=true)
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/users/7?edit=true");

        // Act: render component (query string will bind to [SupplyParameterFromQuery] edit)
        var cut = Render<Details>(ps => ps.Add(p => p.id, 7));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert: Should be in edit mode (Save/Cancel visible, Edit button hidden)
        cut.Find("[data-testid='save-user']");
        cut.Find("[data-testid='cancel-edit']");
        cut.Markup.Should().NotContain("data-testid=\"edit-user\"");

        // And input fields should be rendered
        cut.Find("#forename");
        cut.Find("#surname");
        cut.Find("#email");
        cut.Find("#dob");
        cut.Find("#isActive");
    }

    [Fact]
    public async Task Save_NavigatesBack_WhenReturnProvided()
    {
        // Arrange
        Register();
        var original = new UserListItemDto(7, "Jane", "Doe", "jane@example.com", false, new(1991, 3, 15));
        this.usersClient.Setup(c => c.GetUserAsync(7, default)).ReturnsAsync(original);
        this.usersClient
            .Setup(c => c.UpdateUserAsync(7, It.IsAny<CreateUserRequestDto>(), default))
            .ReturnsAsync(new UserListItemDto(7, "Jane", "Doe", "jane@example.com", false, new(1991, 3, 15)));

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/users/7?edit=true&return=/users");

        var cut = Render<Details>(ps => ps.Add(p => p.id, 7));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act: immediately save (already in edit mode due to query param)
        cut.Find("[data-testid='save-user']").Click();

        // Assert: navigated back to return url
        nav.Uri.Should().EndWith("/users");
    }

    [Fact]
    public async Task Cancel_NavigatesBack_WhenReturnProvided()
    {
        // Arrange
        Register();
        var dto = new UserListItemDto(7, "Jane", "Doe", "jane@example.com", false, new(1991, 3, 15));
        this.usersClient.Setup(c => c.GetUserAsync(7, default)).ReturnsAsync(dto);

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/users/7?edit=true&return=/users");

        var cut = Render<Details>(ps => ps.Add(p => p.id, 7));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        cut.Find("[data-testid='cancel-edit']").Click();

        // Assert
        nav.Uri.Should().EndWith("/users");
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
