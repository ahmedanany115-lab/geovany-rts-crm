using RTSErp.Domain.Common;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Operational;

public class PurchaseReceipt : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;

    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Guid SupplierId { get; set; }
    public BusinessPartner Supplier { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public DateOnly ReceiptDate { get; set; }
    public string? Notes { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public decimal TotalAmount { get; set; }

    // Journal entry created on posting
    public Guid? JournalEntryId { get; set; }

    public ICollection<PurchaseReceiptLine> Lines { get; set; } = [];
}

public class PurchaseReceiptLine : BaseEntity
{
    public Guid PurchaseReceiptId { get; set; }
    public PurchaseReceipt PurchaseReceipt { get; set; } = null!;

    public Guid PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

// ── Supplier Invoice ──────────────────────────────────────────────────────────

public class SupplierInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? SupplierInvoiceNumber { get; set; }   // supplier's own reference

    public Guid SupplierId { get; set; }
    public BusinessPartner Supplier { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public Guid? PurchaseReceiptId { get; set; }
    public PurchaseReceipt? PurchaseReceipt { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue => TotalAmount - PaidAmount;

    // Accounting
    public Guid? JournalEntryId { get; set; }

    // E-Invoice fields (future)
    public string? EInvoiceUUID { get; set; }
    public EInvoiceSubmissionStatus EInvoiceStatus { get; set; } = EInvoiceSubmissionStatus.NotSubmitted;

    public ICollection<SupplierInvoiceLine> Lines { get; set; } = [];
    public ICollection<SupplierPayment> Payments { get; set; } = [];
}

public class SupplierInvoiceLine : BaseEntity
{
    public Guid SupplierInvoiceId { get; set; }
    public SupplierInvoice SupplierInvoice { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }    // qty*unit (tax base)
    public decimal NetAmount { get; set; }    // after discount + tax

    public int SortOrder { get; set; }
}
