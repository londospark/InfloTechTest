using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;
using UserManagement.Shared.DTOs;
using UserManagement.Web.Controllers;
using UserManagement.Web.Tests.TestHelpers;

namespace UserManagement.Web.Tests.Controllers;

/// <summary>
/// Tests for the field change tracking functionality added to the Update endpoint.
/// These tests verify that detailed audit logs are created showing what fields changed
/// and their old/new values.
/// </summary>
public class UpdateFieldChangeTrackingTests
{
    [Fact]
    public async Task Update_AllFieldsChange_LogsAllChangesWithCorrectFormat()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "Jane",
            "Smith",
            "jane@example.com",
            new DateTime(1995, 6, 15),
            false
        );

        // Act
        var result = await controller.Update(user.Id, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        AssertLogContainsChange(userLog.Message, "Forename", "John", "Jane");
        AssertLogContainsChange(userLog.Message, "Surname", "Doe", "Smith");
        AssertLogContainsChange(userLog.Message, "Email", "john@example.com", "jane@example.com");
        AssertLogContainsChange(userLog.Message, "IsActive", "True", "False");
        AssertLogContainsChange(userLog.Message, "DateOfBirth", "1990-01-01", "1995-06-15");
        
        userLog.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Update_SingleForenameChange_OnlyLogsForenameChange()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "Jonathan",
            "Doe",
            "john@example.com",
            new DateTime(1990, 1, 1),
            true
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        AssertLogContainsChange(userLog.Message, "Forename", "John", "Jonathan");
        userLog.Message.Should().NotContain("Surname:");
        userLog.Message.Should().NotContain("Email:");
        userLog.Message.Should().NotContain("IsActive:");
        userLog.Message.Should().NotContain("DateOfBirth:");
    }

    [Fact]
    public async Task Update_SingleSurnameChange_OnlyLogsSurnameChange()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "John",
            "Smith",
            "john@example.com",
            new DateTime(1990, 1, 1),
            true
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        AssertLogContainsChange(userLog.Message, "Surname", "Doe", "Smith");
        userLog.Message.Should().NotContain("Forename:");
    }

    [Fact]
    public async Task Update_SingleEmailChange_OnlyLogsEmailChange()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "old@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "John",
            "Doe",
            "new@example.com",
            new DateTime(1990, 1, 1),
            true
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        AssertLogContainsChange(userLog.Message, "Email", "old@example.com", "new@example.com");
    }

    [Fact]
    public async Task Update_ActiveStatusChange_OnlyLogsActiveStatusChange()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "John",
            "Doe",
            "john@example.com",
            new DateTime(1990, 1, 1),
            false
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        AssertLogContainsChange(userLog.Message, "IsActive", "True", "False");
    }

    [Fact]
    public async Task Update_DateOfBirthChange_OnlyLogsDateChange()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "John",
            "Doe",
            "john@example.com",
            new DateTime(1992, 12, 25),
            true
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        AssertLogContainsChange(userLog.Message, "DateOfBirth", "1990-01-01", "1992-12-25");
    }

    [Fact]
    public async Task Update_NoChanges_DoesNotCreateAuditLog()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "John",
            "Doe",
            "john@example.com",
            new DateTime(1990, 1, 1),
            true
        );

        // Act
        var result = await controller.Update(user.Id, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        
        // Should log that no changes were detected
        logger.LogContains(LogLevel.Information, "No changes detected").Should().BeTrue();
        
        // Should NOT create a UserLog entry
        ctx.UserLogs!.Where(l => l.UserId == user.Id).Should().BeEmpty();
    }

    [Fact]
    public async Task Update_MultipleChanges_SeparatesWithSemicolon()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "Jane",
            "Smith",
            "john@example.com",
            new DateTime(1990, 1, 1),
            true
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        
        // Verify semicolon separation
        userLog.Message.Should().Contain("; ");
        
        // Both changes should be in the message
        AssertLogContainsChange(userLog.Message, "Forename", "John", "Jane");
        AssertLogContainsChange(userLog.Message, "Surname", "Doe", "Smith");
    }

    [Fact]
    public async Task Update_EmailCaseOnlyChange_DoesNotLogChange()
    {
        // Arrange - Email comparison is case-insensitive
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "John",
            "Doe",
            "JOHN@EXAMPLE.COM",
            new DateTime(1990, 1, 1),
            true
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert - case-only change in email should not trigger change detection
        logger.LogContains(LogLevel.Information, "No changes detected").Should().BeTrue();
        ctx.UserLogs!.Where(l => l.UserId == user.Id).Should().BeEmpty();
    }

    [Fact]
    public async Task Update_ChangeFromActiveToInactive_LogsCorrectDirection()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "John",
            "Doe",
            "john@example.com",
            new DateTime(1990, 1, 1),
            false
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        userLog.Message.Should().Contain("IsActive: True → False");
    }

    [Fact]
    public async Task Update_ChangeFromInactiveToActive_LogsCorrectDirection()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", false, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "John",
            "Doe",
            "john@example.com",
            new DateTime(1990, 1, 1),
            true
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        userLog.Message.Should().Contain("IsActive: False → True");
    }

    [Fact]
    public async Task Update_WithSpecialCharactersInFields_LogsCorrectly()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "O'Brien", "Müller", "test@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "O'Connor",
            "Müller-Schmidt",
            "test@example.com",
            new DateTime(1990, 1, 1),
            true
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        AssertLogContainsChange(userLog.Message, "Forename", "O'Brien", "O'Connor");
        AssertLogContainsChange(userLog.Message, "Surname", "Müller", "Müller-Schmidt");
    }

    [Fact]
    public async Task Update_DateFormatIsConsistent_UsesYyyyMmDdFormat()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 3, 5));
        
        var request = new CreateUserRequestDto(
            "John",
            "Doe",
            "john@example.com",
            new DateTime(2000, 12, 25),
            true
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        
        // Verify format is yyyy-MM-dd (not culture-specific)
        userLog.Message.Should().Contain("DateOfBirth: 1990-03-05 → 2000-12-25");
    }

    [Fact]
    public async Task Update_LogMessageStructure_FollowsExpectedFormat()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "Jane",
            "Doe",
            "john@example.com",
            new DateTime(1990, 1, 1),
            true
        );

        // Act
        await controller.Update(user.Id, request);

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        
        // Message should start with "Updated user id X:"
        userLog.Message.Should().StartWith($"Updated user id {user.Id}:");
        
        // Message should contain the arrow symbol
        userLog.Message.Should().Contain("→");
        
        // Message should have the user ID properly set
        userLog.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Update_CreatedAtTimestamp_IsUtc()
    {
        // Arrange
        var (ctx, logger, controller) = CreateTestSetup();
        var user = CreateUser(ctx, "John", "Doe", "john@example.com", true, new DateTime(1990, 1, 1));
        
        var request = new CreateUserRequestDto(
            "Jane",
            "Doe",
            "john@example.com",
            new DateTime(1990, 1, 1),
            true
        );

        // Act
        var before = DateTime.UtcNow;
        await controller.Update(user.Id, request);
        var after = DateTime.UtcNow;

        // Assert
        var userLog = ctx.UserLogs!.Single(l => l.UserId == user.Id);
        userLog.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        userLog.CreatedAt.Should().BeOnOrAfter(before);
        userLog.CreatedAt.Should().BeOnOrBefore(after);
    }

    // Helper methods

    private static (DataContext ctx, MockLogger<UsersController> logger, UsersController controller) CreateTestSetup()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;
        
        var ctx = new DataContext(options);
        ctx.Database.EnsureCreated();

        var logger = new MockLogger<UsersController>();
        var userService = new UserService(ctx);
        var userLogService = new UserLogService(ctx);
        var controller = new UsersController(userService, userLogService, logger.AsILogger());

        return (ctx, logger, controller);
    }

    private static User CreateUser(DataContext ctx, string forename, string surname, string email, bool isActive, DateTime dateOfBirth)
    {
        var user = new User
        {
            Forename = forename,
            Surname = surname,
            Email = email,
            IsActive = isActive,
            DateOfBirth = dateOfBirth
        };

        ctx.Users!.Add(user);
        ctx.SaveChanges();
        
        return user;
    }

    private static void AssertLogContainsChange(string logMessage, string fieldName, string oldValue, string newValue)
    {
        // For string fields, values are wrapped in single quotes
        if (fieldName is "Forename" or "Surname" or "Email")
        {
            logMessage.Should().Contain($"{fieldName}: '{oldValue}' → '{newValue}'");
        }
        else
        {
            logMessage.Should().Contain($"{fieldName}: {oldValue} → {newValue}");
        }
    }
}
