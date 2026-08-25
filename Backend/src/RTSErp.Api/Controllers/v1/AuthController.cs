using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Identity.Commands.Login;
using RTSErp.Application.Identity.Commands.Logout;
using RTSErp.Application.Identity.Commands.Refresh;
using RTSErp.Application.Identity.Queries.GetCurrentUser;

namespace RTSErp.Api.Controllers.v1;

public class AuthController : BaseApiController
{
    private const string RefreshTokenCookieName = "rts_erp_refresh_token";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        try
        {
            command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await Mediator.Send(command);

            if (!result.Succeeded)
                return Unauthorized(new { message = result.Error });

            SetRefreshTokenCookie(result.RefreshToken!);
            return Ok(result.Auth);
        }
        catch (Exception ex)
        {
            // Log and return a structured error instead of an unhandled 500
            var logger = HttpContext.RequestServices
                .GetRequiredService<ILogger<AuthController>>();
            logger.LogError(ex, "Unhandled error during login for {Email}", command.Email);

            return StatusCode(500, new
            {
                message = "Login failed due to a server error.",
                detail  = ex.Message   // safe to expose on first deploy for diagnosis
            });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
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
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrEmpty(refreshToken))
            await Mediator.Send(new LogoutCommand { RefreshToken = refreshToken });

        Response.Cookies.Delete(RefreshTokenCookieName);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await Mediator.Send(new GetCurrentUserQuery());
        return Ok(user);
    }

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.None, // frontend and API on different origins
            Expires  = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
