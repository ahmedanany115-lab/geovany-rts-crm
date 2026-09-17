using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Identity.Commands.Login;
using RTSErp.Application.Identity.Commands.Logout;
using RTSErp.Application.Identity.Commands.Refresh;
using RTSErp.Application.Identity.Queries.GetCurrentUser;
using RTSErp.Domain.Entities.Audit;

namespace RTSErp.Api.Controllers.v1;

public class AuthController : BaseApiController
{
    private const string RefreshTokenCookieName = "rts_erp_refresh_token";
    // Header name the frontend sends when the cookie can't be used cross-origin
    private const string RefreshTokenHeaderName = "X-Refresh-Token";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        try
        {
            command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await Mediator.Send(command);

            if (!result.Succeeded)
            {
                // Fire-and-forget audit: login failed
                var audit = HttpContext.RequestServices.GetService<IAuditService>();
                _ = audit?.LogAsync(AuditActions.LoginFailed, AuditModules.Auth,
                    entityName: command.Email, status: "Failed",
                    details: result.Error, ipAddress: command.IpAddress);
                return Unauthorized(new { message = result.Error });
            }

            SetRefreshTokenCookie(result.RefreshToken!);

            // Fire-and-forget audit: login success
            {
                var audit = HttpContext.RequestServices.GetService<IAuditService>();
                _ = audit?.LogAsync(AuditActions.Login, AuditModules.Auth,
                    entityName: result.Auth?.Email ?? command.Email,
                    ipAddress: command.IpAddress);
            }

            return Ok(result.Auth);
        }
        catch (Exception ex)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AuthController>>();
            logger.LogError(ex, "Login error for {Email}: {Msg}", command.Email, ex.Message);
            return StatusCode(500, new { message = "Login failed.", detail = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        // Accept refresh token from: 1) httpOnly cookie (same-origin / allowed cross-origin)
        //                            2) X-Refresh-Token header (cross-origin SPA fallback)
        var refreshToken =
            Request.Cookies[RefreshTokenCookieName]
            ?? Request.Headers[RefreshTokenHeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "No refresh token present." });

        var result = await Mediator.Send(new RefreshTokenCommand
        {
            RefreshToken = refreshToken,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        if (!result.Succeeded)
        {
            Response.Cookies.Delete(RefreshTokenCookieName);
            return Unauthorized(new { message = result.Error });
        }

        SetRefreshTokenCookie(result.NewRefreshToken!);
        return Ok(result.Auth);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken =
            Request.Cookies[RefreshTokenCookieName]
            ?? Request.Headers[RefreshTokenHeaderName].FirstOrDefault();

        if (!string.IsNullOrEmpty(refreshToken))
            await Mediator.Send(new LogoutCommand { RefreshToken = refreshToken });

        Response.Cookies.Delete(RefreshTokenCookieName);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
        => Ok(await Mediator.Send(new GetCurrentUserQuery()));

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.None,  // required for cross-origin
            Expires  = DateTimeOffset.UtcNow.AddDays(30)
        });
    }
}
