using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Infrastructure.Persistence;

namespace RTSErp.Infrastructure.Identity;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _db;
    private const int RefreshTokenExpiryDays = 7;

    public RefreshTokenService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> IssueAsync(Guid userId, string? createdByIp, CancellationToken cancellationToken = default)
    {
        var (plaintext, hash) = GenerateToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            CreatedByIp = createdByIp
        });

        await _db.SaveChangesAsync(cancellationToken);
        return plaintext;
    }

    public async Task<(string NewToken, Guid UserId)?> RotateAsync(string plaintextToken, string? createdByIp, CancellationToken cancellationToken = default)
    {
        var hash = Hash(plaintextToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

        if (existing is null)
            return null;

        if (!existing.IsActive)
        {
            // Reuse of an already-revoked/expired token is a signal of possible theft —
            // revoke the whole chain for this user as a precaution.
            await RevokeAllForUserAsync(existing.UserId, cancellationToken);
            return null;
        }

        var (newPlaintext, newHash) = GenerateToken();
        var newToken = new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = newHash,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            CreatedByIp = createdByIp
        };

        existing.RevokedAt = DateTime.UtcNow;
        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync(cancellationToken);

        existing.ReplacedByTokenId = newToken.Id;
        await _db.SaveChangesAsync(cancellationToken);

        return (newPlaintext, existing.UserId);
    }

    public async Task RevokeAsync(string plaintextToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(plaintextToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);
        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
            token.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static (string Plaintext, string Hash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var plaintext = Convert.ToBase64String(bytes);
        return (plaintext, Hash(plaintext));
    }

    private static string Hash(string plaintext)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToBase64String(bytes);
    }
}
