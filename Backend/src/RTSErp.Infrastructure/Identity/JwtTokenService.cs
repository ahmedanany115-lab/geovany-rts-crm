using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Shared.Constants;

namespace RTSErp.Infrastructure.Identity;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(ApplicationUser user, IEnumerable<string> permissionCodes)
        => GenerateAccessToken(user, permissionCodes, Array.Empty<string>());

    public string GenerateAccessToken(
        ApplicationUser    user,
        IEnumerable<string> permissionCodes,
        IEnumerable<string> roleNames)
    {
        var jwtSection   = _configuration.GetSection("Jwt");
        var signingKey   = jwtSection["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var expiryMinutes = jwtSection.GetValue<int?>("AccessTokenExpiryMinutes") ?? 60;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            // ClaimTypes.Name is what User.Identity.Name reads — set to email (the UserName)
            new(ClaimTypes.NameIdentifier,     user.Id.ToString()),
            new(ClaimTypes.Name,               user.Email ?? string.Empty),
            new("firstName", user.FirstName),
            new("lastName",  user.LastName),
        };

        // ── Role claims — required for [Authorize(Roles="...")] ──────────────
        foreach (var role in roleNames)
            claims.Add(new Claim(ClaimTypes.Role, role));

        // ── Permission claims ────────────────────────────────────────────────
        claims.AddRange(permissionCodes.Select(code => new Claim(AppClaimTypes.Permission, code)));

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             jwtSection["Issuer"],
            audience:           jwtSection["Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
