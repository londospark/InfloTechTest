using System;
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

        // Check if tests want to skip data access registration
        var skipRegistration = builder.Configuration["SkipDataAccessRegistration"];
        if (string.Equals(skipRegistration, "true", StringComparison.OrdinalIgnoreCase))
        {
            // Tests will handle their own DataContext registration
            // Still register IDataContext mapping for when tests register DataContext later
            services.AddScoped<IDataContext>(sp => sp.GetRequiredService<DataContext>());
            return builder;
        }

        // Detect whether a DataContext/DbContextOptions registration already exists
        var hasDbContextRegistration = services.Any(d => d.ServiceType == typeof(DbContextOptions<DataContext>)
                                                         || d.ServiceType == typeof(DataContext));

        // If nothing registered and connection string present, register SQL Server provider
        if (!hasDbContextRegistration)
        {
            var conn = builder.Configuration.GetConnectionString("UserManagement")
                       ?? builder.Configuration["ConnectionStrings:UserManagement"];

            if (!string.IsNullOrEmpty(conn))
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
