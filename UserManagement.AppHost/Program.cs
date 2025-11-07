using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.UserManagement_Web>("usermanagement-web")
    .WithUrlForEndpoint("https", url => url.DisplayText = "Home")
    .WithUrlForEndpoint("http", url => url.DisplayText = "Home (http)");

builder.Build().Run();
