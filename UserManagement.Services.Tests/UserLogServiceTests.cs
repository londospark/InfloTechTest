using System.Linq;
using System.Threading.Tasks;
using MockQueryable;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;

namespace UserManagement.Services.Tests;

public class UserLogServiceTests
{
    [Fact]
    public void GetAll_WhenContextReturnsEntities_MustReturnSameEntities()
    {
        // Arrange
        var service = CreateService();
        var logs = SetupLogs();

        // Act
        var result = service.GetAll();

        // Assert
        result.Should().BeSameAs(logs);
    }

    [Fact]
    public async Task AddAsync_WhenCalled_CreatesAndReturnsEntity()
    {
        // Arrange
        var service = CreateService();
        var log = new UserLog { UserId = 1, Message = "Created", CreatedAt = System.DateTime.UtcNow };

        // Act
        var result = await service.AddAsync(log);

        // Assert
        result.Should().BeSameAs(log);
        dataContext.Verify(dc => dc.CreateAsync(It.Is<UserLog>(l => l == log)), Times.Once);
    }

    [Fact]
    public void GetByUserId_WhenLogsExistForUser_ReturnsThoseLogs()
    {
        // Arrange
        var service = CreateService();
        var logs = SetupLogs(userId: 42);

        // Act
        var result = service.GetByUserId(42);

        // Assert
        result.Should().BeEquivalentTo(logs);
    }

    [Fact]
    public void GetByUserId_WhenNoLogsForUser_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();
        _ = SetupLogs(userId: 7);

        // Act
        var result = service.GetByUserId(99);

        // Assert
        result.Should().BeEmpty();
    }

    private IQueryable<UserLog> SetupLogs(long userId = 1, string message = "Test", bool setCreated = true)
    {
        var logs = new[]
        {
            new UserLog
            {
                UserId = userId,
                Message = message,
                CreatedAt = setCreated ? System.DateTime.UtcNow : default
            }
        };

        var mockQueryable = logs.BuildMock();
        dataContext
            .Setup(s => s.GetAll<UserLog>())
            .Returns(mockQueryable);

        return mockQueryable;
    }

    private readonly Mock<IDataContext> dataContext = new();
    private UserLogService CreateService() => new(dataContext.Object);
}
