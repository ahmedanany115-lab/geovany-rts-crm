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
    var isUri = rawCs.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
             || rawCs.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    // Detect transaction-mode pooler (port 6543) — breaks DDL
    var hasPort6543 = rawCs.Contains(":6543") || rawCs.Contains("Port=6543");

    string dbAdvice = "";
    if (!isUri && rawCs.Contains("Port=6543"))
        dbAdvice = "WARNING: Port 6543 = Supabase Transaction Pooler — DDL (CREATE TABLE) will fail. Use port 5432 (Session pooler or direct connection).";
    else if (isUri && rawCs.Contains(":6543"))
        dbAdvice = "WARNING: Port 6543 = Supabase Transaction Pooler — DDL (CREATE TABLE) will fail. Use port 5432 (Session pooler or direct connection).";
    else if (string.IsNullOrEmpty(rawCs))
        dbAdvice = "ERROR: ConnectionStrings__DefaultConnection is not set in Railway environment variables.";
    else
        dbAdvice = "Port looks OK (5432). If connection still fails, ensure you use the Session Mode pooler or Direct connection from Supabase Dashboard > Project Settings > Database.";

    return Results.Ok(new
    {
        startupError     = startupError ?? "none",
        dbConfigured     = !string.IsNullOrEmpty(rawCs),
        dbIsUri          = isUri,
        dbHasPort6543    = hasPort6543,
        dbAdvice,
        jwtKeyConfigured = !string.IsNullOrEmpty(cfg["Jwt:SigningKey"]),
        corsOrigins      = cfg.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [],
        environment      = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown"
    });
});

app.MapControllers();

app.Run();
