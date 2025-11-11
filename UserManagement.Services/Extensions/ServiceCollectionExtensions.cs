using Microsoft.Extensions.DependencyInjection;
using UserManagement.Services.Implementations;
using UserManagement.Services.Interfaces;

namespace UserManagement.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
        => services.AddScoped<IUserService, UserService>();
}
