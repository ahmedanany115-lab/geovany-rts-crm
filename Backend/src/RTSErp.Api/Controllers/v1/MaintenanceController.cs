using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
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

    /// <summary>
    /// Generate a Customer Invoice from an active Maintenance Contract.
    /// Safe to call multiple times — prevents duplicate invoices per contract
    /// unless a billing period is specified.
    /// </summary>
    [HttpPost("contracts/{id:guid}/generate-invoice")]
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> GenerateContractInvoice(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] ICurrentUserService user,
        CancellationToken ct)
    {
        var contract = await db.MaintenanceContracts
            .Include(c => c.Customer)
            .Include(c => c.Currency)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
        if (contract is null) return NotFound();

        if (contract.Status != Domain.Entities.Maintenance.ContractStatus.Active)
            return BadRequest(new { message = "Contract must be Active to generate an invoice." });

        if (contract.ContractValue <= 0)
            return BadRequest(new { message = "Contract value must be > 0." });

        // Prevent duplicate: check if an unposted invoice already exists for this contract
        var existing = await db.CustomerInvoices
            .FirstOrDefaultAsync(i => i.Notes != null && i.Notes.Contains(contract.ContractNumber)
                && !i.IsDeleted && i.Status == Domain.Enums.InvoiceStatus.Draft, ct);
        if (existing is not null)
            return Conflict(new { message = "A draft invoice already exists for this contract.", invoiceId = existing.Id });

        var count = await db.CustomerInvoices.CountAsync(ct);
        var invoiceNumber = $"INV-{DateTime.UtcNow.Year}-{(count + 1):D5}";

        var invoice = new Domain.Entities.Operational.CustomerInvoice
        {
            InvoiceNumber = invoiceNumber,
            CustomerId    = contract.CustomerId,
            InvoiceDate   = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate       = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CurrencyId    = contract.CurrencyId,
            ExchangeRate  = 1m,
            Status        = Domain.Enums.InvoiceStatus.Draft,
            Notes         = $"Maintenance Contract {contract.ContractNumber} — {contract.StartDate} to {contract.EndDate}",
            TotalAmount   = contract.ContractValue,
            SubTotal      = contract.ContractValue,
            TaxAmount     = 0,
            CreatedBy     = user.UserId,
        };

        db.CustomerInvoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        return Ok(new { invoiceId = invoice.Id, invoiceNumber });
    }

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
