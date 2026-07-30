namespace RTSErp.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Permissions { get; }
    bool IsAuthenticated { get; }
}
