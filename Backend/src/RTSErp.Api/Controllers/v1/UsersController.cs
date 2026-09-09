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
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var users = await _userManager.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .ToListAsync(ct);

        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
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
