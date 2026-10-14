using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Reports;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/reports")]
public class ReportsController : BaseApiController
{
    private readonly IApplicationDbContext _db;
    public ReportsController(IApplicationDbContext db) => _db = db;

    [HttpGet("income-statement")]
    public async Task<IActionResult> IncomeStatement([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await Mediator.Send(new GetIncomeStatementQuery
        {
            FromDate = fromDate ?? new DateOnly(today.Year, 1, 1), ToDate = toDate ?? today
        }, ct));
    }

    [HttpGet("profit-loss")]
    public async Task<IActionResult> ProfitLoss([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
        => await IncomeStatement(fromDate, toDate, ct);

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet([FromQuery] DateOnly? asOfDate, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await Mediator.Send(new GetBalanceSheetQuery { AsOfDate = asOfDate ?? today }, ct));
    }

    // ── Sales Reports ─────────────────────────────────────────────────────────
    [HttpGet("sales-summary")]
    public async Task<IActionResult> SalesSummary([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from  = fromDate ?? new DateOnly(today.Year, 1, 1);
        var to    = toDate   ?? today;

        var invoices = await _db.CustomerInvoices
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Cancelled && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync(ct);

        var payments = await _db.CustomerPayments
            .Where(p => !p.IsDeleted && p.PaymentDate >= from && p.PaymentDate <= to)
            .ToListAsync(ct);

        var arIds = await GetReceivableAccountIdsAsync(ct);
        var returnEntries = await _db.JournalEntries
            .Include(j => j.Lines)
            .Where(j => !j.IsDeleted && j.Status == JournalEntryStatus.Posted
                && j.ReferenceType == ReferenceType.CustomerReturn
                && j.EntryDate >= from && j.EntryDate <= to)
            .ToListAsync(ct);
        var totalReturns = returnEntries.Sum(j =>
            j.Lines.Where(l => arIds.Contains(l.AccountId)).Sum(l => l.CreditBase - l.DebitBase));

        var orders = await _db.SalesOrders
            .Where(o => !o.IsDeleted && o.OrderDate >= from && o.OrderDate <= to)
            .CountAsync(ct);

        return Ok(new
        {
            invoiceCount     = invoices.Count,
            orderCount       = orders,
            totalRevenue     = invoices.Sum(i => i.SubTotal),
            totalVat         = invoices.Sum(i => i.TaxAmount),
            totalDiscounts   = invoices.Sum(i => i.DiscountAmount),
            netRevenue       = invoices.Sum(i => i.TotalAmount) - totalReturns,
            totalReturns,
            totalCollected   = payments.Sum(p => p.Amount),
            totalOutstanding = invoices.Sum(i => i.TotalAmount - i.PaidAmount) - totalReturns,
        });
    }

    [HttpGet("sales-by-customer")]
    public async Task<IActionResult> SalesByCustomer([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from  = fromDate ?? new DateOnly(today.Year, 1, 1);
        var to    = toDate   ?? today;

        var invoices = await _db.CustomerInvoices
            .Include(i => i.Customer)
            .Where(i => !i.IsDeleted && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync(ct);
        var returnMap = await GetCustomerReturnMapAsync(from, to, ct);

        var rows = invoices
            .GroupBy(i => new { i.CustomerId, i.Customer.Name })
            .Select(g =>
            {
                var returns = g.Sum(i => returnMap.GetValueOrDefault(i.Id));
                return new
                {
                    customerId       = g.Key.CustomerId,
                    customerName     = g.Key.Name,
                    orderCount       = g.Count(),
                    totalRevenue     = g.Sum(i => i.SubTotal),
                    totalVat         = g.Sum(i => i.TaxAmount),
                    totalCollected   = g.Sum(i => i.PaidAmount),
                    totalReturns     = returns,
                    totalOutstanding = g.Sum(i => i.TotalAmount - i.PaidAmount) - returns,
                };
            })
            .OrderByDescending(r => r.totalRevenue)
            .ToList();

        return Ok(rows);
    }

    [HttpGet("sales-by-item")]
    public async Task<IActionResult> SalesByItem([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from  = fromDate ?? new DateOnly(today.Year, 1, 1);
        var to    = toDate   ?? today;

        var lines = await _db.CustomerInvoiceLines
            .Include(l => l.CustomerInvoice)
            .Include(l => l.Product)
            .Where(l => !l.IsDeleted && !l.CustomerInvoice.IsDeleted
                     && l.CustomerInvoice.Status != InvoiceStatus.Draft
                     && l.CustomerInvoice.Status != InvoiceStatus.Cancelled
                     && l.CustomerInvoice.InvoiceDate >= from && l.CustomerInvoice.InvoiceDate <= to)
            .ToListAsync(ct);

        var returnMovements = await _db.InventoryMovements
            .Where(m => !m.IsDeleted
                && m.ReferenceType == "CustomerReturn"
                && m.MovementType == InventoryMovementType.ReturnFromCustomer
                && m.MovementDate >= from && m.MovementDate <= to
                && m.ReferenceId.HasValue)
            .ToListAsync(ct);

        var rows = lines
            .GroupBy(l => new { l.ProductId, l.Product.Name, l.Product.SKU })
            .Select(g =>
            {
                decimal returnedQty = 0, returnedGross = 0, returnedDiscount = 0, returnedVat = 0, returnedNet = 0;
                foreach (var movement in returnMovements.Where(m => m.ProductId == g.Key.ProductId))
                {
                    var source = lines.FirstOrDefault(l =>
                        l.CustomerInvoiceId == movement.ReferenceId!.Value && l.ProductId == movement.ProductId);
                    if (source is null) continue;

                    var gross = movement.Quantity * source.UnitPrice;
                    var discount = gross * (source.DiscountPercent / 100m);
                    var vat = gross * source.TaxRate;
                    returnedQty += movement.Quantity;
                    returnedGross += gross;
                    returnedDiscount += discount;
                    returnedVat += vat;
                    returnedNet += gross - discount + vat;
                }

                return new
                {
                    productId     = g.Key.ProductId,
                    productName   = g.Key.Name,
                    productSku    = g.Key.SKU,
                    quantitySold  = g.Sum(l => l.Quantity) - returnedQty,
                    returnedQty,
                    totalRevenue  = g.Sum(l => l.LineTotal) - returnedGross,
                    totalDiscount = g.Sum(l => l.DiscountAmount) - returnedDiscount,
                    totalVat      = g.Sum(l => l.TaxAmount) - returnedVat,
                    netRevenue    = g.Sum(l => l.NetAmount) - returnedNet,
                };
            })
            .OrderByDescending(r => r.totalRevenue)
            .ToList();

        return Ok(rows);
    }

    [HttpGet("sales-by-rep")]
    public async Task<IActionResult> SalesByRep([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from  = fromDate ?? new DateOnly(today.Year, 1, 1);
        var to    = toDate   ?? today;

        var invoices = await _db.CustomerInvoices
            .Include(i => i.Customer)
            .Where(i => !i.IsDeleted && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync(ct);
        var returnMap = await GetCustomerReturnMapAsync(from, to, ct);

        var rows = invoices
            .GroupBy(i => new { i.Customer.AssignedSalesRepId, i.Customer.AssignedSalesRepName })
            .Select(g =>
            {
                var returns = g.Sum(i => returnMap.GetValueOrDefault(i.Id));
                var netSales = g.Sum(i => i.TotalAmount) - returns;
                return new
                {
                    salesRepId       = g.Key.AssignedSalesRepId,
                    salesRepName     = g.Key.AssignedSalesRepName ?? "Unassigned",
                    orderCount       = g.Count(),
                    totalRevenue     = g.Sum(i => i.SubTotal),
                    totalCollected   = g.Sum(i => i.PaidAmount),
                    totalReturns     = returns,
                    totalOutstanding = g.Sum(i => i.TotalAmount - i.PaidAmount) - returns,
                    commission       = netSales * 0.015m,
                };
            })
            .OrderByDescending(r => r.totalRevenue)
            .ToList();

        return Ok(rows);
    }

    private async Task<List<Guid>> GetReceivableAccountIdsAsync(CancellationToken ct)
    {
        var ids = await _db.BusinessPartners
            .Where(b => !b.IsDeleted && b.ReceivableAccountId.HasValue)
            .Select(b => b.ReceivableAccountId!.Value)
            .Distinct()
            .ToListAsync(ct);
        var fallback = await _db.Accounts
            .Where(a => !a.IsDeleted && a.Code == "1301")
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);
        if (fallback.HasValue && !ids.Contains(fallback.Value)) ids.Add(fallback.Value);
        return ids;
    }

    private async Task<Dictionary<Guid, decimal>> GetCustomerReturnMapAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var arIds = await GetReceivableAccountIdsAsync(ct);
        var entries = await _db.JournalEntries
            .Include(j => j.Lines)
            .Where(j => !j.IsDeleted && j.Status == JournalEntryStatus.Posted
                && j.ReferenceType == ReferenceType.CustomerReturn
                && j.ReferenceId.HasValue
                && j.EntryDate >= from && j.EntryDate <= to)
            .ToListAsync(ct);

        return entries
            .GroupBy(j => j.ReferenceId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(j => j.Lines.Where(l => arIds.Contains(l.AccountId))
                    .Sum(l => l.CreditBase - l.DebitBase)));
    }
}
