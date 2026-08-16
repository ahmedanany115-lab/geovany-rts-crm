using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Identity.Dtos;
using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Application.Identity.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext db,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _userManager = userManager;
        _db = db;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !user.IsActive || user.IsDeleted)
            return new LoginResult { Succeeded = false, Error = "Invalid email or password." };

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
            return new LoginResult { Succeeded = false, Error = "Invalid email or password." };

        var roleNames = await _userManager.GetRolesAsync(user);

        var roleIds = await _db.Roles
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var permissionCodes = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(user, permissionCodes);

        var refreshToken = await _refreshTokenService.IssueAsync(
            user.Id,
            request.IpAddress,
            cancellationToken);

        return new LoginResult
        {
            Succeeded = true,
            RefreshToken = refreshToken,
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