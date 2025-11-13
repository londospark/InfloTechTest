using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        var controller = CreateController();
        var users = SetupUsers();

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
        var controller = CreateController();
        var users = SetupUsers(isActive: true);

        var result = controller.ListByActive(isActive: true);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeOfType<UserListDto>()
            .Which.Items.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void ListByActive_WhenFilteringInactiveUsers_ModelMustContainOnlyInactiveUsers()
    {
        var controller = CreateController();
        var users = SetupUsers(isActive: false);

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
        var logger = new Mock<ILogger<UsersController>>();
        var controller = CreateController(logger.Object);
        var req = new CreateUserRequestDto(
            "Jane",
            "Doe",
            "jane.doe@example.com",
            new(1992, 5, 10),
            true
        );

        // Simulate DB assigning id
        dataContext
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
        dataContext.Verify(dc => dc.Create(It.Is<User>(u =>
            u.Forename == req.Forename &&
            u.Surname == req.Surname &&
            u.Email == req.Email &&
            u.IsActive == req.IsActive &&
            u.DateOfBirth == req.DateOfBirth
        )), Times.Once);

        // And a log was written for creation
        VerifyLogContains(logger, LogLevel.Information, "Created user id");
    }

    [Fact]
    public void Create_WhenInvalidRequest_ReturnsBadRequestAndDoesNotPersist()
    {
        // Arrange: invalid Forename (empty). Provide other valid fields
        var logger = new Mock<ILogger<UsersController>>();
        var controller = CreateController(logger.Object);
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
        dataContext.Verify(dc => dc.Create(It.IsAny<User>()), Times.Never);

        // And a warning was logged
        VerifyLogContains(logger, LogLevel.Warning, "Create user validation failed");
    }

    [Fact]
    public void Create_WhenDateOfBirthInFuture_ReturnsBadRequest()
    {
        // Arrange: use a future DOB to hit shared IValidatableObject rule
        var controller = CreateController();
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
        dataContext.Verify(dc => dc.Create(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public void GetById_WhenFound_Returns200WithUser()
    {
        var controller = CreateController();
        var users = SetupUsers();
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
        var logger = new Mock<ILogger<UsersController>>();
        var controller = CreateController(logger.Object);
        _ = SetupUsers();

        var result = controller.GetById(999);

        result.Result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);

        // Warning logged for not found
        VerifyLogContains(logger, LogLevel.Warning, "User not found for id");
    }

    [Fact]
    public void Delete_WhenFound_ReturnsNoContent()
    {
        // Arrange
        var logger = new Mock<ILogger<UsersController>>();
        var controller = CreateController(logger.Object);
        var users = SetupUsers();
        users.First().Id = 5;

        // Act
        var result = controller.Delete(5);

        // Assert
        result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
        dataContext.Verify(dc => dc.Delete(It.Is<User>(u => u.Id == 5)), Times.Once);

        // Information logs for delete flow
        VerifyLogContains(logger, LogLevel.Information, "Deleting user id");
        VerifyLogContains(logger, LogLevel.Information, "Deleted user id");
    }

    [Fact]
    public void Delete_WhenMissing_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController();
        _ = SetupUsers();

        // Act
        var result = controller.Delete(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
        dataContext.Verify(dc => dc.Delete(It.IsAny<User>()), Times.Never);
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

        dataContext
            .Setup(s => s.GetAll<User>())
            .Returns(users);

        return users;
    }

    private readonly Mock<IDataContext> dataContext = new();
    private UsersController CreateController(ILogger<UsersController>? logger = null)
        => new(
            new UserService(dataContext.Object),
            new UserLogService(dataContext.Object),
            logger ?? new NullLogger<UsersController>());

    [Fact]
    public void GetLogs_WhenLogsExistForUser_ReturnsPagedDescending()
    {
        // Arrange
        var controller = CreateController();
        var now = DateTime.UtcNow;
        var logs = new[]
        {
            new UserLog { Id = 1, UserId = 77, Message = "A", CreatedAt = now.AddMinutes(-1) },
            new UserLog { Id = 2, UserId = 77, Message = "B", CreatedAt = now.AddMinutes(-2) },
            new UserLog { Id = 3, UserId = 77, Message = "C", CreatedAt = now.AddMinutes(-3) },
            new UserLog { Id = 4, UserId = 99, Message = "Other user", CreatedAt = now.AddMinutes(-4) },
        }.AsQueryable();

        dataContext
            .Setup(s => s.GetAll<UserLog>())
            .Returns(logs);

        // Act: request page 1, pageSize 2 for user 77
        var action = controller.GetLogs(77, page: 1, pageSize: 2);

        // Assert
        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var paged = ok.Value.As<PagedResultDto<UserLogDto>>();
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(2);
        paged.TotalCount.Should().Be(3); // only three logs for user 77
        paged.Items.Count.Should().Be(2);
        paged.Items[0].Message.Should().Be("A"); // newest first
        paged.Items[1].Message.Should().Be("B");

        // Act: page 2
        var actionPage2 = controller.GetLogs(77, page: 2, pageSize: 2);
        var ok2 = actionPage2.Result.Should().BeOfType<OkObjectResult>().Subject;
        var paged2 = ok2.Value.As<PagedResultDto<UserLogDto>>();
        paged2.Page.Should().Be(2);
        paged2.PageSize.Should().Be(2);
        paged2.TotalCount.Should().Be(3);
        paged2.Items.Count.Should().Be(1);
        paged2.Items[0].Message.Should().Be("C");
    }

    private static void VerifyLogContains(Mock<ILogger<UsersController>> logger, LogLevel level, string contains)
    {
        logger.Verify(l => l.Log(
                It.Is<LogLevel>(ll => ll == level),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains(contains)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.AtLeastOnce);
    }
}
