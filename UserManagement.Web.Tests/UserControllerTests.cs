using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;
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
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeOfType<UserListDto>()
            .Which.Items.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void ListByActive_WhenFilteringActiveUsers_ModelMustContainOnlyActiveUsers()
    {
        var controller = this.CreateController();
        var users = this.SetupUsers(isActive: true);

        var result = controller.ListByActive(isActive: true);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeOfType<UserListDto>()
            .Which.Items.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void ListByActive_WhenFilteringInactiveUsers_ModelMustContainOnlyInactiveUsers()
    {
        var controller = this.CreateController();
        var users = this.SetupUsers(isActive: false);

        var result = controller.ListByActive(isActive: false);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeOfType<UserListDto>()
            .Which.Items.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void Create_WhenValidRequest_Returns201CreatedWithLocationAndBody()
    {
        // Arrange
        var controller = this.CreateController();
        var req = new CreateUserRequestDto(
            "Jane",
            "Doe",
            "jane.doe@example.com",
            new(1992, 5, 10),
            true
        );

        // Simulate DB assigning id
        this.dataContext
            .Setup(dc => dc.Create(It.IsAny<User>()))
            .Callback<User>(u => u.Id = 123);

        // Act
        var action = controller.Create(req);

        // Assert: returns 201 Created with body and Location
        var created = action.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.ActionName.Should().Be(nameof(UsersController.GetById));
        created.RouteValues!["id"].Should().Be(123);
        var dto = created.Value.As<UserListItemDto>();
        dto.Id.Should().Be(123);
        dto.Forename.Should().Be("Jane");
        dto.Surname.Should().Be("Doe");
        dto.Email.Should().Be("jane.doe@example.com");
        dto.IsActive.Should().BeTrue();
        dto.DateOfBirth.Should().Be(new(1992, 5, 10));

        // Verify the data context was called with expected user
        this.dataContext.Verify(dc => dc.Create(It.Is<User>(u =>
            u.Forename == req.Forename &&
            u.Surname == req.Surname &&
            u.Email == req.Email &&
            u.IsActive == req.IsActive &&
            u.DateOfBirth == req.DateOfBirth
        )), Times.Once);
    }

    [Fact]
    public void Create_WhenInvalidRequest_ReturnsBadRequestAndDoesNotPersist()
    {
        // Arrange: invalid Forename (empty). Provide other valid fields
        var controller = this.CreateController();
        var req = new CreateUserRequestDto(
            "", // invalid
            "Doe",
            "jane.doe@example.com",
            new(1992, 5, 10),
            true
        );

        // Act
        var action = controller.Create(req);

        // Assert: should return a problem response (400 Bad Request in API pipeline)
        var objectResult = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        objectResult.Value.Should().BeOfType<ValidationProblemDetails>();
        objectResult.StatusCode.Should().Be(400);

        // And no persistence should occur
        this.dataContext.Verify(dc => dc.Create(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public void Create_WhenDateOfBirthInFuture_ReturnsBadRequest()
    {
        // Arrange: use a future DOB to hit shared IValidatableObject rule
        var controller = this.CreateController();
        var future = DateTime.Now.AddDays(1);
        var req = new CreateUserRequestDto(
            "Jane",
            "Doe",
            "jane.doe@example.com",
            future,
            true
        );

        // Act
        var action = controller.Create(req);

        // Assert
        var objectResult = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        objectResult.Value.Should().BeOfType<ValidationProblemDetails>()
            .Which.Errors.Should().ContainKey(nameof(CreateUserRequestDto.DateOfBirth));

        // No persistence
        this.dataContext.Verify(dc => dc.Create(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public void GetById_WhenFound_Returns200WithUser()
    {
        var controller = this.CreateController();
        var users = this.SetupUsers();
        users.First().Id = 42;

        var result = controller.GetById(42);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeOfType<UserListItemDto>()
            .Which.Id.Should().Be(42);
    }

    [Fact]
    public void GetById_WhenMissing_Returns404()
    {
        var controller = this.CreateController();
        _ = this.SetupUsers();

        var result = controller.GetById(999);

        result.Result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public void Delete_WhenFound_ReturnsNoContent()
    {
        // Arrange
        var controller = this.CreateController();
        var users = this.SetupUsers();
        users.First().Id = 5;

        // Act
        var result = controller.Delete(5);

        // Assert
        result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
        this.dataContext.Verify(dc => dc.Delete(It.Is<User>(u => u.Id == 5)), Times.Once);
    }

    [Fact]
    public void Delete_WhenMissing_ReturnsNotFound()
    {
        // Arrange
        var controller = this.CreateController();
        _ = this.SetupUsers();

        // Act
        var result = controller.Delete(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
        this.dataContext.Verify(dc => dc.Delete(It.IsAny<User>()), Times.Never);
    }

    private IQueryable<User> SetupUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true, DateTime? dateOfBirth = null)
    {
        dateOfBirth ??= new(1990, 1, 1);
        var users = new[]
        {
            new User
            {
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = isActive,
                DateOfBirth = dateOfBirth.Value
            }
        }.AsQueryable();

        this.dataContext
            .Setup(s => s.GetAll<User>())
            .Returns(users);

        return users;
    }

    private readonly Mock<IDataContext> dataContext = new();
    private UsersController CreateController() => new(new UserService(this.dataContext.Object));
}
