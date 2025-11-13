using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace UserManagement.Data.Extensions;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddDataAccess(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Detect whether a DataContext/DbContextOptions registration already exists
        var hasDbContextRegistration = services.Any(d => d.ServiceType == typeof(Microsoft.EntityFrameworkCore.DbContextOptions<DataContext>)
                                                         || d.ServiceType == typeof(DataContext));

        // If nothing registered, and connection string present and not development, register SQL Server provider
        if (!hasDbContextRegistration)
        {
            var conn = builder.Configuration.GetConnectionString("UserManagement")
                       ?? builder.Configuration["ConnectionStrings:UserManagement"];

            if (!string.IsNullOrEmpty(conn) && !builder.Environment.IsDevelopment())
            {
                services.AddDbContext<DataContext>(opts => opts.UseSqlServer(conn));
                hasDbContextRegistration = true;
            }
        }

        // ALWAYS add IDataContext mapping - even if DataContext isn't registered yet,
        // tests may register it after calling AddDataAccess
        services.AddScoped<IDataContext>(sp => sp.GetRequiredService<DataContext>());

        return builder;
    }
}
