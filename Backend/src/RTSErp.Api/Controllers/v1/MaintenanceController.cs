using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Maintenance;
using RTSErp.Domain.Entities.Maintenance;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Manager,SalesManager,Sales,Accountant,SupportAgent")]
public class MaintenanceController : BaseApiController
{
    // ── Contracts ─────────────────────────────────────────────────────────────

    [HttpGet("contracts")]
    public async Task<IActionResult> ListContracts(
        [FromQuery] ContractStatus? status,
        [FromQuery] Guid?   customerId,
        [FromQuery] string? search,
        CancellationToken ct)
        => Ok(await Mediator.Send(new GetMaintenanceContractsQuery
        {
            Status = status, CustomerId = customerId, Search = search
        }, ct));

    [HttpGet("contracts/{id:guid}")]
    public async Task<IActionResult> GetContract(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetMaintenanceContractQuery { Id = id }, ct));

    [HttpPost("contracts")]
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> CreateContract(
        [FromBody] CreateMaintenanceContractCommand cmd, CancellationToken ct)
    {
        var id = await Mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetContract), new { id }, new { id });
    }

    [HttpPatch("contracts/{id:guid}/status")]
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeStatusRequest req,
        CancellationToken ct)
    {
        await Mediator.Send(new ChangeContractStatusCommand
        {
            ContractId = id, NewStatus = req.Status
        }, ct);
        return NoContent();
    }

    // ── Visits ────────────────────────────────────────────────────────────────

    [HttpGet("visits")]
    public async Task<IActionResult> ListVisits(
        [FromQuery] Guid?        contractId,
        [FromQuery] VisitStatus? status,
        [FromQuery] DateOnly?    fromDate,
        [FromQuery] DateOnly?    toDate,
        CancellationToken ct)
        => Ok(await Mediator.Send(new GetMaintenanceVisitsQuery
        {
            ContractId = contractId, Status = status,
            FromDate = fromDate, ToDate = toDate
        }, ct));

    [HttpPost("visits/schedule")]
    public async Task<IActionResult> ScheduleVisit(
        [FromBody] ScheduleVisitCommand cmd, CancellationToken ct)
    {
        var id = await Mediator.Send(cmd, ct);
        return Ok(new { id });
    }

    [HttpPatch("visits/{id:guid}/complete")]
    public async Task<IActionResult> CompleteVisit(
        Guid id, [FromBody] CompleteVisitCommand cmd, CancellationToken ct)
    {
        cmd.VisitId = id;
        await Mediator.Send(cmd, ct);
        return NoContent();
    }

    [HttpPatch("visits/{id:guid}/cancel")]
    public async Task<IActionResult> CancelVisit(
        Guid id, [FromBody] CancelVisitRequest req, CancellationToken ct)
    {
        await Mediator.Send(new CancelVisitCommand { VisitId = id, Reason = req.Reason }, ct);
        return NoContent();
    }

    // ── Equipment ─────────────────────────────────────────────────────────────

    [HttpPost("equipment")]
    public async Task<IActionResult> UpsertEquipment(
        [FromBody] UpsertEquipmentCommand cmd, CancellationToken ct)
    {
        var id = await Mediator.Send(cmd, ct);
        return Ok(new { id });
    }

    [HttpDelete("equipment/{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> DeleteEquipment(
        Guid id,
        [FromServices] RTSErp.Application.Common.Interfaces.IApplicationDbContext db,
        CancellationToken ct)
    {
        var eq = await db.ContractEquipments.FindAsync([id], ct);
        if (eq is null || eq.IsDeleted) return NotFound();
        eq.IsDeleted  = true;
        eq.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Request records ───────────────────────────────────────────────────────
    public record ChangeStatusRequest(ContractStatus Status);
    public record CancelVisitRequest(string? Reason);
}
