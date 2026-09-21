using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Infrastructure.Persistence;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin")]
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
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        return Ok(new { user.Id, user.IsActive });
    }
}
