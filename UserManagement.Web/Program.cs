using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserManagement.Data.Extensions;
using UserManagement.ServiceDefaults;
using UserManagement.Services.Extensions;
using Westwind.AspNetCore.Markdown;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services
    .AddOpenApi()
    .AddDomainServices()
    .AddMarkdown()
    .AddControllers();

builder.AddDataAccess();

var corsPolicyName = "AllowFrontend";

builder.Services.AddCors(o =>
{
    o.AddPolicy(corsPolicyName, p =>
    {
        if (builder.Environment.IsDevelopment())
        {
            p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
        }
        else
        {
            p.AllowAnyHeader().AllowAnyMethod().WithOrigins(
                builder.Configuration["FrontendOrigin"] ?? "http://localhost:0"
            );
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();

app.UseMarkdown();

app.UseHsts();
app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();

app.UseCors(corsPolicyName);
app.MapControllers();

app.Run();
