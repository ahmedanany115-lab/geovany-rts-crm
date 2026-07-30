namespace RTSErp.Application.Identity.Dtos;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    // Changed from IReadOnlyList to List
    public List<string> Roles { get; set; } = new();

    // Changed from IReadOnlyList to List
    public List<string> Permissions { get; set; } = new();
}