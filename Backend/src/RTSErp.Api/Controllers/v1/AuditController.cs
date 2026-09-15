using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;

namespace RTSErp.Api.Controllers.v1;

/// <summary>
/// User activity / audit log — Admin only.
/// </summary>
[Authorize(Roles = "Admin")]
public class AuditController : BaseApiController
{
    private readonly IApplicationDbContext _db;
    public AuditController(IApplicationDbContext db) => _db = db;

    /// <summary>
    /// Paginated, filtered audit log query.
    /// All filtering happens server-side.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string?   userId,
        [FromQuery] string?   module,
        [FromQuery] string?   action,
        [FromQuery] string?   status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string?   search,
        [FromQuery] int       page     = 1,
        [FromQuery] int       pageSize = 50,
        CancellationToken ct = default)
    {
        if (page     < 1)   page     = 1;
        if (pageSize < 1)   pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var q = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId)
            && Guid.TryParse(userId, out var uid))
            q = q.Where(a => a.UserId == uid);

        if (!string.IsNullOrWhiteSpace(module))
            q = q.Where(a => a.Module == module);

        if (!string.IsNullOrWhiteSpace(action))
            q = q.Where(a => a.Action == action);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(a => a.Status == status);

        if (fromDate.HasValue)
            q = q.Where(a => a.OccurredAt >= fromDate.Value.ToUniversalTime());

        if (toDate.HasValue)
            q = q.Where(a => a.OccurredAt <= toDate.Value.ToUniversalTime().AddDays(1));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(a =>
                a.UserName.ToLower().Contains(s)
             || a.UserEmail.ToLower().Contains(s)
             || (a.EntityName != null && a.EntityName.ToLower().Contains(s))
             || (a.Reference  != null && a.Reference.ToLower().Contains(s))
             || (a.Details    != null && a.Details.ToLower().Contains(s)));
        }

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id, a.UserId, a.UserName, a.UserEmail,
                a.Action, a.Module, a.EntityType, a.EntityId,
                a.EntityName, a.Reference, a.Status, a.Details,
                a.IpAddress, a.OccurredAt,
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, rows });
    }

    /// <summary>Distinct users who have audit entries — for filter dropdown.</summary>
    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken ct)
    {
        var users = await _db.AuditLogs
            .Where(a => a.UserId != null)
            .GroupBy(a => new { a.UserId, a.UserName, a.UserEmail })
            .Select(g => new { g.Key.UserId, g.Key.UserName, g.Key.UserEmail })
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);
        return Ok(users);
    }

    /// <summary>Distinct modules — for filter dropdown.</summary>
    [HttpGet("modules")]
    public async Task<IActionResult> Modules(CancellationToken ct)
    {
        var mods = await _db.AuditLogs
            .Select(a => a.Module)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync(ct);
        return Ok(mods);
    }
}
