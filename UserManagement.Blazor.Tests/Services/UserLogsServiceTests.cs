using System;
using System.Net.Http;
using FluentAssertions;
using UserManagement.Blazor.Services;
using Xunit;

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
}
