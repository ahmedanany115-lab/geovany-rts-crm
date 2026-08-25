using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Infrastructure.Identity;
using RTSErp.Infrastructure.Persistence;
using RTSErp.Infrastructure.Services;

namespace RTSErp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var rawCs = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var connectionString = NormalizePostgresConnectionString(rawCs);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                npgsql.CommandTimeout(120);
            }));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.User.RequireUniqueEmail = true;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAccountingService, AccountingService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IEInvoiceService, MockEInvoiceService>();

        return services;
    }

    /// <summary>
    /// Supabase (and Railway) expose the connection string as a postgres:// URI.
    /// Npgsql requires the ADO.NET key=value format. This method converts either form
    /// so both work transparently — key=value strings pass through unchanged.
    /// </summary>
    internal static string NormalizePostgresConnectionString(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        raw = raw.Trim();

        // Already a key=value string — pass through unchanged
        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return raw;

        // Parse the URI manually to avoid any platform-specific Uri class quirks
        // and to handle special characters in passwords correctly.
        // Format: postgres://user:password@host:port/database[?params]
        try
        {
            var uri = new Uri(raw);

            var host     = uri.Host;
            var port     = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/').Split('?')[0];

            // UserInfo is "user:password" — password may contain special chars,
            // Uri.UserInfo URL-decodes them for us automatically.
            var userInfo = uri.UserInfo.Split(':', 2);
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;

            // Build a proper Npgsql key=value string.
            // We avoid NpgsqlConnectionStringBuilder here because it would throw
            // on the original URI format — that's exactly the error we're fixing.
            var builder = new System.Text.StringBuilder();
            builder.Append($"Host={host};");
            builder.Append($"Port={port};");
            builder.Append($"Database={database};");
            builder.Append($"Username={username};");
            // Escape semicolons in the password (the only character that breaks KV format)
            builder.Append($"Password={password.Replace(";", "\\;")};");
            builder.Append("SSL Mode=Require;");
            builder.Append("Trust Server Certificate=true;");

            return builder.ToString();
        }
        catch (Exception ex)
        {
            // Return raw and let EF produce a clearer error
            Console.Error.WriteLine(
                $"[DependencyInjection] Failed to parse PostgreSQL URI: {ex.Message}. " +
                "Using raw connection string as-is.");
            return raw;
        }
    }
}

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.User.RequireUniqueEmail = true;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAccountingService, AccountingService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IEInvoiceService, MockEInvoiceService>();

        return services;
    }
}
