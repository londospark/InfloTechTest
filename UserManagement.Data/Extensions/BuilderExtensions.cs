using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UserManagement.Data.Extensions;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddDataAccess(this IHostApplicationBuilder builder)
    {
        builder.AddSqlServerDbContext<DataContext>("UserManagement");

        builder.Services.AddScoped<IDataContext>(sp => sp.GetRequiredService<DataContext>());
        return builder;
    }
}
