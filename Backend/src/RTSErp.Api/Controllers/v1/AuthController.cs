using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Identity.Commands.Login;
using RTSErp.Application.Identity.Commands.Logout;
using RTSErp.Application.Identity.Commands.Refresh;
using RTSErp.Application.Identity.Queries.GetCurrentUser;
using RTSErp.Domain.Entities.Audit;

namespace RTSErp.Api.Controllers.v1;

[Microsoft.AspNetCore.Mvc.Route("api/v1/auth")]
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
                // Audit: login failed — pass email explicitly since user is not authenticated yet
                var audit = HttpContext.RequestServices.GetService<IAuditService>();
                if (audit != null)
                    _ = Task.Run(() => audit.LogAsync(
                        AuditActions.LoginFailed, AuditModules.Auth,
                        entityName: command.Email, status: "Failed",
                        details: result.Error, ipAddress: command.IpAddress));
                return Unauthorized(new { message = result.Error });
            }

            SetRefreshTokenCookie(result.RefreshToken!);

            // Audit: login success — pass email explicitly since user is not yet in HttpContext.User
            {
                var audit = HttpContext.RequestServices.GetService<IAuditService>();
                var email = result.Auth?.Email ?? command.Email;
                if (audit != null)
                    _ = Task.Run(() => audit.LogAsync(
                        AuditActions.Login, AuditModules.Auth,
                        entityName: email, details: $"Login from {command.IpAddress ?? "unknown"}",
                        ipAddress: command.IpAddress));
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

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest req,
        [FromServices] UserManager<RTSErp.Domain.Entities.Identity.ApplicationUser> userManager,
        CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (userId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var result = await userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        return Ok(new { message = "Password changed successfully." });
    }

    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    [HttpPatch("update-profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest req,
        [FromServices] UserManager<RTSErp.Domain.Entities.Identity.ApplicationUser> userManager)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (userId is null) return Unauthorized();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();
        if (!string.IsNullOrWhiteSpace(req.FirstName)) user.FirstName = req.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(req.LastName))  user.LastName  = req.LastName.Trim();
        await userManager.UpdateAsync(user);
        return Ok(new { user.FirstName, user.LastName });
    }

    public record UpdateProfileRequest(string? FirstName, string? LastName);

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
