using RTSErp.Domain.Common;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Sales;

public class WarrantyCertificate : BaseEntity
{
    public string CertificateNumber { get; set; } = string.Empty;
    public DateOnly CertificateDate { get; set; }

    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerContact { get; set; }

    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; } = 1m;

    public Guid? InvoiceId { get; set; }
    public Guid? SalesOrderId { get; set; }

    public DateOnly WarrantyStartDate { get; set; }
    public DateOnly WarrantyEndDate { get; set; }
    public string? WarrantyTerms { get; set; }
    public string? Notes { get; set; }

    public WarrantyCertificateStatus Status { get; set; } = WarrantyCertificateStatus.Active;
}
