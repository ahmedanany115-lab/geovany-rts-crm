using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    /// <summary>Issues and persists a new refresh token for the user. Returns the plaintext token (only time it's ever visible).</summary>
    Task<string> IssueAsync(Guid userId, string? createdByIp, CancellationToken cancellationToken = default);

    /// <summary>Validates the plaintext token, rotates it (revokes old, issues new), and returns the new plaintext token + user id. Null if invalid/expired/already-revoked (possible reuse attack).</summary>
    Task<(string NewToken, Guid UserId)?> RotateAsync(string plaintextToken, string? createdByIp, CancellationToken cancellationToken = default);

    /// <summary>Revokes a single refresh token.</summary>
    Task RevokeAsync(string plaintextToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes every active refresh token for a user ("log out of all devices").</summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
