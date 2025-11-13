using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;
using Microsoft.Data.Sqlite;

namespace UserManagement.Services.Tests;

public class UserServiceTests
{
    [Fact]
    public void GetAll_WhenContextReturnsEntities_MustReturnSameEntities()
    {
        var ctx = CreateContext();
        var service = new UserService(ctx);
        var users = SetupUsers(ctx);

        var result = service.GetAll();

        result.Should().BeSameAs(users);
    }

    [Fact]
    public void FilterByActive_WhenContextReturnsActiveEntities_MustReturnAllEntities()
    {
        var ctx = CreateContext();
        var service = new UserService(ctx);
        var users = SetupUsers(ctx, isActive: true);

        var result = service.FilterByActive(true);

        result.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void FilterByActive_WhenContextReturnsInactiveEntities_MustReturnNoEntities()
    {
        var ctx = CreateContext();
        var service = new UserService(ctx);
        _ = SetupUsers(ctx, isActive: false);

        var result = service.FilterByActive(true);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_DeletesAndReturnsTrue()
    {
        var ctx = CreateContext();
        var service = new UserService(ctx);
        var users = SetupUsers(ctx);
        var existing = users.First();

        var result = await service.DeleteAsync(existing.Id);

        result.Should().BeTrue();
        ctx.Users!.Any(u => u.Id == existing.Id).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenUserMissing_ReturnsFalseAndDoesNotDelete()
    {
        var ctx = CreateContext();
        var service = new UserService(ctx);
        _ = SetupUsers(ctx);

        var result = await service.DeleteAsync(999);

        result.Should().BeFalse();
    }

    private static IQueryable<User> SetupUsers(DataContext ctx, string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true)
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

        ctx.Users!.AddRange(users);
        ctx.SaveChanges();
        return ctx.Users.AsQueryable();
    }

    private static DataContext CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var opts = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new DataContext(opts);
        ctx.Database.EnsureCreated();

        // Clear seeded data to provide isolated test state
        ctx.Users?.RemoveRange(ctx.Users);
        ctx.UserLogs?.RemoveRange(ctx.UserLogs);
        ctx.SaveChanges();

        return ctx;
    }
}
