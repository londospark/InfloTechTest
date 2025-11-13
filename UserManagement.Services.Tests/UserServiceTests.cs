using System.Linq;
using System.Threading.Tasks;
using MockQueryable;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;

namespace UserManagement.Services.Tests;

public class UserServiceTests
{
    [Fact]
    public void GetAll_WhenContextReturnsEntities_MustReturnSameEntities()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var users = SetupUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = service.GetAll();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(users);
    }

    [Fact]
    public void FilterByActive_WhenContextReturnsActiveEntities_MustReturnAllEntities()
    {
        var service = CreateService();
        var users = SetupUsers(isActive: true);

        var result = service.FilterByActive(true);

        result.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void FilterByActive_WhenContextReturnsInactiveEntities_MustReturnNoEntities()
    {
        var service = CreateService();
        _ = SetupUsers(isActive: false);

        var result = service.FilterByActive(true);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var users = SetupUsers();
        var existing = users.First();
        existing.Id = 10;

        dataContext.Setup(dc => dc.DeleteAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await service.DeleteAsync(10);

        // Assert
        result.Should().BeTrue();
        dataContext.Verify(dc => dc.DeleteAsync(It.Is<User>(u => u == existing)), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserMissing_ReturnsFalseAndDoesNotDelete()
    {
        // Arrange
        var service = CreateService();
        _ = SetupUsers();

        // Act
        var result = await service.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
        dataContext.Verify(dc => dc.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    private IQueryable<User> SetupUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true)
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

        var mockQueryable = users.BuildMock();
        dataContext
            .Setup(s => s.GetAll<User>())
            .Returns(mockQueryable);

        return mockQueryable;
    }

    private readonly Mock<IDataContext> dataContext = new();
    private UserService CreateService() => new(dataContext.Object);
}
