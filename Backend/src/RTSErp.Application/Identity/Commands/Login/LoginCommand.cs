using MediatR;
using RTSErp.Application.Identity.Dtos;

namespace RTSErp.Application.Identity.Commands.Login;

public class LoginCommand : IRequest<LoginResult>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}

public class LoginResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public AuthResponseDto? Auth { get; set; }
    public string? RefreshToken { get; set; }
}
