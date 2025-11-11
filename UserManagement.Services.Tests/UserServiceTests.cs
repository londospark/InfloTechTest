using System.Linq;
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
        var service = this.CreateService();
        var users = this.SetupUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = service.GetAll();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(users);
    }

    [Fact]
    public void FilterByActive_WhenContextReturnsActiveEntities_MustReturnAllEntities()
    {
        var service = this.CreateService();
        var users = this.SetupUsers(isActive: true);

        var result = service.FilterByActive(true);

        result.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void FilterByActive_WhenContextReturnsInactiveEntities_MustReturnNoEntities()
    {
        var service = this.CreateService();
        _ = this.SetupUsers(isActive: false);

        var result = service.FilterByActive(true);

        result.Should().BeEmpty();
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
        }.AsQueryable();

        this.dataContext
            .Setup(s => s.GetAll<User>())
            .Returns(users);

        return users;
    }

    private readonly Mock<IDataContext> dataContext = new();
    private UserService CreateService() => new(this.dataContext.Object);
}
