# Test Helpers Documentation

This document describes the test helper classes that have been extracted from common patterns found across the test suite. These helpers reduce code duplication and make tests more maintainable.

## UserManagement.Web.Tests Helpers

### DatabaseHelpers

Located in `UserManagement.Web.Tests\TestHelpers\DatabaseHelpers.cs`

Provides helper methods for creating test databases and contexts using SQLite in-memory databases.

#### Methods:

- **CreateInMemorySqliteContext()**: Creates an in-memory SQLite DataContext with a unique connection per instance. Ideal for isolated unit tests.

- **CreateSharedMemoryConnection()**: Creates a shared in-memory SQLite connection that can be used across multiple contexts. Useful for integration tests.

- **CreateContext(SqliteConnection)**: Creates a DataContext using an existing SQLite connection.

- **CreateSharedMemoryContext()**: Creates a DataContext with a unique shared in-memory database.

- **SeedUsers(...)**: Adds test users to the context and saves changes. Accepts optional parameters for forename, surname, email, isActive, and dateOfBirth.

- **SeedUserLog(...)**: Adds a test user log to the context.

- **ClearData(DataContext)**: Clears all data from Users and UserLogs tables.

#### Example Usage:

```csharp
[Fact]
public async Task MyTest()
{
    // Create an in-memory context
    var ctx = DatabaseHelpers.CreateInMemorySqliteContext();
    
    // Seed with test data
    var users = DatabaseHelpers.SeedUsers(ctx, "John", "Doe", "john@example.com");
    
    // Your test logic...
    
    // Clean up
    ctx.Dispose();
}
```

### MockLogger<T>

Located in `UserManagement.Web.Tests\TestHelpers\MockLogger.cs`

A lightweight test logger that captures log messages for verification in tests.

#### Methods:

- **AsILogger()**: Returns the logger as an ILogger<T> instance.
- **LogContains(LogLevel, string)**: Checks if any log entry matches the level and contains the specified text.
- **AnyLogContains(string)**: Checks if any log entry contains the specified text regardless of level.
- **GetLogs()**: Returns all captured log entries.
- **GetLogs(LogLevel)**: Returns log entries for a specific log level.
- **Clear()**: Clears all captured logs.

#### Example Usage:

```csharp
[Fact]
public async Task MyControllerTest()
{
    var logger = new MockLogger<UsersController>();
    var controller = new UsersController(service, logService, logger.AsILogger());
    
    await controller.Create(request);
    
    logger.LogContains(LogLevel.Information, "Created user").Should().BeTrue();
}
```

### FakeLogger & FakeLoggerFactory

Located in `UserManagement.Web.Tests\TestHelpers\FakeLogger.cs`

Fake logger implementations for testing scenarios where you need to track logging activity.

#### FakeLoggerFactory Methods:

- **CreateLogger(string)**: Creates a logger for the specified category.
- **GetLogger()**: Returns the underlying FakeLogger instance.

#### FakeLogger Properties:

- **Logged**: Boolean indicating if any logging occurred.
- **LastLevel**: The log level of the last log entry.
- **LastState**: The state object of the last log entry.
- **LastException**: The exception from the last log entry.
- **Messages**: List of all logged messages with their levels.

#### Example Usage:

```csharp
[Fact]
public void MyTest()
{
    var factory = new FakeLoggerFactory();
    var dbLogger = new DatabaseLogger<UsersController>(factory, scopeFactory, null);
    
    dbLogger.LogInformation("test");
    
    factory.GetLogger().Logged.Should().BeTrue();
}
```

### MockServiceScopeHelpers

Located in `UserManagement.Web.Tests\TestHelpers\MockServiceScopeHelpers.cs`

Helper methods for creating mock service scopes for dependency injection testing.

#### Methods:

- **CreateMockScopeFactory(IServiceProvider)**: Creates a mock IServiceScopeFactory with the provided service provider.
- **CreateMockScopeFactory(ServiceCollection)**: Creates a mock IServiceScopeFactory with services built from the provided collection.
- **CreateMockScopeFactory<TService>(TService)**: Creates a mock IServiceScopeFactory with a single service registered.
- **CreateStrictMockScopeFactory()**: Creates a strict mock that expects no calls (for negative testing).

