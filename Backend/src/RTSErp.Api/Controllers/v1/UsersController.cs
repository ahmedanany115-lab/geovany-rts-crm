using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Infrastructure.Persistence;

namespace RTSErp.Api.Controllers.v1;

[Authorize]   // authenticated; individual methods specify which roles
[Microsoft.AspNetCore.Mvc.Route("api/v1/users")]
public class UsersController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db          = db;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Accountant,Manager")]   // extended so Accountant can load sales users for customer assignment
    public async Task<IActionResult> List([FromQuery] string? role, CancellationToken ct)
    {
        // Pure view-only accounts that are not real staff — hidden from user management.
        // Randa (randa@rtegy.com) is ReadOnly but is a real staff member, so she IS shown.
        var hiddenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mhy@rtegy.com",
            "kfahim@rtegy.com",
            "Dgeorge@rtegy.com",
            "Farah@rtegy.com",
        };

        var users = await _userManager.Users
            .Where(u => !u.IsDeleted && !hiddenEmails.Contains(u.Email!))
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .ToListAsync(ct);

        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);

            // Filter by role if specified
            if (!string.IsNullOrWhiteSpace(role) &&
                !roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase)))
                continue;

            result.Add(new
            {
                id         = u.Id,
                email      = u.Email,
                firstName  = u.FirstName,
                lastName   = u.LastName,
                fullName   = $"{u.FirstName} {u.LastName}".Trim(),
                isActive   = u.IsActive,
                roles,
                createdAt  = u.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpPatch("{id:guid}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        return Ok(new { user.Id, user.IsActive });
    }

    /// <summary>Create a new ERP user account — Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "Email is required." });
        if (string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Password is required." });
        if (string.IsNullOrWhiteSpace(req.Role))
            return BadRequest(new { message = "Role is required." });

        // Verify role exists
        var roleExists = await _db.Roles.AnyAsync(r => r.Name == req.Role);
        if (!roleExists)
            return BadRequest(new { message = $"Role '{req.Role}' does not exist." });

        // Check email uniqueness
        var existing = await _userManager.FindByEmailAsync(req.Email.Trim());
        if (existing != null)
            return Conflict(new { message = $"A user with email '{req.Email}' already exists." });

        var user = new RTSErp.Domain.Entities.Identity.ApplicationUser
        {
            UserName      = req.Email.Trim(),
            Email         = req.Email.Trim(),
            FirstName     = req.FirstName?.Trim() ?? string.Empty,
            LastName      = req.LastName?.Trim()  ?? string.Empty,
            JobTitle      = req.JobTitle?.Trim(),
            Department    = req.Department?.Trim(),
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        await _userManager.AddToRoleAsync(user, req.Role);

        return Ok(new { user.Id, user.Email, user.FirstName, user.LastName, role = req.Role });
    }

    public record CreateUserRequest(
        string Email, string Password, string Role,
        string? FirstName, string? LastName,
        string? JobTitle, string? Department);
}
