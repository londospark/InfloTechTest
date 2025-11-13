using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;

namespace UserManagement.Web.Tests.TestHelpers;

/// <summary>
/// Helper methods for creating test databases and contexts
/// </summary>
public static class DatabaseHelpers
{
    /// <summary>
    /// Creates an in-memory SQLite DataContext with a unique connection per instance
    /// </summary>
    public static DataContext CreateInMemorySqliteContext()
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

    /// <summary>
    /// Creates a shared in-memory SQLite connection for use across multiple contexts
    /// </summary>
    public static SqliteConnection CreateSharedMemoryConnection()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var connectionString = $"Data Source=file:memdb-{dbName}?mode=memory&cache=shared";
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Creates a DataContext using an existing SQLite connection
    /// </summary>
    public static DataContext CreateContext(SqliteConnection connection)
    {
        var opts = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;
        
        var ctx = new DataContext(opts);
        ctx.Database.EnsureCreated();
        
        return ctx;
    }

    /// <summary>
    /// Creates a DataContext with a unique shared in-memory database
    /// </summary>
    public static DataContext CreateSharedMemoryContext()
    {
        var dbName = $"DataSource=file:memdb_{Guid.NewGuid()}?mode=memory&cache=shared";
        var connection = new SqliteConnection(dbName);
        connection.Open();

        var opts = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;
        
        var ctx = new DataContext(opts);
        ctx.Database.EnsureCreated();
        
        return ctx;
    }

    /// <summary>
    /// Adds test users to the context and saves changes
    /// </summary>
    public static User[] SeedUsers(
        DataContext ctx,
        string forename = "Johnny",
        string surname = "User",
        string email = "juser@example.com",
        bool isActive = true,
        DateTime? dateOfBirth = null)
    {
        dateOfBirth ??= new DateTime(1990, 1, 1);
        
        var users = new[]
        {
            new User
            {
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = isActive,
                DateOfBirth = dateOfBirth.Value
            }
        };

        ctx.Users!.AddRange(users);
        ctx.SaveChanges();
        
        return users;
    }

    /// <summary>
    /// Adds a test user log to the context
    /// </summary>
    public static UserLog SeedUserLog(DataContext ctx, long userId, string message)
    {
        var log = new UserLog
        {
            UserId = userId,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
        
        ctx.UserLogs!.Add(log);
        ctx.SaveChanges();
        
        return log;
    }

    /// <summary>
    /// Clears all data from Users and UserLogs tables
    /// </summary>
    public static void ClearData(DataContext ctx)
    {
        ctx.Users?.RemoveRange(ctx.Users);
        
        ctx.UserLogs?.RemoveRange(ctx.UserLogs);
        
        ctx.SaveChanges();
    }
}
