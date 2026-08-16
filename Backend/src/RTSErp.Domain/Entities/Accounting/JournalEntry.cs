using RTSErp.Domain.Common;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Accounting;

public class JournalEntry : BaseEntity
{
    public string EntryNumber { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public ReferenceType ReferenceType { get; set; } = ReferenceType.Manual;
    public Guid? ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }

    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;

    // Currency
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    // Fiscal period
    public Guid FiscalPeriodId { get; set; }
    public FiscalPeriod FiscalPeriod { get; set; } = null!;

    // Reversal tracking
    public Guid? ReversedByEntryId { get; set; }
    public JournalEntry? ReversedByEntry { get; set; }
    public Guid? ReversesEntryId { get; set; }
    public JournalEntry? ReversesEntry { get; set; }

    public ICollection<JournalEntryLine> Lines { get; set; } = [];
}
