using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Infrastructure.Persistence;
using RTSErp.Infrastructure.Persistence.Seed;

namespace RTSErp.Infrastructure.Services;

/// <summary>
/// Runs EnsureCreated + DbSeeder after the app is already listening on its port,
/// so Railway's health-check passes immediately and startup never times out.
/// Retries every 30 s until seeding succeeds (handles Supabase cold-start delays).
/// </summary>
public sealed class DatabaseSeedingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseSeedingService> _logger;

    public DatabaseSeedingService(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseSeedingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so the app finishes starting before we touch the DB.
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope   = _scopeFactory.CreateScope();
                var db     = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var umgr   = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var rmgr   = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

                _logger.LogInformation("[Seed] Ensuring database schema...");
                await db.Database.EnsureCreatedAsync(stoppingToken);

                _logger.LogInformation("[Seed] Running seeder...");
                await DbSeeder.SeedAsync(db, umgr, rmgr, _logger);

                _logger.LogInformation("[Seed] Complete.");
                return; // success — stop the background loop
            }
            catch (OperationCanceledException)
            {
                return; // app shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Seed] Failed — will retry in 30 s. Error: {Msg}", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
