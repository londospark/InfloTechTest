var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddSqlServer("SQL-Server")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("UserManagement");

// Add migration runner that runs before the API starts
var migrations = builder.AddProject<Projects.UserManagement_Migrations>("Migrations")
    .WithReference(database)
    .WaitFor(database)
    .WithReplicas(1); // Only run once

var api = builder.AddProject<Projects.UserManagement_Web>("API")
    .WithUrlForEndpoint("https", url => url.DisplayText = "Home")
    .WithUrl("/scalar", "Scalar API Reference")
    .WithReference(database)
    .WaitFor(migrations); // Wait for migrations to complete before starting API

var frontend = builder.AddProject<Projects.UserManagement_Blazor>("Blazor")
    .WithUrlForEndpoint("https", url => url.DisplayText = "Home")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("ApiBaseAddress", api.GetEndpoint("https"));

api.WithEnvironment("FrontendOrigin", frontend.GetEndpoint("https"));

builder.Build().Run();
