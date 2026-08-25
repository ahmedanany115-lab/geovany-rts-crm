using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RTSErp.Api.Extensions;
using RTSErp.Api.Middleware;
using RTSErp.Application;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Infrastructure;
using RTSErp.Infrastructure.Persistence;
using RTSErp.Infrastructure.Persistence.Seed;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ── Layer composition ─────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwtSupport();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required: refresh token is an httpOnly cookie
    });
});

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Auto-migrate and seed on startup (all environments) ───────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var umgr   = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var rmgr   = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, umgr, rmgr, logger);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database migration/seed failed on startup.");
    // Don't crash the app — it may still serve requests if DB was already migrated.
}

// ── Swagger (all environments) ────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

// ── Health endpoint ───────────────────────────────────────────────────────────
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { status = "RTS ERP API", version = "2.0" }));

app.MapControllers();

app.Run();
