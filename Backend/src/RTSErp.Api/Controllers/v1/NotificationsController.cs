using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Notifications;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
[Microsoft.AspNetCore.Mvc.Route("api/v1/notifications")]
public class NotificationsController : BaseApiController
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;

    public NotificationsController(IApplicationDbContext db, ICurrentUserService user)
    { _db = db; _user = user; }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? unreadOnly, CancellationToken ct)
    {
        if (_user.UserId is null) return Unauthorized();

        var q = _db.Notifications
            .Where(n => n.UserId == _user.UserId.Value && !n.IsDeleted);

        if (unreadOnly == true) q = q.Where(n => !n.IsRead);

        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new
            {
                n.Id, n.Title, n.Body, n.Type,
                n.RelatedRoute, n.RelatedId, n.IsRead, n.ReadAt, n.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        if (_user.UserId is null) return Ok(new { count = 0 });
        var count = await _db.Notifications
            .CountAsync(n => n.UserId == _user.UserId.Value && !n.IsRead && !n.IsDeleted, ct);
        return Ok(new { count });
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var n = await _db.Notifications.FindAsync([id], ct);
        if (n is null || n.UserId != _user.UserId) return NotFound();
        n.IsRead = true; n.ReadAt = DateTime.UtcNow; n.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        if (_user.UserId is null) return Unauthorized();
        var unread = await _db.Notifications
            .Where(n => n.UserId == _user.UserId.Value && !n.IsRead && !n.IsDeleted)
            .ToListAsync(ct);
        foreach (var n in unread) { n.IsRead = true; n.ReadAt = DateTime.UtcNow; n.ModifiedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var n = await _db.Notifications.FindAsync([id], ct);
        if (n is null || n.UserId != _user.UserId) return NotFound();
        n.IsDeleted = true; n.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Admin/system endpoint to post a notification to a specific user.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest req, CancellationToken ct)
    {
        var notif = new AppNotification
        {
            UserId       = req.UserId,
            Title        = req.Title,
            Body         = req.Body,
            Type         = req.Type ?? "info",
            RelatedRoute = req.RelatedRoute,
            RelatedId    = req.RelatedId,
            CreatedBy    = _user.UserId,
        };
        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync(ct);
        return Ok(new { notif.Id });
    }

    public record CreateNotificationRequest(
        Guid    UserId,
        string  Title,
        string  Body,
        string? Type = "info",
        string? RelatedRoute = null,
        Guid?   RelatedId = null);
}
