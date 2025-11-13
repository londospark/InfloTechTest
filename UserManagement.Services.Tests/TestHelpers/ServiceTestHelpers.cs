using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;

namespace UserManagement.Services.Tests.TestHelpers;

/// <summary>
/// Helper methods for creating test databases and contexts in service tests
/// </summary>
public static class ServiceTestHelpers
{
    /// <summary>
    /// Creates an in-memory SQLite DataContext for service testing
    /// </summary>
    public static DataContext CreateContext()
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

    /// <summary>
    /// Adds test users to the context and returns them as a queryable
    /// </summary>
    public static IQueryable<User> SetupUsers(
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
        
        return ctx.Users.AsQueryable();
    }

    /// <summary>
    /// Adds multiple test users with different properties
    /// </summary>
    public static IQueryable<User> SetupMultipleUsers(DataContext ctx, params (string forename, string surname, string email, bool isActive)[] users)
    {
        var entities = users.Select(u => new User
        {
            Forename = u.forename,
            Surname = u.surname,
            Email = u.email,
            IsActive = u.isActive,
            DateOfBirth = new DateTime(1990, 1, 1)
        }).ToArray();

        ctx.Users!.AddRange(entities);
        ctx.SaveChanges();
        
        return ctx.Users.AsQueryable();
    }
}
