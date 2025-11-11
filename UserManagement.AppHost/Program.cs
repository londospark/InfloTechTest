var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.UserManagement_Web>("Api")
    .WithUrlForEndpoint("https", url => url.DisplayText = "Home")
    .WithUrlForEndpoint("http", url => url.DisplayText = "Home (http)");

var frontend = builder.AddProject<Projects.UserManagement_Blazor>("Blazor")
    .WithUrlForEndpoint("https", url => url.DisplayText = "Home")
    .WithReference(api)
    .WithEnvironment("ApiBaseAddress", api.GetEndpoint("https"));

api.WithEnvironment("FrontendOrigin", frontend.GetEndpoint("https"));

builder.Build().Run();
