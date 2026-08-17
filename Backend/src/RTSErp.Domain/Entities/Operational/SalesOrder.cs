using RTSErp.Domain.Common;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Operational;

// ── Sales Order ───────────────────────────────────────────────────────────────

public class SalesOrder : BaseEntity
{
    public string SONumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public BusinessPartner Customer { get; set; } = null!;

    public DateOnly OrderDate { get; set; }
    public string? Notes { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    // Salesperson (optional)
    public Guid? SalespersonId { get; set; }
    public ApplicationUser? Salesperson { get; set; }

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<SalesOrderLine> Lines { get; set; } = [];
    public ICollection<SalesDelivery> Deliveries { get; set; } = [];
    public ICollection<SalesCommission> Commissions { get; set; } = [];
}

public class SalesOrderLine : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal DeliveredQuantity { get; set; }  // updated as deliveries come in
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal NetAmount { get; set; }

    public int SortOrder { get; set; }
}

// ── Sales Delivery ────────────────────────────────────────────────────────────

public class SalesDelivery : BaseEntity
{
    public string DeliveryNumber { get; set; } = string.Empty;

    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public BusinessPartner Customer { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public DateOnly DeliveryDate { get; set; }
    public string? Notes { get; set; }

    public decimal TotalCOGS { get; set; }

    // Journal entry for COGS
    public Guid? JournalEntryId { get; set; }

    public ICollection<SalesDeliveryLine> Lines { get; set; } = [];
}

public class SalesDeliveryLine : BaseEntity
{
    public Guid SalesDeliveryId { get; set; }
    public SalesDelivery SalesDelivery { get; set; } = null!;

    public Guid SalesOrderLineId { get; set; }
    public SalesOrderLine SalesOrderLine { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }   // average cost at time of delivery
    public decimal TotalCost { get; set; }
}

// ── Customer Invoice ──────────────────────────────────────────────────────────

public class CustomerInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public BusinessPartner Customer { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public Guid? SalespersonId { get; set; }
    public ApplicationUser? Salesperson { get; set; }

    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public InvoiceType InvoiceType { get; set; } = InvoiceType.Standard;

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue => TotalAmount - PaidAmount;

    // Accounting
    public Guid? JournalEntryId { get; set; }

    // E-Invoice (Egyptian ETA — future integration)
    public string? TaxRegistrationNumber { get; set; }
    public string? EInvoiceUUID { get; set; }
    public EInvoiceSubmissionStatus EInvoiceStatus { get; set; } = EInvoiceSubmissionStatus.NotSubmitted;
    public DateTime? EInvoiceSubmissionDate { get; set; }
    public string? ExternalInvoiceId { get; set; }
    public string? ExternalStatus { get; set; }
    public string? QRCode { get; set; }
    public string? CancellationStatus { get; set; }

    public ICollection<CustomerInvoiceLine> Lines { get; set; } = [];
    public ICollection<CustomerPayment> Payments { get; set; } = [];
    public ICollection<SalesCommission> Commissions { get; set; } = [];
}

public class CustomerInvoiceLine : BaseEntity
{
    public Guid CustomerInvoiceId { get; set; }
    public CustomerInvoice CustomerInvoice { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal NetAmount { get; set; }

    // COGS (filled when delivered)
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public int SortOrder { get; set; }
}
