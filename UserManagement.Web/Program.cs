using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;
using UserManagement.Data;
using UserManagement.Data.Extensions;
using UserManagement.ServiceDefaults;
using UserManagement.Services.Extensions;
using UserManagement.Web.Controllers;
using UserManagement.Web.Helpers;

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
        p.AllowAnyHeader().AllowAnyMethod().WithOrigins(
            builder.Configuration["FrontendOrigin"] ?? "http://localhost:0"
        );
    });
});

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

using (var scope = app.Services.CreateScope())
{
    // Some test hosts (WebApplicationFactory) may not register DataContext; guard against that.
    var db = scope.ServiceProvider.GetService<DataContext>();
    if (db is not null)
    {
        if (db.Database.IsRelational())
            db.Database.Migrate();
        else
            db.Database.EnsureCreated();
    }
}

app.MapDefaultEndpoints();

app.UseHsts();
app.UseHttpsRedirection();

app.UseRouting();

// CORS must run after routing and before authentication/authorization so the CORS headers are applied.
app.UseCors(corsPolicyName);

app.UseAuthorization();

app.MapControllers();

app.Run();
