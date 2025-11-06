using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.UserManagement_Web>("usermanagement-web");

builder.Build().Run();
