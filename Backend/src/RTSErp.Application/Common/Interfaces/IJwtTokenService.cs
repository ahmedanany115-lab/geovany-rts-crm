using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Issues a short-lived access token embedding the user's id, email, and permission claims.</summary>
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> permissionCodes);
}
