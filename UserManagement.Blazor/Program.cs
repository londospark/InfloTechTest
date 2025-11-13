using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using UserManagement.Blazor;
using UserManagement.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["ApiBaseAddress"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new(apiBase) });

// Strongly-typed API client
builder.Services.AddScoped<IUsersClient, UsersClient>();

// User logs SignalR service - use scoped lifetime to match HttpClient
builder.Services.AddScoped<IUserLogsService, UserLogsService>(sp => 
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    return new UserLogsService(httpClient);
});

await builder.Build().RunAsync();
