using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Accounting;

public enum BankAccountType
{
    Cash = 1,
    Bank = 2
}

/// <summary>
/// Foundation for cash accounts and bank accounts.
/// Maps each physical bank account to its corresponding Chart of Accounts entry.
/// The full Banking module (Phase 2) will build on this.
/// </summary>
public class BankAccount : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public BankAccountType AccountType { get; set; }

    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IBAN { get; set; }

    public bool IsActive { get; set; } = true;

    // The GL account this bank/cash account maps to
    public Guid GlAccountId { get; set; }
    public Account GlAccount { get; set; } = null!;

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
}
