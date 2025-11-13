using System;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using UserManagement.Blazor.Services;
using UserManagement.Shared.DTOs;
using Xunit;

namespace UserManagement.Blazor.Tests.Pages.Users;

public class AddPageTests : BunitContext
{
    private readonly Mock<IUsersClient> client = new();

    [Fact]
    public void RendersFormWithFields()
    {
        // Arrange
        Services.AddSingleton(client.Object);

        // Act
        var cut = Render<Blazor.Pages.Users.Add>();

        // Assert
        cut.Markup.Should().Contain("Forename");
        cut.Markup.Should().Contain("Surname");
        cut.Markup.Should().Contain("Email");
        cut.Markup.Should().Contain("Date of Birth");
        // Ensure inputs exist
        cut.Find("#forename");
        cut.Find("#surname");
        cut.Find("#email");
        cut.Find("#dob");
        cut.Find("#isActive");
    }

    [Fact]
    public void Submit_WithInvalidModel_ShowsValidationMessages()
    {
        // Arrange
        Services.AddSingleton(client.Object);
        var cut = Render<Blazor.Pages.Users.Add>();

        // Act: submit without filling fields to trigger validation
        cut.Find("form").Submit();

        // Assert: required message from DataAnnotations
        cut.Markup.Should().Contain("The Forename field is required.");
        cut.Markup.Should().Contain("The Surname field is required.");
        cut.Markup.Should().Contain("The Email field is required.");
    }

    [Fact]
    public void Submit_WithValidModel_CallsClientAndNavigates()
    {
        // Arrange
        var nav = new TestNav();
        Services.AddSingleton<NavigationManager>(nav);

        client
            .Setup(c => c.CreateUserAsync(It.IsAny<CreateUserRequestDto>(), default))
            .ReturnsAsync(new UserListItemDto(1, "Jane", "Doe", "jane.doe@example.com", true, new(1992, 5, 10)));

        Services.AddSingleton(client.Object);
        var cut = Render<Blazor.Pages.Users.Add>();

        // Act: fill in form and submit
        cut.Find("#forename").Change("Jane");
        cut.Find("#surname").Change("Doe");
        cut.Find("#email").Change("jane.doe@example.com");
        cut.Find("#dob").Change("1992-05-10");
        cut.Find("#isActive").Change(true);

        cut.Find("form").Submit();

        // Assert
        client.Verify(c => c.CreateUserAsync(It.Is<CreateUserRequestDto>(r => r.Forename == "Jane" && r.Surname == "Doe" && r.Email == "jane.doe@example.com" && r.IsActive), default), Times.Once);
        nav.Uri.Should().EndWith("/users");
    }

    [Fact]
    public void Submit_WithFutureDob_ShowsValidationMessage_AndDoesNotCallClient()
    {
        // Arrange
        Services.AddSingleton(client.Object);
        var cut = Render<Blazor.Pages.Users.Add>();

        // Act: fill valid fields but future DOB
        cut.Find("#forename").Change("Jane");
        cut.Find("#surname").Change("Doe");
        cut.Find("#email").Change("jane.doe@example.com");
        var future = DateTime.Today.AddDays(1);
        cut.Find("#dob").Change(future.ToString("yyyy-MM-dd"));
        cut.Find("#isActive").Change(true);

        cut.Find("form").Submit();

        // Assert: validation message appears from IValidatableObject rule
        cut.Markup.Should().Contain("Date of Birth cannot be in the future.");
        client.Verify(c => c.CreateUserAsync(It.IsAny<CreateUserRequestDto>(), default), Times.Never);
    }

    private sealed class TestNav : NavigationManager
    {
        public TestNav() =>
            Initialize("http://localhost/", "http://localhost/");

        protected override void NavigateToCore(string uri, bool forceLoad) =>
            Uri = ToAbsoluteUri(uri).ToString();
    }
}
