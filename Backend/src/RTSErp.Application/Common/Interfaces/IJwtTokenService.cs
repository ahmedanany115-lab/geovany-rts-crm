using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Issues an access token with permission claims only (no roles — legacy overload).</summary>
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> permissionCodes);

    /// <summary>Issues an access token with both role claims and permission claims.</summary>
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> permissionCodes, IEnumerable<string> roleNames);
}
