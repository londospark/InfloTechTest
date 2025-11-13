using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using UserManagement.Data;
using UserManagement.Data.Extensions;
using UserManagement.ServiceDefaults;
using UserManagement.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services
    .AddOpenApi()
    .AddDomainServices()
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
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
}

app.MapDefaultEndpoints();

app.UseHsts();
app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();

app.UseCors(corsPolicyName);
app.MapControllers();

app.Run();
