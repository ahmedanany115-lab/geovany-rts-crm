using RTSErp.Domain.Common;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Finance;

/// <summary>
/// Records a cash (or petty cash) payment out.
/// Debit ExpenseAccount / Credit CashAccount.
/// </summary>
public class CashPayment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty;   // CPMT{year}-{seq:D5}
    public DateOnly PaymentDate { get; set; }

    /// <summary>Person or company the cash was paid to.</summary>
    public string? PaidTo { get; set; }

    public decimal Amount { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>GL account that is credited (cash goes OUT of this account).</summary>
    public Guid CashAccountId { get; set; }
    public Account CashAccount { get; set; } = null!;

    /// <summary>GL account that is debited (expense, asset, AP …).</summary>
    public Guid ExpenseAccountId { get; set; }
    public Account ExpenseAccount { get; set; } = null!;

    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? PreparedBy { get; set; }
    public string? ApprovedBy { get; set; }

    /// <summary>Set when the payment is posted. Points to the auto-generated JournalEntry.</summary>
    public Guid? JournalEntryId { get; set; }

    public CashTransactionStatus Status { get; set; } = CashTransactionStatus.Draft;
}
