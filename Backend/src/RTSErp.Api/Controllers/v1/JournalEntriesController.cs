using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Accounting.JournalEntries.Commands.CreateJournalEntry;
using RTSErp.Application.Accounting.JournalEntries.Commands.PostJournalEntry;
using RTSErp.Application.Accounting.JournalEntries.Commands.ReverseJournalEntry;
using RTSErp.Application.Accounting.JournalEntries.Queries.GetJournalEntries;
using RTSErp.Application.Accounting.JournalEntries.Queries.GetJournalEntry;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
public class JournalEntriesController : BaseApiController
{
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
    /// Delete a DRAFT journal entry. Posted entries must be reversed, not deleted.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] RTSErp.Application.Common.Interfaces.IApplicationDbContext db,
        CancellationToken ct)
    {
        var entry = await db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct);

        if (entry is null) return NotFound();

        if (entry.Status != JournalEntryStatus.Draft)
            return BadRequest(new
            {
                message = "Only Draft entries can be deleted. Use 'Reverse' for posted entries.",
            });

        // Soft-delete the entry and all its lines
        entry.IsDeleted  = true;
        entry.ModifiedAt = DateTime.UtcNow;
        foreach (var line in entry.Lines)
        {
            line.IsDeleted  = true;
            line.ModifiedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
