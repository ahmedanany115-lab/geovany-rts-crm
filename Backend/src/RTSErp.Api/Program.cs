using RTSErp.Api.Extensions;
using RTSErp.Api.Middleware;
using RTSErp.Application;
using RTSErp.Infrastructure;
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

// ── Schema + seed run in DatabaseSeedingService (BackgroundService) ───────────
// This keeps startup instant — Railway's health check passes before the DB
// is touched. The background service starts 2 s after the app begins listening
// and retries every 30 s until seeding succeeds.
string? startupError = null; // kept for the /diagnostics endpoint

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
