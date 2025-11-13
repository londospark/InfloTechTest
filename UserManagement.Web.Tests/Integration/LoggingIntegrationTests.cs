using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserManagement.Data;
using UserManagement.Services.Implementations;
using UserManagement.Services.Interfaces;
using UserManagement.Web.Controllers;
using UserManagement.Web.Helpers;

namespace UserManagement.Web.Tests.Integration;

public class LoggingIntegrationTests
{
    [Fact]
    public async Task CreateEndpoint_ShouldPersistUserLog_AndForwardNonPersistedLogs()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder => builder.AddConsole());

        // In-memory EF DbContext
        services.AddDbContext<DataContext>(opt => opt.UseInMemoryDatabase("int-test-logs"));
        services.AddScoped<IDataContext>(sp => sp.GetRequiredService<DataContext>());

        // Domain services
        services.AddScoped<IUserLogService, UserLogService>();
        services.AddScoped<IUserService, UserService>();

        var sp = services.BuildServiceProvider();

        // Prepare logger factory and forward logger
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var forward = loggerFactory.CreateLogger("ForwardedLogger");

        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        // Create DatabaseLogger using the real scope factory so it resolves IUserLogService per-scope
        var dbLogger = new DatabaseLogger<UsersController>(loggerFactory, scopeFactory, forward);

        // Create controller with real services
        var userService = sp.GetRequiredService<IUserService>();
        var userLogService = sp.GetRequiredService<IUserLogService>();
        var controller = new UsersController(userService, userLogService, dbLogger);

        var req = new Shared.DTOs.CreateUserRequestDto("Int", "Test", "int@test.com", new System.DateTime(1990, 1, 1), true);

        // Act: call Create
        var action = controller.Create(req);

        // Assert: a UserLog was persisted in the in-memory DB
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<DataContext>();
        var logs = await ctx.UserLogs!.ToListAsync();
        logs.Should().NotBeEmpty();
        logs.Should().Contain(l => l.Message.Contains("Created user id"));

        // And forward logger exists and is functional
        forward.Should().NotBeNull();
    }
}
