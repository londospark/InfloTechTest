using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.Data.Tests.TestHelpers;

/// <summary>
/// Helper methods for creating test DataContext instances
/// </summary>
public static class DataContextTestHelpers
{
    /// <summary>
    /// Creates an in-memory SQLite DataContext with a unique connection
    /// </summary>
    public static DataContext CreateInMemoryContext()
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
    /// Creates an in-memory SQLite DataContext with a specific database name (shared connection)
    /// </summary>
    public static DataContext CreateInMemoryContext(string databaseName)
    {
        var connectionString = $"Data Source=file:{databaseName}?mode=memory&cache=shared";
        var connection = new SqliteConnection(connectionString);
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
}
