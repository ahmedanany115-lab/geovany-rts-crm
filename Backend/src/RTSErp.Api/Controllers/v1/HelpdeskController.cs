using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Helpdesk;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
[Microsoft.AspNetCore.Mvc.Route("api/v1/helpdesk")]
public class HelpdeskController : BaseApiController
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;

    public HelpdeskController(IApplicationDbContext db, ICurrentUserService user)
    { _db = db; _user = user; }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] TicketStatus? status,
        [FromQuery] string? assignedToMe,
        CancellationToken ct)
    {
        var q = _db.Tickets.Where(t => !t.IsDeleted);

        // Non-agents (non-Admin/Manager/SupportAgent) only see their own tickets
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && !User.IsInRole("SupportAgent"))
            q = q.Where(t => t.ReportedById == _user.UserId);

        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        if (assignedToMe == "true" && _user.UserId.HasValue)
            q = q.Where(t => t.AssignedToId == _user.UserId.Value);

        var tickets = await q
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id, t.TicketNumber, t.Title, t.Description, t.Priority, t.Status, t.Category,
                priorityName = t.Priority.ToString(), statusName = t.Status.ToString(), categoryName = t.Category.ToString(),
                t.ReportedByName, t.ReportedByEmail, t.AssignedToName, t.RelatedEntityRef,
                t.Resolution, t.ResolvedAt, t.ClosedAt, t.CreatedAt,
                commentCount = t.Comments.Count(c => !c.IsDeleted),
            })
            .ToListAsync(ct);

        return Ok(tickets);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var t = await _db.Tickets
            .Include(x => x.Comments.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (t is null) return NotFound();
        return Ok(t);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest req, CancellationToken ct)
    {
        var count  = await _db.Tickets.CountAsync(ct);
        var ticket = new Ticket
        {
            TicketNumber    = $"TKT-{DateTime.UtcNow.Year}-{(count + 1):D4}",
            Title           = req.Title.Trim(),
            Description     = req.Description?.Trim(),
            Priority        = req.Priority,
            Category        = req.Category,
            Status          = TicketStatus.Open,
            ReportedById    = _user.UserId,
            ReportedByName  = _user.UserName ?? string.Empty,
            ReportedByEmail = _user.Email    ?? string.Empty,
            RelatedEntityRef = req.RelatedEntityRef?.Trim(),
            CreatedBy       = _user.UserId,
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        return Ok(new { ticket.Id, ticket.TicketNumber });
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Manager,SupportAgent")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FindAsync([id], ct);
        if (ticket is null || ticket.IsDeleted) return NotFound();
        ticket.Status     = req.Status;
        ticket.ModifiedAt = DateTime.UtcNow;
        ticket.ModifiedBy = _user.UserId;
        if (req.Status == TicketStatus.Resolved) { ticket.Resolution = req.Resolution; ticket.ResolvedAt = DateTime.UtcNow; }
        if (req.Status == TicketStatus.Closed)   ticket.ClosedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/assign")]
    [Authorize(Roles = "Admin,Manager,SupportAgent")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignRequest req, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FindAsync([id], ct);
        if (ticket is null || ticket.IsDeleted) return NotFound();
        ticket.AssignedToId   = req.AgentId;
        ticket.AssignedToName = req.AgentName;
        ticket.ModifiedAt     = DateTime.UtcNow;
        if (ticket.Status == TicketStatus.Open) ticket.Status = TicketStatus.InProgress;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest req, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FindAsync([id], ct);
        if (ticket is null || ticket.IsDeleted) return NotFound();

        var comment = new TicketComment
        {
            TicketId   = id,
            Body       = req.Body.Trim(),
            AuthorId   = _user.UserId,
            AuthorName = _user.UserName ?? string.Empty,
            IsInternal = req.IsInternal && (User.IsInRole("Admin") || User.IsInRole("SupportAgent")),
            CreatedBy  = _user.UserId,
        };
        _db.TicketComments.Add(comment);

        if (ticket.Status == TicketStatus.Pending) ticket.Status = TicketStatus.InProgress;
        await _db.SaveChangesAsync(ct);
        return Ok(new { comment.Id });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FindAsync([id], ct);
        if (ticket is null || ticket.IsDeleted) return NotFound();
        ticket.IsDeleted  = true;
        ticket.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    public record CreateTicketRequest(
        string Title, string? Description,
        TicketPriority Priority, TicketCategory Category,
        string? RelatedEntityRef);

    public record UpdateStatusRequest(TicketStatus Status, string? Resolution);
    public record AssignRequest(Guid? AgentId, string? AgentName);
    public record AddCommentRequest(string Body, bool IsInternal = false);
}
