using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Accounting;

public class JournalEntryLine : BaseEntity
{
    public Guid JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    // Amounts in transaction currency
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    // Foreign currency support
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    // Base currency equivalents (EGP)
    public decimal DebitBase { get; set; }
    public decimal CreditBase { get; set; }

    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
