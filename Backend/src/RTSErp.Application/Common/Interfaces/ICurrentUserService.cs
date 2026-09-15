namespace RTSErp.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid?   UserId   { get; }
    string? Email    { get; }
    string? UserName { get; }    // Display name (FirstName LastName)
    string? UserEmail => Email;  // Alias
    IReadOnlyList<string> Permissions { get; }
    bool IsAuthenticated { get; }
}
