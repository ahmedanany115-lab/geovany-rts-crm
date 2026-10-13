using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Operational.PurchaseOrders;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Manager,Accountant,Purchasing,Sales")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/purchaseorders")]
public class PurchaseOrdersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PurchaseOrderStatus? status, [FromQuery] Guid? supplierId)
        => Ok(await Mediator.Send(new GetPurchaseOrdersQuery { Status = status, SupplierId = supplierId }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await Mediator.Send(new GetPurchaseOrderQuery { Id = id }));

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseOrderCommand cmd)
    { var id = await Mediator.Send(cmd); return CreatedAtAction(nameof(Get), new { id }, new { id }); }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    { await Mediator.Send(new ApprovePurchaseOrderCommand { Id = id }); return NoContent(); }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeletePurchaseOrderCommand { Id = id });
        return NoContent();
    }
}

[Authorize(Roles = "Admin,Manager,Accountant,Purchasing,Sales")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/purchasereceipts")]
public class PurchaseReceiptsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? purchaseOrderId, [FromQuery] Guid? supplierId)
        => Ok(await Mediator.Send(new GetPurchaseReceiptsQuery { PurchaseOrderId = purchaseOrderId, SupplierId = supplierId }));

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseReceiptCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Reverse(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] IInventoryService inventory,
        [FromServices] IAccountingService accounting,
        [FromServices] ICurrentUserService user,
        CancellationToken ct)
    {
        var receipt = await db.PurchaseReceipts
            .Include(r => r.Lines).ThenInclude(l => l.PurchaseOrderLine)
            .Include(r => r.PurchaseOrder).ThenInclude(o => o.Lines)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
        if (receipt is null) return NotFound();

        var hasInvoice = await db.SupplierInvoices.AnyAsync(i =>
            !i.IsDeleted && i.PurchaseReceiptId == id, ct);
        if (hasInvoice)
            return BadRequest(new { message = "This receipt is linked to a supplier invoice. Delete/reverse that invoice first." });

        if (receipt.JournalEntryId.HasValue)
        {
            var reversed = await accounting.ReverseJournalEntryAsync(
                receipt.JournalEntryId.Value,
                $"Reversed purchase receipt {receipt.ReceiptNumber}",
                DateOnly.FromDateTime(DateTime.UtcNow),
                ct);
            if (!reversed.Succeeded)
                return BadRequest(new { message = string.Join(", ", reversed.Errors) });
        }

        foreach (var line in receipt.Lines.Where(l => !l.IsDeleted))
        {
            try
            {
                await inventory.MoveInventoryAsync(
                    line.ProductId,
                    receipt.WarehouseId,
                    -line.Quantity,
                    line.UnitCost,
                    InventoryMovementType.ReturnToSupplier,
                    DateOnly.FromDateTime(DateTime.UtcNow),
                    referenceType: "PurchaseReceiptReversal",
                    referenceId: receipt.Id,
                    referenceNumber: receipt.ReceiptNumber,
                    notes: $"Reversal of {receipt.ReceiptNumber}",
                    createdBy: user.UserId,
                    cancellationToken: ct);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = $"Cannot reverse receipt because stock is no longer available: {ex.Message}" });
            }

            line.PurchaseOrderLine.ReceivedQuantity = Math.Max(0, line.PurchaseOrderLine.ReceivedQuantity - line.Quantity);
            line.PurchaseOrderLine.ModifiedAt = DateTime.UtcNow;
            line.PurchaseOrderLine.ModifiedBy = user.UserId;
            line.IsDeleted = true;
            line.ModifiedAt = DateTime.UtcNow;
            line.ModifiedBy = user.UserId;
        }

        var activeOrderLines = receipt.PurchaseOrder.Lines.Where(l => !l.IsDeleted).ToList();
        var anyReceived = activeOrderLines.Any(l => l.ReceivedQuantity > 0);
        var allReceived = activeOrderLines.Count > 0 && activeOrderLines.All(l => l.ReceivedQuantity >= l.Quantity);
        receipt.PurchaseOrder.Status = allReceived
            ? PurchaseOrderStatus.Received
            : anyReceived ? PurchaseOrderStatus.PartiallyReceived : PurchaseOrderStatus.Approved;
        receipt.PurchaseOrder.ModifiedAt = DateTime.UtcNow;
        receipt.PurchaseOrder.ModifiedBy = user.UserId;

        receipt.IsDeleted = true;
        receipt.ModifiedAt = DateTime.UtcNow;
        receipt.ModifiedBy = user.UserId;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

[Authorize(Roles = "Admin,Manager,Accountant,Purchasing,Sales")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/supplierinvoices")]
public class SupplierInvoicesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? status, [FromQuery] Guid? supplierId)
        => Ok(await Mediator.Send(new GetSupplierInvoicesQuery { Status = status, SupplierId = supplierId }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplierInvoiceCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id)
    { await Mediator.Send(new PostSupplierInvoiceCommand { Id = id }); return NoContent(); }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrReverse(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] IAccountingService accounting,
        [FromServices] ICurrentUserService user,
        CancellationToken ct)
    {
        var invoice = await db.SupplierInvoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct);
        if (invoice is null) return NotFound();

        if (invoice.PaidAmount > 0 || invoice.Payments.Any(p => !p.IsDeleted))
            return BadRequest(new { message = "Invoice has payments. Reverse/delete its payments first." });

        if (invoice.JournalEntryId.HasValue)
        {
            var reversed = await accounting.ReverseJournalEntryAsync(
                invoice.JournalEntryId.Value,
                $"Cancelled supplier invoice {invoice.InvoiceNumber}",
                DateOnly.FromDateTime(DateTime.UtcNow),
                ct);
            if (!reversed.Succeeded)
                return BadRequest(new { message = string.Join(", ", reversed.Errors) });
        }

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.IsDeleted = true;
        invoice.ModifiedAt = DateTime.UtcNow;
        invoice.ModifiedBy = user.UserId;
        foreach (var line in invoice.Lines)
        {
            line.IsDeleted = true;
            line.ModifiedAt = DateTime.UtcNow;
            line.ModifiedBy = user.UserId;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
