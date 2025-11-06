using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Westwind.AspNetCore.Markdown;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services
    .AddDataAccess()
    .AddDomainServices()
    .AddMarkdown()
    .AddControllersWithViews();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseMarkdown();

app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
