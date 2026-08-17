using RTSErp.Domain.Common;
using RTSErp.Domain.Entities.Accounting;

namespace RTSErp.Domain.Entities.Operational;

public class Product : BaseEntity
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Unit { get; set; } = "Piece";   // e.g. Piece, Kg, Box
    public string? Barcode { get; set; }

    // Pricing
    public decimal PurchasePrice { get; set; }
    public decimal SalesPrice { get; set; }
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    // Tax
    public Guid? TaxRateId { get; set; }
    public TaxRate? TaxRate { get; set; }

    // GL Accounts
    public Guid? InventoryAccountId { get; set; }
    public Account? InventoryAccount { get; set; }

    public Guid? COGSAccountId { get; set; }
    public Account? COGSAccount { get; set; }

    public Guid? SalesAccountId { get; set; }
    public Account? SalesAccount { get; set; }

    public Guid? PurchaseAccountId { get; set; }
    public Account? PurchaseAccount { get; set; }

    // Inventory control
    public decimal MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<InventoryBalance> InventoryBalances { get; set; } = [];
    public ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = [];
    public ICollection<SalesOrderLine> SalesOrderLines { get; set; } = [];
}
