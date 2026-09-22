using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Reports;
using RTSErp.Application.Common.Interfaces;

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
            .Where(i => !i.IsDeleted && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .ToListAsync(ct);

        var payments = await _db.CustomerPayments
            .Where(p => !p.IsDeleted && p.PaymentDate >= from && p.PaymentDate <= to)
            .ToListAsync(ct);

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
            netRevenue       = invoices.Sum(i => i.TotalAmount),
            totalCollected   = payments.Sum(p => p.Amount),
            totalOutstanding = invoices.Sum(i => i.TotalAmount - i.PaidAmount),
        });
    }

    [HttpGet("sales-by-customer")]
    public async Task<IActionResult> SalesByCustomer([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from  = fromDate ?? new DateOnly(today.Year, 1, 1);
        var to    = toDate   ?? today;

        var rows = await _db.CustomerInvoices
            .Include(i => i.Customer)
            .Where(i => !i.IsDeleted && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .GroupBy(i => new { i.CustomerId, i.Customer.Name })
            .Select(g => new
            {
                customerId       = g.Key.CustomerId,
                customerName     = g.Key.Name,
                orderCount       = g.Count(),
                totalRevenue     = g.Sum(i => i.SubTotal),
                totalVat         = g.Sum(i => i.TaxAmount),
                totalCollected   = g.Sum(i => i.PaidAmount),
                totalOutstanding = g.Sum(i => i.TotalAmount - i.PaidAmount),
            })
            .OrderByDescending(r => r.totalRevenue)
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("sales-by-item")]
    public async Task<IActionResult> SalesByItem([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from  = fromDate ?? new DateOnly(today.Year, 1, 1);
        var to    = toDate   ?? today;

        var rows = await _db.CustomerInvoiceLines
            .Include(l => l.Invoice).Include(l => l.Product)
            .Where(l => !l.IsDeleted && !l.Invoice.IsDeleted
                     && l.Invoice.InvoiceDate >= from && l.Invoice.InvoiceDate <= to
                     && l.ProductId != null)
            .GroupBy(l => new { l.ProductId, l.Product!.Name, l.Product.SKU })
            .Select(g => new
            {
                productId    = g.Key.ProductId,
                productName  = g.Key.Name,
                productSku   = g.Key.SKU,
                quantitySold = g.Sum(l => l.Quantity),
                totalRevenue = g.Sum(l => l.LineTotal),
                totalDiscount= g.Sum(l => l.DiscountAmount),
                totalVat     = g.Sum(l => l.TaxAmount),
                netRevenue   = g.Sum(l => l.NetAmount),
            })
            .OrderByDescending(r => r.totalRevenue)
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("sales-by-rep")]
    public async Task<IActionResult> SalesByRep([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from  = fromDate ?? new DateOnly(today.Year, 1, 1);
        var to    = toDate   ?? today;

        var rows = await _db.CustomerInvoices
            .Include(i => i.Customer)
            .Where(i => !i.IsDeleted && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .GroupBy(i => new { i.Customer.AssignedSalesRepId, i.Customer.AssignedSalesRepName })
            .Select(g => new
            {
                salesRepId       = g.Key.AssignedSalesRepId,
                salesRepName     = g.Key.AssignedSalesRepName ?? "Unassigned",
                orderCount       = g.Count(),
                totalRevenue     = g.Sum(i => i.SubTotal),
                totalCollected   = g.Sum(i => i.PaidAmount),
                totalOutstanding = g.Sum(i => i.TotalAmount - i.PaidAmount),
                commission       = g.Sum(i => i.SubTotal) * 0.015m,   // 1.5% existing rule
            })
            .OrderByDescending(r => r.totalRevenue)
            .ToListAsync(ct);

        return Ok(rows);
    }
}
