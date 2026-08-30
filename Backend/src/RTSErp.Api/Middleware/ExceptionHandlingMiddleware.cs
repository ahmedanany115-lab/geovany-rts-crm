using System.Net;
using System.Text.Json;
using FluentValidation;
using RTSErp.Application.Common.Exceptions;

namespace RTSErp.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest,
                "validation-error", "One or more validation errors occurred.",
                ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound, "not-found", ex.Message);
        }
        catch (UnauthorizedAccessAppException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Unauthorized, "unauthorized", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Method} {Path} — {Msg}",
                context.Request.Method, context.Request.Path, ex.Message);

            // Always include exception detail so we can diagnose production issues
            // without needing Railway log access. Remove once system is stable.
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError,
                "server-error",
                "An unexpected error occurred.",
                new Dictionary<string, string[]>
                {
                    ["exception"] = [ex.GetType().Name],
                    ["message"]   = [ex.Message],
                    ["inner"]     = [ex.InnerException?.Message ?? "none"],
                    ["source"]    = [ex.Source ?? "unknown"]
                });
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context, HttpStatusCode statusCode, string type, string title,
        Dictionary<string, string[]>? errors = null)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type,
            title,
            status = (int)statusCode,
            errors
        }));
    }
}
