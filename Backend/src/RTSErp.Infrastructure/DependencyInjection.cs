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

        // Resolve the Supabase hostname to an explicit IPv4 address at startup.
        // Railway containers have no IPv6 route — if DNS returns an AAAA record
        // first, every Npgsql connection attempt fails with SocketException 101.
        connectionString = ResolveHostToIPv4(connectionString);

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
    /// Replaces the Host= value in an ADO.NET key=value connection string with
    /// the first IPv4 address returned by DNS. This prevents Npgsql from connecting
    /// via IPv6 on Railway, where IPv6 is unreachable (SocketException 101).
    /// Called synchronously at startup — DNS lookup is fast and only happens once.
    /// </summary>
    private static string ResolveHostToIPv4(string connectionString)
    {
        try
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var hostPart = parts.FirstOrDefault(p =>
                p.Trim().StartsWith("Host=", StringComparison.OrdinalIgnoreCase));

            if (hostPart is null) return connectionString;

            var hostname = hostPart.Trim()["Host=".Length..].Trim();

            // Already an IP address — nothing to resolve
            if (System.Net.IPAddress.TryParse(hostname, out _)) return connectionString;

            var addresses = System.Net.Dns.GetHostAddresses(hostname);
            var ipv4 = addresses.FirstOrDefault(a =>
                a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (ipv4 is null) return connectionString; // no IPv4 found — fall through

            return connectionString.Replace(
                $"Host={hostname}", $"Host={ipv4}",
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // DNS failure — return unchanged and let Npgsql handle the error
            return connectionString;
        }
    }
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

        return kv;
    }
}
