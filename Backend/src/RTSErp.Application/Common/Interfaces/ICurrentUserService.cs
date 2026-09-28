namespace RTSErp.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid?   UserId   { get; }
    string? Email    { get; }
    string? UserName { get; }    // Display name (FirstName LastName)
    string? UserEmail => Email;  // Alias
    IReadOnlyList<string> Permissions { get; }
    bool IsAuthenticated { get; }

    /// <summary>Returns true when the current user holds the given ASP.NET Identity role.</summary>
    bool IsInRole(string role);
}
