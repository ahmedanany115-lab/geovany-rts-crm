using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Accounting;

public enum BusinessPartnerType
{
    Customer = 1,
    Supplier = 2,
    Both = 3
}

/// <summary>
/// Accounting foundation for customers and suppliers.
/// The full CRM customer / purchasing supplier modules will reference this entity
/// to get AR/AP account balances without duplicating accounting logic.
/// </summary>
public class BusinessPartner : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public BusinessPartnerType PartnerType { get; set; }
    public bool IsActive { get; set; } = true;

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
