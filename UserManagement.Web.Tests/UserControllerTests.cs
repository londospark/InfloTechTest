using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;
using UserManagement.Shared.DTOs;
using UserManagement.Web.Controllers;

namespace UserManagement.Web.Tests;

public class UserControllerTests
{
    [Fact]
    public void List_WhenServiceReturnsUsers_ModelMustContainUsers()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = this.CreateController();
        var users = this.SetupUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = controller.List();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Value
            .Should().BeOfType<UserListDto>()
            .Which.Items.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void ListByActive_WhenFilteringActiveUsers_ModelMustContainOnlyActiveUsers()
    {
        var controller = this.CreateController();
        var users = this.SetupUsers(isActive: true);

        var result = controller.ListByActive(isActive: true);

        result.Value
            .Should().BeOfType<UserListDto>()
            .Which.Items.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void ListByActive_WhenFilteringInactiveUsers_ModelMustContainOnlyInactiveUsers()
    {
        var controller = this.CreateController();
        var users = this.SetupUsers(isActive: false);

        var result = controller.ListByActive(isActive: false);

        result.Value
            .Should().BeOfType<UserListDto>()
            .Which.Items.Should().BeEquivalentTo(users);
    }
    
    private User[] SetupUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true)
    {
        var users = new[]
        {
            new User
            {
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = isActive
            }
        };

        this.userService
            .Setup(s => s.GetAll())
            .Returns(users);

        this.userService
            .Setup(s => s.FilterByActive(isActive))
            .Returns(users);

        return users;
    }

    private readonly Mock<IUserService> userService = new();
    private UsersController CreateController() => new(this.userService.Object);
}
