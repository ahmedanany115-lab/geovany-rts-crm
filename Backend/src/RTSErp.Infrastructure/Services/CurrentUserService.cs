using Microsoft.AspNetCore.Http;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Shared.Constants;

namespace RTSErp.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private System.Security.Claims.ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;

    public string? UserName
    {
        get
        {
            var name = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(name)) return name;
            // Fall back to email prefix if no name claim
            return Email?.Split('@')[0];
        }
    }

    public IReadOnlyList<string> Permissions =>
        User?.FindAll(AppClaimTypes.Permission).Select(c => c.Value).ToList() ?? [];
}
