using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Accounting;

public class Currency : BaseEntity
{
    public string Code { get; set; } = string.Empty;         // e.g. EGP, USD
    public string Name { get; set; } = string.Empty;         // e.g. Egyptian Pound
    public string Symbol { get; set; } = string.Empty;       // e.g. ج.م
    public decimal ExchangeRate { get; set; } = 1m;          // Rate relative to base currency (EGP)
    public bool IsBaseCurrency { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Account> Accounts { get; set; } = [];
    public ICollection<JournalEntry> JournalEntries { get; set; } = [];
    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = [];
}
