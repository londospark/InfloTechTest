using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UserManagement.Data;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Get connection string from configuration (will be injected by Aspire)
var connectionString = builder.Configuration.GetConnectionString("UserManagement");

if (string.IsNullOrEmpty(connectionString))
{
    Console.Error.WriteLine("ERROR: Connection string 'UserManagement' not found in configuration.");
    return 1;
}

Console.WriteLine("=== Database Migration Tool ===");
Console.WriteLine($"Connection string: {MaskConnectionString(connectionString)}");

// Register DbContext with SQL Server
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(300); // 5 minutes for large migrations
    }));

var app = builder.Build();

try
{
    Console.WriteLine("Applying database migrations...");
    
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    
    // Check for pending migrations
    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        Console.WriteLine($"Found {pendingMigrations.Count()} pending migration(s):");
        foreach (var migration in pendingMigrations)
        {
            Console.WriteLine($"  - {migration}");
        }
        
        Console.WriteLine("Applying migrations...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("✓ Migrations applied successfully!");
    }
    else
    {
        Console.WriteLine("✓ Database is already up to date. No migrations to apply.");
    }
    
    // Verify database connection
    var canConnect = await dbContext.Database.CanConnectAsync();
    if (!canConnect)
    {
        Console.Error.WriteLine("ERROR: Cannot connect to database after migration.");
        return 1;
    }
    
    Console.WriteLine("✓ Database connection verified.");
    Console.WriteLine("=== Migration completed successfully ===");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: Migration failed: {ex.Message}");
    Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
    return 1;
}

// Helper method to mask sensitive information in connection string
static string MaskConnectionString(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return string.Empty;
    
    // Simple masking - hide password
    var parts = connectionString.Split(';');
    var masked = parts.Select(part =>
    {
        if (part.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            part.Contains("Pwd", StringComparison.OrdinalIgnoreCase))
        {
            var keyValue = part.Split('=');
            return keyValue.Length == 2 ? $"{keyValue[0]}=****" : part;
        }
        return part;
    });
    
    return string.Join(";", masked);
}
