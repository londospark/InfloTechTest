using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
        => services.AddScoped<IDataContext, DataContext>();
}
