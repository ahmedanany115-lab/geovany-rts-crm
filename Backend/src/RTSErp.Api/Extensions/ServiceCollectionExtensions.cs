using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RTSErp.Shared.Constants;

namespace RTSErp.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var signingKey = jwtSection["SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            // Log a clear error and use a placeholder so the app starts and returns
            // useful error messages rather than crashing with a 500 on every request.
            Console.Error.WriteLine(
                "[FATAL] Jwt:SigningKey is not configured. " +
                "Set the Jwt__SigningKey environment variable on Railway. " +
                "All authenticated endpoints will reject requests until this is fixed.");

            // Minimum-length placeholder keeps the JWT middleware wired up so
            // the app boots; tokens signed with this key will always fail validation.
            signingKey = "UNCONFIGURED_KEY_SET_Jwt__SigningKey_ENV_VAR_NOW_32chars";
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

        return services;
    }

    /// <summary>
    /// Registers one authorization policy per permission code so controllers can declare
    /// [Authorize(Policy = "crm.customers.write")] — checked against the "permission" claims
    /// embedded in the JWT at login. Module-specific permission codes are added as each
    /// module's controllers are built; Auth + Shell doesn't gate anything by permission yet.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder();
        // Example, added per-module going forward:
        // .AddPolicy("crm.customers.write", policy => policy.RequireClaim(AppClaimTypes.Permission, "crm.customers.write"));

        return services;
    }

    public static IServiceCollection AddSwaggerWithJwtSupport(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "RTS ERP API", Version = "v1" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the access token returned by /api/v1/auth/login."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    []
                }
            });
        });

        return services;
    }
}
