using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using UserManagement.Data;
using UserManagement.Shared.DTOs;
using Xunit;
using FluentAssertions;

namespace UserManagement.Web.Tests.Integration;

public class ApiIntegrationTests(WebApplicationFactory<Program> Factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_ListEndpoint_DoesNotPersistUserLogs()
    {
        // Arrange: use unique shared in-memory SQLite DB per test so tests can run in parallel
        var dbName = Guid.NewGuid().ToString("N");
        var connectionString = $"Data Source=file:memdb-{dbName}?mode=memory&cache=shared";
        var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Set environment variable BEFORE creating the factory so it's available during startup
        Environment.SetEnvironmentVariable("SkipDataAccessRegistration", "true");

        try
        {
            var factory = Factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // remove existing DataContext registrations if any
                    var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<DataContext>) || d.ServiceType == typeof(DataContext)).ToList();
                    foreach (var d in descriptors)
                        services.Remove(d);

                    // add SQLite in-memory DbContext using the open connection
                    services.AddDbContext<DataContext>(opts => opts.UseSqlite(connection));
                });
            });

            // Ensure DB schema is created using a local DataContext instance to avoid touching the host's DataContext
            var localOpts = new DbContextOptionsBuilder<DataContext>().UseSqlite(connection).Options;
            using (var ctx = new DataContext(localOpts))
            {
                ctx.Database.EnsureCreated();
            }

            var client = factory.CreateClient();

            // Act
            var resp = await client.GetAsync("/api/users");

            // Assert
            resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);

            // Check DB for logs using local DataContext against the same connection
            using (var ctx2 = new DataContext(localOpts))
            {
                var logs = await ctx2.UserLogs!.ToListAsync();
                logs.Should().BeEmpty();
            }
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable("SkipDataAccessRegistration", null);
            connection.Close();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Post_CreateEndpoint_PersistsUserLog()
    {
        // Arrange: unique SQLite shared in-memory DB for this test
        var dbName = Guid.NewGuid().ToString("N");
        var connectionString = $"Data Source=file:memdb-{dbName}?mode=memory&cache=shared";
        var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Set environment variable BEFORE creating the factory
        Environment.SetEnvironmentVariable("SkipDataAccessRegistration", "true");

        try
        {
            var factory = Factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // remove existing DataContext registrations if any
                    var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<DataContext>) || d.ServiceType == typeof(DataContext)).ToList();
                    foreach (var d in descriptors)
                        services.Remove(d);

                    services.AddDbContext<DataContext>(opts => opts.UseSqlite(connection));
                });
            });

            // Ensure DB schema is created using a local DataContext instance
            var localOpts = new DbContextOptionsBuilder<DataContext>().UseSqlite(connection).Options;
            using (var ctx = new DataContext(localOpts))
            {
                ctx.Database.EnsureCreated();
            }

            var client = factory.CreateClient();

            var req = new CreateUserRequestDto("IntApi", "Test", "intapi@test.com", new System.DateTime(1990,1,1), true);
            var json = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var resp = await client.PostAsync("/api/users", content);

            // Assert
            resp.StatusCode.Should().Be(HttpStatusCode.Created);

            using (var ctx2 = new DataContext(localOpts))
            {
                var logs = await ctx2.UserLogs!.ToListAsync();
                logs.Should().NotBeEmpty();
                logs.First().Message.Should().Contain("Created user id");
            }
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable("SkipDataAccessRegistration", null);
            connection.Close();
            connection.Dispose();
        }
    }
}
