using RTSErp.Domain.Common;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Operational;

public class PurchaseOrder : BaseEntity
{
    public string PONumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }
    public BusinessPartner Supplier { get; set; } = null!;

    public DateOnly OrderDate { get; set; }
    public string? Notes { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    // Totals (computed server-side)
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = [];
    public ICollection<PurchaseReceipt> Receipts { get; set; } = [];
}

public class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }   // updated as receipts come in
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }

    // Tax (per-line, VAT 14% default)
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }          // price*qty (no discount, no tax) — used as tax base
    public decimal NetAmount { get; set; }          // after discount + tax

    public int SortOrder { get; set; }
}
