using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Data;

namespace UserManagement.Web.Tests.TestHelpers;

/// <summary>
/// Helper methods for integration testing with WebApplicationFactory
/// </summary>
public static class IntegrationTestHelpers
{
    /// <summary>
    /// Creates a WebApplicationFactory configured with an in-memory SQLite database
    /// </summary>
    public static WebApplicationFactory<Program> CreateFactoryWithSqlite(
        WebApplicationFactory<Program> baseFactory,
        SqliteConnection connection) => baseFactory.WithWebHostBuilder(builder =>
                                             {
                                                 builder.ConfigureServices(services =>
                                                 {
                                                     // Remove all EF Core related registrations for DataContext
                                                     var descriptors = services
                                                         .Where(d => 
                                                             d.ServiceType == typeof(DbContextOptions<DataContext>) ||
                                                             d.ServiceType == typeof(DbContextOptions) ||
                                                             d.ServiceType == typeof(DataContext) ||
                                                             d.ServiceType == typeof(IDataContext) ||
                                                             (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                                                             (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextFactory<>)) ||
                                                             d.ServiceType == typeof(IDbContextOptions) ||
                                                             (d.ImplementationType?.Name.Contains("DataContext") ?? false))
                                                         .ToList();

                                                     foreach (var descriptor in descriptors)
                                                     {
                                                         services.Remove(descriptor);
                                                     }

                                                     // Add SQLite in-memory DbContext using the open connection
                                                     services.AddDbContext<DataContext>(opts => opts.UseSqlite(connection));
                                                     
                                                     // Re-register IDataContext mapping
                                                     services.AddScoped<IDataContext>(sp => sp.GetRequiredService<DataContext>());
                                                 });
                                             });

    /// <summary>
    /// Creates a WebApplicationFactory with a new shared in-memory SQLite database
    /// </summary>
    public static (WebApplicationFactory<Program> Factory, SqliteConnection Connection) CreateFactoryWithNewSqlite(
        WebApplicationFactory<Program> baseFactory)
    {
        var connection = DatabaseHelpers.CreateSharedMemoryConnection();
        var factory = CreateFactoryWithSqlite(baseFactory, connection);

        // Ensure DB schema is created
        var localOpts = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;

        using (var ctx = new DataContext(localOpts))
        {
            ctx.Database.EnsureCreated();
        }

        return (factory, connection);
    }

    /// <summary>
    /// Stub HTTP message handler for testing HttpClient-based services
    /// </summary>
    public sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(handler(request));
    }

    /// <summary>
    /// Creates a stub HTTP response with the specified status code
    /// </summary>
    public static HttpResponseMessage CreateStubResponse(HttpStatusCode statusCode, string? content = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content != null)
        {
            response.Content = new StringContent(content);
        }
        return response;
    }
}
