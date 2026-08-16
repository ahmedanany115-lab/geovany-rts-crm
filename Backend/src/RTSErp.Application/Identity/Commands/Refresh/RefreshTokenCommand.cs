using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Identity.Dtos;
using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Application.Identity.Commands.Refresh;

public class RefreshTokenCommand : IRequest<RefreshTokenResult>
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}

public class RefreshTokenResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public AuthResponseDto? Auth { get; set; }
    public string? NewRefreshToken { get; set; }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenService refreshTokenService,
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext db,
        IJwtTokenService jwtTokenService)
    {
        _refreshTokenService = refreshTokenService;
        _userManager = userManager;
        _db = db;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var rotation = await _refreshTokenService.RotateAsync(request.RefreshToken, request.IpAddress, cancellationToken);
        if (rotation is null)
            return new RefreshTokenResult { Succeeded = false, Error = "Refresh token is invalid or has expired." };

        var user = await _userManager.FindByIdAsync(rotation.Value.UserId.ToString());
        if (user is null || !user.IsActive || user.IsDeleted)
            return new RefreshTokenResult { Succeeded = false, Error = "Account is no longer active." };

        var roleNames = await _userManager.GetRolesAsync(user);
        var roleIds = await _db.Roles.Where(r => roleNames.Contains(r.Name!)).Select(r => r.Id).ToListAsync(cancellationToken);
        var permissionCodes = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(user, permissionCodes);

        return new RefreshTokenResult
        {
            Succeeded = true,
            NewRefreshToken = rotation.Value.NewToken,
            Auth = new AuthResponseDto
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    AvatarUrl = user.AvatarUrl,
                    Roles = roleNames.ToList(),
                    Permissions = permissionCodes
                }
            }
        };
    }
}
