var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddSqlServer("SQL-Server")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("UserManagement");

var api = builder.AddProject<Projects.UserManagement_Web>("API")
    .WithUrlForEndpoint("https", url => url.DisplayText = "Home")
    .WithUrl("/scalar", "Scalar API Reference")
    .WithReference(database)
    .WaitFor(database);

var frontend = builder.AddProject<Projects.UserManagement_Blazor>("Blazor")
    .WithUrlForEndpoint("https", url => url.DisplayText = "Home")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("ApiBaseAddress", api.GetEndpoint("https"));

api.WithEnvironment("FrontendOrigin", frontend.GetEndpoint("https"));

builder.Build().Run();
