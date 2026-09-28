using RTSErp.Domain.Common;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Finance;

/// <summary>
/// Records cash (or petty cash) received. Debit CashAccount / Credit ContraAccount.
/// </summary>
public class CashReceipt : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;   // CR{year}-{seq:D5}
    public DateOnly ReceiptDate { get; set; }

    /// <summary>Person or company the cash was received from.</summary>
    public string? ReceivedFrom { get; set; }

    public decimal Amount { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>GL account that is debited (cash comes IN to this account).</summary>
    public Guid CashAccountId { get; set; }
    public Account CashAccount { get; set; } = null!;

    /// <summary>GL account that is credited (the source of the receipt: AR, revenue, loan …).</summary>
    public Guid ContraAccountId { get; set; }
    public Account ContraAccount { get; set; } = null!;

    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? PreparedBy { get; set; }
    public string? ApprovedBy { get; set; }

    /// <summary>Set when the receipt is posted. Points to the auto-generated JournalEntry.</summary>
    public Guid? JournalEntryId { get; set; }

    public CashTransactionStatus Status { get; set; } = CashTransactionStatus.Draft;
}
