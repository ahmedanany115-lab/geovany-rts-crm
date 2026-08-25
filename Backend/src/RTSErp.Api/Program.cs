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

// ── Create schema and seed on startup ────────────────────────────────────────
string? startupError = null;
try
{
    using var scope = app.Services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var umgr   = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var rmgr   = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // 90-second total budget for schema creation + seed.
    // If Supabase is cold/slow the app still starts; seed finishes on next restart.
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

    logger.LogInformation("Ensuring database schema exists...");
    await db.Database.EnsureCreatedAsync(cts.Token);
    logger.LogInformation("Database schema OK. Running seed...");

    await DbSeeder.SeedAsync(db, umgr, rmgr, logger);
    logger.LogInformation("Seed complete.");
}
catch (OperationCanceledException)
{
    startupError = "Seed timed out after 90 s — app is running, seed will finish on next restart.";
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(startupError);
}
catch (Exception ex)
{
    startupError = ex.ToString();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "STARTUP ERROR — database init or seed failed: {Message}", ex.Message);
}

// ── Swagger (all environments) ────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
// NOTE: Do NOT call app.UseHttpsRedirection() on Railway/Render — TLS is
// terminated at the ingress proxy. Calling it here causes redirect loops.
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

// ── Health + diagnostics endpoints ───────────────────────────────────────────
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { status = "RTS ERP API", version = "2.0" }));

// Diagnostics: returns startup error (if any) and key config state.
// REMOVE this endpoint once the deployment is stable.
app.MapGet("/diagnostics", (IConfiguration cfg) =>
{
    var rawCs = cfg.GetConnectionString("DefaultConnection") ?? "";
    var normalized = RTSErp.Infrastructure.DependencyInjection.NormalizePostgresConnectionString(rawCs);
    // Strip password from the diagnostic output
    var safeCs = System.Text.RegularExpressions.Regex.Replace(
        normalized, @"Password=[^;]*", "Password=***");

    return Results.Ok(new
    {
        startupError     = startupError ?? "none",
        dbConfigured     = !string.IsNullOrEmpty(rawCs),
        dbConnectionSafe = safeCs,
        jwtKeyConfigured = !string.IsNullOrEmpty(cfg["Jwt:SigningKey"]),
        corsOrigins      = cfg.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [],
        environment      = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown"
    });
});

app.MapControllers();

app.Run();
