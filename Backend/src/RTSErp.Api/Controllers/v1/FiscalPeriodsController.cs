using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Accounting.FiscalPeriods.Commands.CloseFiscalPeriod;
using RTSErp.Application.Accounting.FiscalPeriods.Commands.CreateFiscalPeriod;
using RTSErp.Application.Accounting.FiscalPeriods.Queries;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/fiscalperiods")]
public class FiscalPeriodsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var result = await Mediator.Send(new GetFiscalPeriodsQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFiscalPeriodCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(List), new { id }, new { id });
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id)
    {
        await Mediator.Send(new CloseFiscalPeriodCommand { Id = id });
        return NoContent();
    }

    [HttpPost("{id:guid}/open")]
    public async Task<IActionResult> Open(Guid id)
    {
        await Mediator.Send(new OpenFiscalPeriodCommand { Id = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var period = await db.FiscalPeriods.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (period is null) return NotFound();

        var hasEntries = await db.JournalEntries.AnyAsync(e => !e.IsDeleted && e.FiscalPeriodId == id, ct);
        if (hasEntries)
            return Conflict(new { message = "Fiscal period contains journal entries and cannot be deleted. Close or lock it instead." });

        period.IsDeleted = true;
        period.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
