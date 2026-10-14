using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;
using RTSErp.Infrastructure.Persistence;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Manager,Accountant,SalesManager,Purchasing")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/returns")]
public class ReturnsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var items = await db.JournalEntries
            .Include(j => j.Currency)
            .Include(j => j.Lines)
            .Where(j => !j.IsDeleted
                && j.Status == JournalEntryStatus.Posted
                && (j.ReferenceType == ReferenceType.CustomerReturn || j.ReferenceType == ReferenceType.SupplierReturn))
            .OrderByDescending(j => j.EntryDate)
            .ThenByDescending(j => j.CreatedAt)
            .Select(j => new
            {
                j.Id,
                j.EntryDate,
                j.EntryNumber,
                j.ReferenceNumber,
                j.ReferenceId,
                type = j.ReferenceType == ReferenceType.CustomerReturn ? "Customer Return" : "Supplier Return",
                direction = j.ReferenceType == ReferenceType.CustomerReturn ? "customer" : "supplier",
                j.Description,
                currencyCode = j.Currency.Code,
                amount = j.ReferenceType == ReferenceType.CustomerReturn
                    ? j.Lines.Where(l => l.Description != null && l.Description.StartsWith("Reduce receivable")).Sum(l => l.Credit)
                    : j.Lines.Where(l => l.Description != null && l.Description.StartsWith("Reduce payable")).Sum(l => l.Debit)
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("customer-invoices")]
    public async Task<IActionResult> CustomerInvoices(
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var items = await db.CustomerInvoices
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Where(i => !i.IsDeleted
                && i.Status != InvoiceStatus.Draft
                && i.Status != InvoiceStatus.Cancelled)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                partyName = i.Customer.Name,
                i.InvoiceDate,
                i.CurrencyId,
                i.ExchangeRate,
                warehouseId = i.SalesOrder != null ? (Guid?)i.SalesOrder.WarehouseId : null,
                lines = i.Lines.Where(l => !l.IsDeleted).Select(l => new
                {
                    id = l.Id,
                    l.ProductId,
                    productName = l.Product.Name,
                    l.Quantity,
                    l.UnitPrice,
                    l.DiscountPercent,
                    l.TaxRate
                })
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("supplier-invoices")]
    public async Task<IActionResult> SupplierInvoices(
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var items = await db.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.PurchaseReceipt)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Where(i => !i.IsDeleted
                && i.Status != InvoiceStatus.Draft
                && i.Status != InvoiceStatus.Cancelled)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                partyName = i.Supplier.Name,
                i.InvoiceDate,
                i.CurrencyId,
                i.ExchangeRate,
                warehouseId = i.PurchaseReceipt != null ? (Guid?)i.PurchaseReceipt.WarehouseId : null,
                lines = i.Lines.Where(l => !l.IsDeleted && l.ProductId.HasValue).Select(l => new
                {
                    id = l.Id,
                    productId = l.ProductId!.Value,
                    productName = l.Product != null ? l.Product.Name : l.Description,
                    l.Quantity,
                    l.UnitPrice,
                    l.DiscountPercent,
                    l.TaxRate
                })
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("customer")]
    [Authorize(Roles = "Admin,Manager,Accountant,SalesManager")]
    public async Task<IActionResult> CustomerReturn(
        CustomerReturnRequest req,
        [FromServices] ApplicationDbContext db,
        [FromServices] IAccountingService accounting,
        [FromServices] IInventoryService inventory,
        [FromServices] ICurrentUserService user,
        CancellationToken ct)
    {
        var invoice = await db.CustomerInvoices
            .Include(i => i.Customer)
            .Include(i => i.Currency)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(i => i.Id == req.InvoiceId && !i.IsDeleted, ct);

        if (invoice is null) return NotFound(new { message = "Customer invoice not found." });
        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
            return BadRequest(new { message = "Only posted customer invoices can be returned." });
        if (req.Lines.Count == 0 || req.Lines.All(l => l.Quantity <= 0))
            return BadRequest(new { message = "Enter at least one return quantity." });

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var requested = req.Lines.Where(x => x.Quantity > 0).ToList();
        var selectedLines = invoice.Lines
            .Where(l => requested.Any(r => r.InvoiceLineId == l.Id))
            .ToList();

        if (selectedLines.Count != requested.Count)
            return BadRequest(new { message = "One or more invoice lines are invalid." });

        var previousByProduct = await db.InventoryMovements
            .Where(m => !m.IsDeleted
                && m.ReferenceType == "CustomerReturn"
                && m.ReferenceId == invoice.Id
                && m.MovementType == InventoryMovementType.ReturnFromCustomer)
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, ct);

        foreach (var group in requested.Join(selectedLines, r => r.InvoiceLineId, l => l.Id, (r, l) => new { r, l })
                     .GroupBy(x => x.l.ProductId))
        {
            var originalQty = invoice.Lines.Where(l => !l.IsDeleted && l.ProductId == group.Key).Sum(l => l.Quantity);
            var alreadyReturned = previousByProduct.GetValueOrDefault(group.Key);
            var nowRequested = group.Sum(x => x.r.Quantity);
            if (alreadyReturned + nowRequested > originalQty)
                return BadRequest(new { message = $"Return quantity exceeds the original invoiced quantity for {group.First().l.Product.Name}." });
        }

        var arId = invoice.Customer.ReceivableAccountId
            ?? (await db.Accounts.FirstOrDefaultAsync(a => a.Code == "1301" && !a.IsDeleted, ct))?.Id;
        var vatOutputId = (await db.Accounts.FirstOrDefaultAsync(a => a.Code == "2200" && !a.IsDeleted, ct))?.Id;
        var defaultSalesId = (await db.Accounts.FirstOrDefaultAsync(a => a.Code == "4100" && !a.IsDeleted, ct))?.Id;
        var defaultInventoryId = (await db.Accounts.FirstOrDefaultAsync(a => a.Code == "1502" && !a.IsDeleted, ct))?.Id;
        var defaultCogsId = (await db.Accounts.FirstOrDefaultAsync(a => a.Code == "5100" && !a.IsDeleted, ct))?.Id;

        if (!arId.HasValue) return BadRequest(new { message = "Trade Receivables account is not configured." });

        var lines = new List<JournalEntryLineRequest>();
        decimal totalReturn = 0;
        int sort = 0;

        foreach (var x in requested)
        {
            var line = selectedLines.First(l => l.Id == x.InvoiceLineId);
            if (x.Quantity > line.Quantity)
                return BadRequest(new { message = $"Return quantity exceeds invoice quantity for {line.Product.Name}." });

            var gross = line.UnitPrice * x.Quantity;
            var discount = gross * (line.DiscountPercent / 100m);
            var tax = gross * line.TaxRate;
            var net = gross - discount + tax;
            totalReturn += net;

            var salesId = line.Product.SalesAccountId ?? defaultSalesId;
            if (!salesId.HasValue) return BadRequest(new { message = $"Sales account missing for {line.Product.Name}." });

            lines.Add(new() { AccountId = salesId.Value, Debit = gross, Credit = 0, Description = $"Customer return: {line.Product.Name}", SortOrder = ++sort });
            if (discount > 0)
                lines.Add(new() { AccountId = salesId.Value, Debit = 0, Credit = discount, Description = $"Returned discount: {line.Product.Name}", SortOrder = ++sort });
            if (tax > 0)
            {
                if (!vatOutputId.HasValue) return BadRequest(new { message = "VAT Payable account is not configured." });
                lines.Add(new() { AccountId = vatOutputId.Value, Debit = tax, Credit = 0, Description = "Reverse output VAT", SortOrder = ++sort });
            }
            lines.Add(new() { AccountId = arId.Value, Debit = 0, Credit = net, Description = $"Reduce receivable: {invoice.Customer.Name}", SortOrder = ++sort });

            var (_, currentAvgCost) = await inventory.GetBalanceAsync(line.ProductId, req.WarehouseId, ct);
            var unitCost = line.UnitCost > 0 ? line.UnitCost : currentAvgCost;
            var inventoryId = line.Product.InventoryAccountId ?? defaultInventoryId;
            var cogsId = line.Product.COGSAccountId ?? defaultCogsId;
            var cost = unitCost * x.Quantity;

            if (cost > 0 && inventoryId.HasValue && cogsId.HasValue)
            {
                lines.Add(new() { AccountId = inventoryId.Value, Debit = cost, Credit = 0, Description = $"Inventory returned: {line.Product.Name}", SortOrder = ++sort });
                lines.Add(new() { AccountId = cogsId.Value, Debit = 0, Credit = cost, Description = $"Reverse COGS: {line.Product.Name}", SortOrder = ++sort });
            }

            await inventory.MoveInventoryAsync(
                line.ProductId, req.WarehouseId, x.Quantity, unitCost,
                InventoryMovementType.ReturnFromCustomer, req.ReturnDate,
                referenceType: "CustomerReturn", referenceId: invoice.Id,
                referenceNumber: invoice.InvoiceNumber,
                notes: req.Notes ?? $"Customer return against {invoice.InvoiceNumber}",
                createdBy: user.UserId, cancellationToken: ct);
        }

        var returnNumber = await NextReturnNumberAsync(db, "CRTN", ct);
        var result = await accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            EntryDate = req.ReturnDate,
            Description = $"Customer Return {returnNumber} - {invoice.Customer.Name} - Invoice {invoice.InvoiceNumber}",
            ReferenceType = ReferenceType.CustomerReturn,
            ReferenceId = invoice.Id,
            ReferenceNumber = returnNumber,
            CurrencyId = invoice.CurrencyId,
            ExchangeRate = invoice.ExchangeRate,
            PostImmediately = true,
            Lines = lines,
            CreatedBy = user.UserId
        }, ct);

        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors) });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(new { id = result.EntryId, returnNumber, amount = totalReturn });
    }

    [HttpPost("supplier")]
    [Authorize(Roles = "Admin,Manager,Accountant,Purchasing")]
    public async Task<IActionResult> SupplierReturn(
        SupplierReturnRequest req,
        [FromServices] ApplicationDbContext db,
        [FromServices] IAccountingService accounting,
        [FromServices] IInventoryService inventory,
        [FromServices] ICurrentUserService user,
        CancellationToken ct)
    {
        var invoice = await db.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Currency)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(i => i.Id == req.InvoiceId && !i.IsDeleted, ct);

        if (invoice is null) return NotFound(new { message = "Supplier invoice not found." });
        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
            return BadRequest(new { message = "Only posted supplier invoices can be returned." });

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var requested = req.Lines.Where(x => x.Quantity > 0).ToList();
        if (requested.Count == 0) return BadRequest(new { message = "Enter at least one return quantity." });

        var selectedLines = invoice.Lines
            .Where(l => l.ProductId.HasValue && requested.Any(r => r.InvoiceLineId == l.Id))
            .ToList();
        if (selectedLines.Count != requested.Count)
            return BadRequest(new { message = "One or more supplier invoice lines are invalid or are not product lines." });

        var previousByProduct = await db.InventoryMovements
            .Where(m => !m.IsDeleted
                && m.ReferenceType == "SupplierReturn"
                && m.ReferenceId == invoice.Id
                && m.MovementType == InventoryMovementType.ReturnToSupplier)
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = -g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, ct);

        foreach (var group in requested.Join(selectedLines, r => r.InvoiceLineId, l => l.Id, (r, l) => new { r, l })
                     .GroupBy(x => x.l.ProductId!.Value))
        {
            var originalQty = invoice.Lines.Where(l => !l.IsDeleted && l.ProductId == group.Key).Sum(l => l.Quantity);
            var alreadyReturned = previousByProduct.GetValueOrDefault(group.Key);
            var nowRequested = group.Sum(x => x.r.Quantity);
            if (alreadyReturned + nowRequested > originalQty)
                return BadRequest(new { message = $"Return quantity exceeds the original invoiced quantity for {group.First().l.Product?.Name}." });
        }

        var apId = invoice.Supplier.PayableAccountId
            ?? (await db.Accounts.FirstOrDefaultAsync(a => a.Code == "2101" && !a.IsDeleted, ct))?.Id;
        var vatInputId = (await db.Accounts.FirstOrDefaultAsync(a => a.Code == "1400" && !a.IsDeleted, ct))?.Id;
        var defaultPurchaseId = (await db.Accounts.FirstOrDefaultAsync(a => a.Code == "1502" && !a.IsDeleted, ct))?.Id
            ?? (await db.Accounts.FirstOrDefaultAsync(a => a.Code == "6600" && !a.IsDeleted, ct))?.Id;
        var varianceAccountId = (await db.Accounts.FirstOrDefaultAsync(a => a.Code == "6600" && !a.IsDeleted, ct))?.Id;

        if (!apId.HasValue) return BadRequest(new { message = "Trade Payables account is not configured." });

        var lines = new List<JournalEntryLineRequest>();
        decimal totalReturn = 0;
        int sort = 0;

        foreach (var x in requested)
        {
            var line = selectedLines.First(l => l.Id == x.InvoiceLineId);
            var product = line.Product!;
            if (x.Quantity > line.Quantity)
                return BadRequest(new { message = $"Return quantity exceeds invoice quantity for {product.Name}." });

            var gross = line.UnitPrice * x.Quantity;
            var discount = gross * (line.DiscountPercent / 100m);
            var tax = gross * line.TaxRate;
            var net = gross - discount + tax;
            totalReturn += net;

            var purchaseId = product.PurchaseAccountId ?? product.InventoryAccountId ?? defaultPurchaseId;
            if (!purchaseId.HasValue) return BadRequest(new { message = $"Purchase/Inventory account missing for {product.Name}." });

            var (_, avgCost) = await inventory.GetBalanceAsync(product.Id, req.WarehouseId, ct);
            var purchaseBase = gross - discount;
            var unitCost = avgCost > 0 ? avgCost : Math.Max(0, purchaseBase / x.Quantity);
            var stockCost = unitCost * x.Quantity;

            lines.Add(new() { AccountId = apId.Value, Debit = net, Credit = 0, Description = $"Reduce payable: {invoice.Supplier.Name}", SortOrder = ++sort });
            lines.Add(new() { AccountId = purchaseId.Value, Debit = 0, Credit = stockCost, Description = $"Supplier return stock: {product.Name}", SortOrder = ++sort });
            if (tax > 0)
            {
                if (!vatInputId.HasValue) return BadRequest(new { message = "VAT Receivable account is not configured." });
                lines.Add(new() { AccountId = vatInputId.Value, Debit = 0, Credit = tax, Description = "Reverse input VAT", SortOrder = ++sort });
            }

            var variance = purchaseBase - stockCost;
            if (Math.Abs(variance) > 0.001m)
            {
                if (!varianceAccountId.HasValue) return BadRequest(new { message = "Purchase return variance account (6600) is not configured." });
                lines.Add(new()
                {
                    AccountId = varianceAccountId.Value,
                    Debit = variance < 0 ? Math.Abs(variance) : 0,
                    Credit = variance > 0 ? variance : 0,
                    Description = $"Purchase return cost variance: {product.Name}",
                    SortOrder = ++sort
                });
            }

            try
            {
                await inventory.MoveInventoryAsync(
                    product.Id, req.WarehouseId, -x.Quantity, unitCost,
                    InventoryMovementType.ReturnToSupplier, req.ReturnDate,
                    referenceType: "SupplierReturn", referenceId: invoice.Id,
                    referenceNumber: invoice.InvoiceNumber,
                    notes: req.Notes ?? $"Supplier return against {invoice.InvoiceNumber}",
                    createdBy: user.UserId, cancellationToken: ct);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = $"Cannot return {product.Name}: {ex.Message}" });
            }
        }

        var returnNumber = await NextReturnNumberAsync(db, "SRTN", ct);
        var result = await accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            EntryDate = req.ReturnDate,
            Description = $"Supplier Return {returnNumber} - {invoice.Supplier.Name} - Invoice {invoice.InvoiceNumber}",
            ReferenceType = ReferenceType.SupplierReturn,
            ReferenceId = invoice.Id,
            ReferenceNumber = returnNumber,
            CurrencyId = invoice.CurrencyId,
            ExchangeRate = invoice.ExchangeRate,
            PostImmediately = true,
            Lines = lines,
            CreatedBy = user.UserId
        }, ct);

        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors) });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(new { id = result.EntryId, returnNumber, amount = totalReturn });
    }

    private static async Task<string> NextReturnNumberAsync(IApplicationDbContext db, string prefix, CancellationToken ct)
    {
        var yearPrefix = $"{prefix}{DateTime.UtcNow.Year}-";
        var last = await db.JournalEntries
            .Where(j => j.ReferenceNumber != null && j.ReferenceNumber.StartsWith(yearPrefix))
            .OrderByDescending(j => j.ReferenceNumber)
            .Select(j => j.ReferenceNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && last.Length > yearPrefix.Length &&
            int.TryParse(last[yearPrefix.Length..], out var n))
            seq = n + 1;

        return $"{yearPrefix}{seq:D5}";
    }

    public record ReturnLineRequest(Guid InvoiceLineId, decimal Quantity);
    public record CustomerReturnRequest(Guid InvoiceId, Guid WarehouseId, DateOnly ReturnDate, string? Notes, List<ReturnLineRequest> Lines);
    public record SupplierReturnRequest(Guid InvoiceId, Guid WarehouseId, DateOnly ReturnDate, string? Notes, List<ReturnLineRequest> Lines);
}
