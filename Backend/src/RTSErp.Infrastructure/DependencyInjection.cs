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

        // Build a NpgsqlDataSource so we can configure socket-level settings.
        // "No IPv6=true" in the connection string tells Npgsql to only use IPv4
        // when resolving hostnames — Railway containers have no IPv6 route.
        var dataSource = new Npgsql.NpgsqlDataSourceBuilder(connectionString).Build();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(dataSource, npgsql =>
            {
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

        services.AddHostedService<DatabaseSeedingService>();

        return services;
    }

    /// <summary>
    /// Converts postgres:// or postgresql:// URIs to Npgsql ADO.NET key=value format.
    /// Also appends safe defaults for Supabase connectivity.
    ///
    /// IMPORTANT: Do NOT append Pooling=false — that breaks EnableRetryOnFailure
    /// (Npgsql requires pooling when retries are enabled). Supabase's PgBouncer
    /// handles connection pooling at the proxy level.
    ///
    /// Use port 5432 (Session Mode or Direct), NOT 6543 (Transaction Mode).
    /// Transaction Mode pooler does not support DDL (CREATE TABLE).
    /// </summary>
    public static string NormalizePostgresConnectionString(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        raw = raw.Trim();

        string kv;

        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
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
                kv = $"Host={host};Port={port};Database={database};Username={username};Password={safePassword};SSL Mode=Require;Trust Server Certificate=true;";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DI] Could not parse PostgreSQL URI: {ex.Message}");
                kv = raw;
            }
        }
        else
        {
            kv = raw;
        }

        // Append command timeout if not already specified
        if (!kv.Contains("Command Timeout", StringComparison.OrdinalIgnoreCase) &&
            !kv.Contains("CommandTimeout", StringComparison.OrdinalIgnoreCase))
            kv += "Command Timeout=120;";

        // Force IPv4 — Railway containers have no IPv6 route.
        // Without this, Npgsql resolves Supabase hostnames to IPv6 addresses
        // and gets SocketException 101 (Network is unreachable).
        if (!kv.Contains("No IPv6", StringComparison.OrdinalIgnoreCase))
            kv += "No IPv6=true;";

        return kv;
    }
}
