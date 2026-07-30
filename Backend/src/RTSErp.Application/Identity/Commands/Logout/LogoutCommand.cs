using MediatR;
using RTSErp.Application.Common.Interfaces;

namespace RTSErp.Application.Identity.Commands.Logout;

public class LogoutCommand : IRequest<Unit>
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutCommandHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        return Unit.Value;
    }
}

public class LogoutAllCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
}

public class LogoutAllCommandHandler : IRequestHandler<LogoutAllCommand, Unit>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutAllCommandHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Unit> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        await _refreshTokenService.RevokeAllForUserAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}
