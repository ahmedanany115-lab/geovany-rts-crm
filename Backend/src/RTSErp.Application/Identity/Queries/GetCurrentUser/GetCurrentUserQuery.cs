using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Identity.Dtos;
using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Application.Identity.Queries.GetCurrentUser;

public class GetCurrentUserQuery : IRequest<UserDto>
{
}

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _db;

    public GetCurrentUserQueryHandler(ICurrentUserService currentUser, UserManager<ApplicationUser> userManager, IApplicationDbContext db)
    {
        _currentUser = currentUser;
        _userManager = userManager;
        _db = db;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            throw new UnauthorizedAccessAppException("No authenticated user.");

        var user = await _userManager.FindByIdAsync(_currentUser.UserId.Value.ToString())
            ?? throw new NotFoundException(nameof(ApplicationUser), _currentUser.UserId.Value);

        var roleNames = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            Roles = roleNames.ToList(),
            Permissions = _currentUser.Permissions.ToList()
        };
    }
}
