using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Accounting;

public enum BankAccountType
{
    Cash = 1,
    Bank = 2
}

/// <summary>
/// Physical bank / cash account, linked to Chart of Accounts GL entry.
/// Phase 2: tracks OpeningBalance, CurrentBalance; linked to BankTransactions.
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

    // Opening / current balance (informational — authoritative balance is in GL)
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }

    // The GL account this bank/cash account maps to
    public Guid GlAccountId { get; set; }
    public Account GlAccount { get; set; } = null!;

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
}
