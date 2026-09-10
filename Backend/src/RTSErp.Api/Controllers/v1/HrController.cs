using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Domain.Entities.HR;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Infrastructure.Persistence;

namespace RTSErp.Api.Controllers.v1;

/// <summary>
/// Employee Self-Service: leave requests and meeting logs.
/// Any authenticated user can submit their own records.
/// Admin / Accountant / SalesManager can see all records and approve/reject.
/// </summary>
[Authorize]
public class HrController : BaseApiController
{
    private readonly ApplicationDbContext         _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public HrController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        var name = User.Identity?.Name ?? User.Claims
            .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name ||
                                 c.Type == System.Security.Claims.ClaimTypes.Email ||
                                 c.Type == "sub" || c.Type == "email")?.Value
            ?? string.Empty;
        return (await _userManager.FindByNameAsync(name))!;
    }

    private bool CanReview() =>
        User.IsInRole("Admin") || User.IsInRole("Accountant") || User.IsInRole("SalesManager");

    // ────────────────────────────────────────────────────────────────────────────
    // LEAVE REQUESTS
    // ────────────────────────────────────────────────────────────────────────────

    [HttpGet("leaves")]
    public async Task<IActionResult> GetLeaves(
        [FromQuery] string? status,
        [FromQuery] string? employeeId,
        CancellationToken ct)
    {
        var query = _db.LeaveRequests
            .Where(r => !r.IsDeleted)
            .AsQueryable();

        // Non-reviewers see only their own records
        if (!CanReview())
        {
            var me = await GetCurrentUserAsync();
            query  = query.Where(r => r.EmployeeId == me.Id);
        }
        else if (!string.IsNullOrEmpty(employeeId) && Guid.TryParse(employeeId, out var eid))
        {
            query = query.Where(r => r.EmployeeId == eid);
        }

        if (!string.IsNullOrEmpty(status) && int.TryParse(status, out var s))
            query = query.Where(r => (int)r.Status == s);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id, r.EmployeeId, r.EmployeeEmail, r.EmployeeName,
                type     = r.Type.ToString(),
                typeId   = (int)r.Type,
                r.StartDate, r.EndDate, r.DaysCount, r.Reason,
                status   = r.Status.ToString(),
                statusId = (int)r.Status,
                r.ReviewedByName, r.ReviewedAt, r.ReviewNote, r.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("leaves")]
    public async Task<IActionResult> SubmitLeave([FromBody] SubmitLeaveRequest req)
    {
        var me = await GetCurrentUserAsync();
        if (me is null) return Unauthorized();

        var days = (req.EndDate.ToDateTime(TimeOnly.MinValue) -
                    req.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

        var leave = new LeaveRequest
        {
            EmployeeId    = me.Id,
            EmployeeEmail = me.Email!,
            EmployeeName  = $"{me.FirstName} {me.LastName}".Trim(),
            Type          = req.Type,
            StartDate     = req.StartDate,
            EndDate       = req.EndDate,
            DaysCount     = days,
            Reason        = req.Reason,
            Status        = LeaveStatus.Pending,
            CreatedBy     = me.Id
        };

        _db.LeaveRequests.Add(leave);
        await _db.SaveChangesAsync();
        return Ok(new { leave.Id });
    }

    [HttpPatch("leaves/{id:guid}/review")]
    public async Task<IActionResult> ReviewLeave(Guid id, [FromBody] ReviewRequest req)
    {
        if (!CanReview()) return Forbid();

        var leave = await _db.LeaveRequests.FindAsync(id);
        if (leave is null || leave.IsDeleted) return NotFound();

        var me = await GetCurrentUserAsync();

        leave.Status         = req.Approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        leave.ReviewedByName = $"{me?.FirstName} {me?.LastName}".Trim();
        leave.ReviewedAt     = DateTime.UtcNow;
        leave.ReviewNote     = req.Note;
        leave.ModifiedAt     = DateTime.UtcNow;
        leave.ModifiedBy     = me?.Id;

        await _db.SaveChangesAsync();
        return Ok(new { leave.Id, status = leave.Status.ToString() });
    }

    [HttpDelete("leaves/{id:guid}")]
    public async Task<IActionResult> DeleteLeave(Guid id)
    {
        var leave = await _db.LeaveRequests.FindAsync(id);
        if (leave is null || leave.IsDeleted) return NotFound();

        // Only owner or admin can delete; only if still Pending
        var me = await GetCurrentUserAsync();
        if (leave.EmployeeId != me.Id && !User.IsInRole("Admin"))
            return Forbid();
        if (leave.Status != LeaveStatus.Pending && !User.IsInRole("Admin"))
            return BadRequest(new { message = "Only pending requests can be cancelled." });

        leave.IsDeleted  = true;
        leave.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ────────────────────────────────────────────────────────────────────────────
    // MEETING LOGS
    // ────────────────────────────────────────────────────────────────────────────

    [HttpGet("meetings")]
    public async Task<IActionResult> GetMeetings(
        [FromQuery] string? employeeId,
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        CancellationToken ct)
    {
        var query = _db.MeetingLogs
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        if (!CanReview())
        {
            var me = await GetCurrentUserAsync();
            query  = query.Where(m => m.EmployeeId == me.Id);
        }
        else if (!string.IsNullOrEmpty(employeeId) && Guid.TryParse(employeeId, out var eid))
        {
            query = query.Where(m => m.EmployeeId == eid);
        }

        if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var fd))
            query = query.Where(m => m.StartTime >= fd);
        if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var td))
            query = query.Where(m => m.StartTime <= td);

        var items = await query
            .OrderByDescending(m => m.StartTime)
            .Select(m => new
            {
                m.Id, m.EmployeeId, m.EmployeeEmail, m.EmployeeName,
                m.Title, m.Description,
                type    = m.Type.ToString(),
                typeId  = (int)m.Type,
                m.StartTime, m.EndTime,
                durationMinutes = (int)(m.EndTime - m.StartTime).TotalMinutes,
                m.Location, m.Attendees, m.Outcome, m.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("meetings")]
    public async Task<IActionResult> LogMeeting([FromBody] LogMeetingRequest req)
    {
        var me = await GetCurrentUserAsync();
        if (me is null) return Unauthorized();

        var meeting = new MeetingLog
        {
            EmployeeId    = me.Id,
            EmployeeEmail = me.Email!,
            EmployeeName  = $"{me.FirstName} {me.LastName}".Trim(),
            Title         = req.Title,
            Description   = req.Description,
            Type          = req.Type,
            StartTime     = req.StartTime,
            EndTime       = req.EndTime,
            Location      = req.Location,
            Attendees     = req.Attendees,
            Outcome       = req.Outcome,
            CreatedBy     = me.Id
        };

        _db.MeetingLogs.Add(meeting);
        await _db.SaveChangesAsync();
        return Ok(new { meeting.Id });
    }

    [HttpDelete("meetings/{id:guid}")]
    public async Task<IActionResult> DeleteMeeting(Guid id)
    {
        var meeting = await _db.MeetingLogs.FindAsync(id);
        if (meeting is null || meeting.IsDeleted) return NotFound();

        var me = await GetCurrentUserAsync();
        if (meeting.EmployeeId != me.Id && !User.IsInRole("Admin"))
            return Forbid();

        meeting.IsDeleted  = true;
        meeting.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Request DTOs ──────────────────────────────────────────────────────────

    public record SubmitLeaveRequest(
        LeaveType Type,
        DateOnly  StartDate,
        DateOnly  EndDate,
        string    Reason);

    public record ReviewRequest(bool Approve, string? Note);

    public record LogMeetingRequest(
        string      Title,
        string?     Description,
        MeetingType Type,
        DateTime    StartTime,
        DateTime    EndTime,
        string?     Location,
        string?     Attendees,
        string?     Outcome);
}