#### Example Usage:

```csharp
[Fact]
public void MyTest()
{
    var userLogService = new Mock<IUserLogService>();
    var scopeFactory = MockServiceScopeHelpers.CreateMockScopeFactory(userLogService.Object);
    
    var logger = new DatabaseLogger<UsersController>(loggerFactory, scopeFactory.Object, null);
    // ...
}
```

### IntegrationTestHelpers

Located in `UserManagement.Web.Tests\TestHelpers\IntegrationTestHelpers.cs`

Helper methods for integration testing with WebApplicationFactory.

#### Methods:

- **CreateFactoryWithSqlite(WebApplicationFactory, SqliteConnection)**: Creates a WebApplicationFactory configured with an in-memory SQLite database.
- **CreateFactoryWithNewSqlite(WebApplicationFactory)**: Creates a WebApplicationFactory with a new shared in-memory SQLite database. Returns both the factory and connection.

#### Classes:

- **StubHttpMessageHandler**: Stub HTTP message handler for testing HttpClient-based services.

#### Example Usage:

```csharp
[Fact]
public async Task MyIntegrationTest()
{
    var (factory, connection) = IntegrationTestHelpers.CreateFactoryWithNewSqlite(baseFactory);
    var client = factory.CreateClient();
    
    var response = await client.GetAsync("/api/users");
    
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    connection.Close();
    connection.Dispose();
}
```

## UserManagement.Blazor.Tests Helpers

### TestNavigationManager

Located in `UserManagement.Blazor.Tests\TestHelpers\TestNavigationManager.cs`

Test NavigationManager for unit testing Blazor components.

#### Example Usage:

```csharp
[Fact]
public void MyBlazorTest()
{
    var nav = new TestNavigationManager();
    Services.AddSingleton<NavigationManager>(nav);
    
    var cut = Render<MyComponent>();
    cut.Find("form").Submit();
    
    nav.Uri.Should().EndWith("/users");
}
```

### Fake IUsersClient Implementations

Located in `UserManagement.Blazor.Tests\TestHelpers\FakeUsersClient.cs`

Multiple fake implementations of IUsersClient for different testing scenarios:

- **FakeUsersClient**: Base implementation with default behavior returning sample data.
- **FakeUsersClientEmpty**: Returns empty results.
- **FakeUsersClientPaging**: Supports pagination testing with pre-defined logs.
- **FakeUsersClientForDifferentUsers**: Returns different logs for different users.
- **FakeUsersClientThrow**: Throws exceptions to test error handling.
- **FakeUsersClientWithInvalidLog**: Returns logs with invalid/missing data.

#### Example Usage:

```csharp
[Fact]
public async Task MyComponentTest()
{
    var fakeClient = new FakeUsersClient();
    Services.AddScoped<IUsersClient>(_ => fakeClient);
    
    var cut = Render<ActivityLogs>(parameters => parameters
        .Add(p => p.UserId, 123L));
    
    cut.Markup.Should().Contain("Test log message");
}
```

### HttpTestHelpers

Located in `UserManagement.Blazor.Tests\TestHelpers\FakeUsersClient.cs`

Helper methods for creating test HTTP responses.

#### Methods:

- **CreateJsonResponse<T>(T, HttpStatusCode)**: Creates an HTTP response with JSON content.
- **CreateEmptyResponse(HttpStatusCode)**: Creates an empty HTTP response.
- **CreateNullResponse(HttpStatusCode)**: Creates an HTTP response with null content.

### FakeUserLogsService & FakeUserLogsServiceWithTracking

Located in `UserManagement.Blazor.Tests\TestHelpers\FakeUserLogsService.cs`

Fake implementations of IUserLogsService for testing SignalR functionality.

- **FakeUserLogsService**: Base implementation with no-op methods.
- **FakeUserLogsServiceWithTracking**: Tracks lifecycle calls (Started, Stopped, JoinedUser, LeftUser).

#### Example Usage:

```csharp
[Fact]
public async Task MySignalRTest()
{
    var fakeLogsService = new FakeUserLogsService();
    Services.AddScoped<IUserLogsService>(_ => fakeLogsService);
    
    var cut = Render<ActivityLogs>(parameters => parameters
        .Add(p => p.UserId, 123L));
    
    // Simulate SignalR message
    var newLog = new UserLogDto(999, 123, "New log", DateTime.UtcNow);
    fakeLogsService.Raise(newLog);
    
    cut.Markup.Should().Contain("New log");
}
```

