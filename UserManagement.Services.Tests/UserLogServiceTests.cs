using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;
using Microsoft.Data.Sqlite;

namespace UserManagement.Services.Tests;

public class UserLogServiceTests
{
    [Fact]
    public void GetAll_WhenContextReturnsEntities_MustReturnSameEntities()
    {
        var ctx = CreateContext();
        var service = new UserLogService(ctx);
        var logs = SetupLogs(ctx);

        var result = service.GetAll();

        result.Should().BeSameAs(logs);
    }

    [Fact]
    public async Task AddAsync_WhenCalled_CreatesAndReturnsEntity()
    {
        var ctx = CreateContext();
        var service = new UserLogService(ctx);
        var log = new UserLog { UserId = 1, Message = "Created", CreatedAt = System.DateTime.UtcNow };

        var result = await service.AddAsync(log);

        result.Should().BeSameAs(log);
        // Check by properties rather than reference equality in query
        ctx.UserLogs!.Any(l => l.UserId == log.UserId && l.Message == log.Message).Should().BeTrue();
    }

    [Fact]
    public void GetByUserId_WhenLogsExistForUser_ReturnsThoseLogs()
    {
        var ctx = CreateContext();
        var service = new UserLogService(ctx);
        var logs = SetupLogs(ctx, userId: 42);

        var result = service.GetByUserId(42);

        result.Should().BeEquivalentTo(logs);
    }

    [Fact]
    public void GetByUserId_WhenNoLogsForUser_ReturnsEmpty()
    {
        var ctx = CreateContext();
        var service = new UserLogService(ctx);
        _ = SetupLogs(ctx, userId: 7);

        var result = service.GetByUserId(99);

        result.Should().BeEmpty();
    }

    private IQueryable<UserLog> SetupLogs(DataContext ctx, long userId = 1, string message = "Test", bool setCreated = true)
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

        ctx.UserLogs!.AddRange(logs);
        ctx.SaveChanges();
        return ctx.UserLogs.AsQueryable();
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
        return ctx;
    }
}
