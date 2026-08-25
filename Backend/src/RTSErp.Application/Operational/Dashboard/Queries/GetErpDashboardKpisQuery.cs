using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Operational.Dashboard.Queries;

public class ErpDashboardKpiDto
{
    // Sales
    public decimal TotalSalesThisMonth { get; set; }
    public decimal TotalSalesThisYear { get; set; }
    public int PendingSalesOrders { get; set; }

    // Purchasing
    public decimal TotalPurchasesThisMonth { get; set; }
    public int PendingPurchaseOrders { get; set; }

    // AR/AP
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }

    // Bank
    public List<BankBalanceDto> BankBalances { get; set; } = [];

    // Inventory
    public decimal InventoryValue { get; set; }
    public int LowStockProducts { get; set; }

    // Cheques
    public decimal OutstandingCheques { get; set; }
    public int OutstandingChequeCount { get; set; }

    // Commission
    public decimal PendingCommission { get; set; }

    // VAT
    public decimal VatPayable { get; set; }
    public decimal VatReceivable { get; set; }
}

public class BankBalanceDto
{
    public string BankName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class GetErpDashboardKpisQuery : IRequest<ErpDashboardKpiDto> { }

public class GetErpDashboardKpisQueryHandler : IRequestHandler<GetErpDashboardKpisQuery, ErpDashboardKpiDto>
{
    private readonly IApplicationDbContext _db;
    public GetErpDashboardKpisQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ErpDashboardKpiDto> Handle(GetErpDashboardKpisQuery req, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateOnly(now.Year, now.Month, 1);
        var yearStart = new DateOnly(now.Year, 1, 1);

        var dto = new ErpDashboardKpiDto();

        // Sales this month / year (posted customer invoices)
        var salesInvoices = await _db.CustomerInvoices
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync(ct);

        dto.TotalSalesThisMonth = salesInvoices.Where(i => i.InvoiceDate >= monthStart).Sum(i => i.TotalAmount);
        dto.TotalSalesThisYear = salesInvoices.Where(i => i.InvoiceDate >= yearStart).Sum(i => i.TotalAmount);
        dto.TotalReceivables = salesInvoices.Sum(i => i.TotalAmount - i.PaidAmount);

        // Purchases
        var supplierInvoices = await _db.SupplierInvoices
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync(ct);

        dto.TotalPurchasesThisMonth = supplierInvoices.Where(i => i.InvoiceDate >= monthStart).Sum(i => i.TotalAmount);
        dto.TotalPayables = supplierInvoices.Sum(i => i.TotalAmount - i.PaidAmount);

        // Pending orders
        dto.PendingSalesOrders = await _db.SalesOrders
            .CountAsync(o => !o.IsDeleted && (o.Status == SalesOrderStatus.Approved || o.Status == SalesOrderStatus.PartiallyDelivered), ct);
        dto.PendingPurchaseOrders = await _db.PurchaseOrders
            .CountAsync(o => !o.IsDeleted && (o.Status == PurchaseOrderStatus.Approved || o.Status == PurchaseOrderStatus.PartiallyReceived), ct);

        // Bank balances
        var bankAccounts = await _db.BankAccounts
            .Include(b => b.Currency).Where(b => !b.IsDeleted && b.IsActive).ToListAsync(ct);
        dto.BankBalances = bankAccounts.Select(b => new BankBalanceDto
        {
            BankName = b.Name, Currency = b.Currency.Code, Balance = b.CurrentBalance
        }).ToList();

        // Inventory value
        dto.InventoryValue = await _db.InventoryBalances
            .Where(b => !b.IsDeleted && b.Quantity > 0)
            .SumAsync(b => b.Quantity * b.AverageCost, ct);

        // Low stock: load active products with their total balance, count below minimum
        var products = await _db.Products
            .Where(p => !p.IsDeleted && p.IsActive)
            .Select(p => new { p.MinimumStock, Total = p.InventoryBalances.Sum(b => b.Quantity) })
            .ToListAsync(ct);
        dto.LowStockProducts = products.Count(p => p.Total < p.MinimumStock);

        // Outstanding cheques
        var outstandingCheques = await _db.Cheques
            .Where(c => !c.IsDeleted && (c.Status == ChequeStatus.Received || c.Status == ChequeStatus.Deposited))
            .ToListAsync(ct);
        dto.OutstandingCheques = outstandingCheques.Sum(c => c.Amount);
        dto.OutstandingChequeCount = outstandingCheques.Count;

        // Pending commission
        dto.PendingCommission = await _db.SalesCommissions
            .Where(c => !c.IsDeleted && c.Status == CommissionStatus.Pending)
            .SumAsync(c => c.CommissionAmount, ct);

        return dto;
    }
}
