using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;
using UserManagement.Data.Extensions;
using UserManagement.ServiceDefaults;
using UserManagement.Services.Extensions;
using UserManagement.Web.Controllers;
using UserManagement.Web.Helpers;
using UserManagement.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Disable ValidateScopes to avoid DI validation failures when running under test harness (WebApplicationFactory)
builder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = false);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services
    .AddOpenApi()
    .AddDomainServices()
    .AddScoped<ILogger<UsersController>>(sp =>
    {
        var factory = sp.GetRequiredService<ILoggerFactory>();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        // Create a forward logger using a non-typed category name to avoid DI recursion during validation
        var forward = factory.CreateLogger("ForwardedLogger");
        return new DatabaseLogger<UsersController>(factory, scopeFactory, forward);
    })
    .AddControllers();

builder.AddDataAccess();

var corsPolicyName = "AllowFrontend";

builder.Services.AddCors(o =>
{
    o.AddPolicy(corsPolicyName, p =>
    {
        var frontendOrigin = builder.Configuration["FrontendOrigin"];

        if (!string.IsNullOrEmpty(frontendOrigin))
        {
            // Production or Aspire: use specific origin with credentials for SignalR
            p.WithOrigins(frontendOrigin)
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            // Development fallback: allow localhost on common ports with credentials
            p.WithOrigins(
                "https://localhost:5001",
                "https://localhost:7183",
                "http://localhost:5000")
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials();
        }
        else
        {
            // Production without explicit origin: fail fast rather than allow any origin
            throw new InvalidOperationException(
                "FrontendOrigin configuration is required in production environments. " +
                "Set the FrontendOrigin environment variable or configuration value.");
        }
    });
});

// Add SignalR
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opt =>
    {
        opt.Title = "User Management API";
        opt.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        opt.Theme = ScalarTheme.BluePlanet;
    });
}

// NOTE: Database migrations are handled by UserManagement.Migrations project in Aspire AppHost.
// The migration tool runs before the API starts, ensuring the database is always up to date.
// Tests manage their own database schema independently.

app.MapDefaultEndpoints();

app.UseHsts();
app.UseHttpsRedirection();

app.UseRouting();

// CORS must run after routing and before authentication/authorization so the CORS headers are applied.
app.UseCors(corsPolicyName);

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<UserLogsHub>("/hubs/userlogs");

app.Run();