## UserManagement.Services.Tests Helpers

### ServiceTestHelpers

Located in `UserManagement.Services.Tests\TestHelpers\ServiceTestHelpers.cs`

Helper methods for service layer testing.

#### Methods:

- **CreateContext()**: Creates an in-memory SQLite DataContext for service testing with cleared seed data.
- **SetupUsers(...)**: Adds test users and returns them as an IQueryable.
- **SetupMultipleUsers(...)**: Adds multiple test users with different properties.

#### Example Usage:

```csharp
[Fact]
public async Task MyServiceTest()
{
    var ctx = ServiceTestHelpers.CreateContext();
    var service = new UserService(ctx);
    var users = ServiceTestHelpers.SetupUsers(ctx, isActive: true);
    
    var result = service.FilterByActive(true);
    
    result.Should().BeEquivalentTo(users);
}
```

## UserManagement.Data.Tests Helpers

### DataContextTestHelpers

Located in `UserManagement.Data.Tests\TestHelpers\DataContextTestHelpers.cs`

Helper methods for data layer testing using SQLite in-memory databases.

#### Methods:

- **CreateInMemoryContext()**: Creates an in-memory SQLite DataContext with a unique connection. Each call creates a completely isolated database instance.

- **CreateInMemoryContext(string)**: Creates an in-memory SQLite DataContext with a specific database name using a shared connection. Useful when you need multiple contexts to share the same data.

- **CreateSharedMemoryConnection()**: Creates a shared in-memory SQLite connection for use across multiple contexts. Returns the connection that should be properly disposed after use.

- **CreateContext(SqliteConnection)**: Creates a DataContext using an existing SQLite connection. Useful for scenarios where you need fine control over the connection lifecycle.

#### Example Usage:

```csharp
[Fact]
public async Task MyDataTest()
{
    var context = DataContextTestHelpers.CreateInMemoryContext();
    
    var entity = new User 
    { 
        Forename = "Test", 
        Surname = "User", 
        Email = "test@example.com",
        DateOfBirth = new DateTime(1990, 1, 1)
    };
    await context.CreateAsync(entity);
    
    var result = context.GetAll<User>();
    
    result.Should().Contain(s => s.Email == entity.Email);
}
```

#### Example with Shared Connection:

```csharp
[Fact]
public async Task MySharedContextTest()
{
    var connection = DataContextTestHelpers.CreateSharedMemoryConnection();
    
    // Create two contexts sharing the same database
    var context1 = DataContextTestHelpers.CreateContext(connection);
    var context2 = DataContextTestHelpers.CreateContext(connection);
    
    // Data added via context1 is visible in context2
    await context1.CreateAsync(new User { ... });
    var result = context2.GetAll<User>();
    
    result.Should().NotBeEmpty();
    
    connection.Close();
    connection.Dispose();
}
```

## Benefits

1. **Reduced Code Duplication**: Common patterns are now centralized in helper classes.
2. **Improved Maintainability**: Changes to test infrastructure only need to be made in one place.
3. **Better Readability**: Tests are more focused on the actual test logic rather than setup code.
4. **Consistency**: All tests use the same infrastructure patterns.
5. **Easier Onboarding**: New developers can quickly understand and use the established patterns.

## Migration Guide

To update existing tests to use these helpers:

1. Replace manual DataContext creation with `DatabaseHelpers.CreateInMemorySqliteContext()` or similar methods.
2. Replace custom MockLogger implementations with the provided `MockLogger<T>` class.
3. Use `FakeUsersClient` instead of creating custom fake clients in each test.
4. Replace manual service scope factory setup with `MockServiceScopeHelpers` methods.
5. Use `TestNavigationManager` for Blazor component navigation testing.

## Future Enhancements

Potential future improvements to the test helpers:

- Add builders for complex test data scenarios
- Create helpers for testing SignalR hub methods
- Add helpers for testing authentication/authorization scenarios
- Create helpers for testing file upload/download scenarios
