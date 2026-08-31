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

        // Runs schema creation + seed AFTER the app is already listening —
        // never blocks startup or causes health-check timeouts.
        services.AddHostedService<DatabaseSeedingService>();

        return services;
    }

    /// <summary>
    /// Converts a postgres:// or postgresql:// URI to Npgsql ADO.NET key=value format,
    /// then appends connection parameters required for Supabase reliability:
    ///
    ///   Pooling=false          — do not layer Npgsql pooling on top of PgBouncer;
    ///                            Supabase's pooler manages connections itself
    ///   No Reset On Close=true — skip the SET … commands Npgsql sends when returning
    ///                            a connection to its pool (not supported in transaction mode)
    ///   Command Timeout=120    — allow slow cold-start DDL statements to complete
    ///
    /// IMPORTANT — use the Session Mode pooler (port 5432) or Direct connection from Railway.
    /// The Transaction Mode pooler (port 6543) does NOT support DDL (CREATE TABLE).
    /// Supabase Dashboard → Project Settings → Database → Connection String
    ///   Session mode:   postgres://postgres.XXXX:PASSWORD@aws-0-REGION.pooler.supabase.com:5432/postgres
    ///   Direct:         postgres://postgres:PASSWORD@db.XXXX.supabase.co:5432/postgres
    /// </summary>
    internal static string NormalizePostgresConnectionString(string raw)
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

                // Semicolons in the password break key=value parsing
                var safePassword = password.Replace(";", "\\;");

                kv = $"Host={host};Port={port};Database={database};Username={username};Password={safePassword};SSL Mode=Require;Trust Server Certificate=true;";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[DI] Could not parse PostgreSQL URI: {ex.Message}. Passing raw string.");
                kv = raw;
            }
        }
        else
        {
            kv = raw;
        }

        // Append Supabase-safe Npgsql parameters if not already present
        if (!kv.Contains("Pooling=", StringComparison.OrdinalIgnoreCase))
            kv += "Pooling=false;";

        if (!kv.Contains("No Reset On Close", StringComparison.OrdinalIgnoreCase))
            kv += "No Reset On Close=true;";

        if (!kv.Contains("Command Timeout", StringComparison.OrdinalIgnoreCase) &&
            !kv.Contains("CommandTimeout", StringComparison.OrdinalIgnoreCase))
            kv += "Command Timeout=120;";

        return kv;
    }
}
