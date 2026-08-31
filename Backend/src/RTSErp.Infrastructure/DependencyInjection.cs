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
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

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

        services.AddHostedService<DatabaseSeedingService>();

        return services;
    }

    /// <summary>
    /// Converts a postgres:// or postgresql:// URI to Npgsql ADO.NET key=value format.
    /// Key=value strings are returned unchanged. Called by DatabaseSeedingService
    /// to get the same connection string format for raw DDL statements.
    /// </summary>
    public static string NormalizePostgresConnectionString(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        raw = raw.Trim();

        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return raw;

        try
        {
            var uri      = new Uri(raw);
            var host     = uri.Host;
            var port     = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/').Split('?')[0];

            var userInfo = uri.UserInfo.Split(':', 2);
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;

            var safePassword = password.Replace(";", "\\;");
            return $"Host={host};Port={port};Database={database};Username={username};Password={safePassword};SSL Mode=Require;Trust Server Certificate=true;";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DI] Could not parse PostgreSQL URI: {ex.Message}");
            return raw;
        }
    }
}
