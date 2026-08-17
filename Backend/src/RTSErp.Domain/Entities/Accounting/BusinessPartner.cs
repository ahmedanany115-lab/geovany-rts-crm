using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Accounting;

public enum BusinessPartnerType
{
    Customer = 1,
    Supplier = 2,
    Both = 3
}

/// <summary>
/// Unified customer/supplier entity.
/// A partner may be Customer, Supplier, or Both.
/// Phase 2 operational modules (Sales, Purchasing, Banking) reference this.
/// </summary>
public class BusinessPartner : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public BusinessPartnerType PartnerType { get; set; }
    public bool IsActive { get; set; } = true;

    // Contact
    public string? TaxNumber { get; set; }       // VAT/Tax registration number
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    // Accounts Receivable account for customers
    public Guid? ReceivableAccountId { get; set; }
    public Account? ReceivableAccount { get; set; }

    // Accounts Payable account for suppliers
    public Guid? PayableAccountId { get; set; }
    public Account? PayableAccount { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    // Credit limit for customers
    public decimal? CreditLimit { get; set; }
}
