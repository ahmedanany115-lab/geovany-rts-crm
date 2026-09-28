using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.JournalEntries.Commands.CreateJournalEntry;
using RTSErp.Application.Accounting.JournalEntries.Commands.PostJournalEntry;
using RTSErp.Application.Accounting.JournalEntries.Commands.ReverseJournalEntry;
using RTSErp.Application.Accounting.JournalEntries.Commands.UpdateJournalEntry;
using RTSErp.Application.Accounting.JournalEntries.Queries.GetJournalEntries;
using RTSErp.Application.Accounting.JournalEntries.Queries.GetJournalEntry;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Audit;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/journalentries")]
public class JournalEntriesController : BaseApiController
{
    // Moataz's exact seeded email — identified from DbSeeder.cs.
    // Admin role OR this email may edit/delete Posted entries.
    private const string MoatazEmail = "Moataz@rtegy.com";

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] JournalEntryStatus? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? fiscalPeriodId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetJournalEntriesQuery
        {
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            FiscalPeriodId = fiscalPeriodId,
            Page = page,
            PageSize = pageSize
        });
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await Mediator.Send(new GetJournalEntryQuery { Id = id });
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateJournalEntryCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors });
        return CreatedAtAction(nameof(Get), new { id = result.EntryId }, result);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id)
    {
        var result = await Mediator.Send(new PostJournalEntryCommand { Id = id });
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors });
        return Ok(result);
    }

    /// <summary>
    /// Update a journal entry.
    /// Draft: any authorized user (Admin, Accountant) may edit.
    /// Posted: only Admin role or Moataz@rtegy.com may edit (enforced in handler).
    /// Reversed: not allowed.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Update(Guid id, UpdateJournalEntryCommand command)
    {
        command.Id = id;
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors });
        return NoContent();
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<IActionResult> Reverse(Guid id, ReverseJournalEntryCommand command)
    {
        command.Id = id;
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors });
        return Ok(result);
    }

    /// <summary>
    /// Delete a journal entry.
    /// Draft: any Admin or Accountant may delete.
    /// Posted: only Admin role or Moataz@rtegy.com may delete (enforced here + audit logged).
    /// Reversed: not allowed.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        var entry = await db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct);

        if (entry is null) return NotFound();

        // Reversed entries are never deletable
        if (entry.Status == JournalEntryStatus.Reversed)
            return BadRequest(new
            {
                message = "Reversed journal entries cannot be deleted.",
            });

        // Posted entries: only Admin or Moataz
        if (entry.Status == JournalEntryStatus.Posted)
        {
            var isAdmin = currentUser.IsInRole("Admin");
            var isMoataz = string.Equals(currentUser.Email, MoatazEmail, StringComparison.OrdinalIgnoreCase);

            if (!isAdmin && !isMoataz)
                return BadRequest(new
                {
                    message = "Only administrators or privileged users can delete Posted journal entries.",
                });
        }

        // Soft-delete the entry and all its lines
        entry.IsDeleted  = true;
        entry.ModifiedAt = DateTime.UtcNow;
        foreach (var line in entry.Lines)
        {
            line.IsDeleted  = true;
            line.ModifiedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        // Audit — fire-and-forget
        await audit.LogAsync(
            action:     AuditActions.Deleted,
            module:     AuditModules.Finance,
            entityName: "JournalEntry",
            entityId:   entry.Id,
            entityType: "JournalEntry",
            reference:  entry.EntryNumber,
            details:    entry.Status == JournalEntryStatus.Posted
                            ? $"Posted entry deleted by privileged user ({currentUser.Email})"
                            : "Draft entry deleted",
            ct: ct);

        return NoContent();
    }
}
