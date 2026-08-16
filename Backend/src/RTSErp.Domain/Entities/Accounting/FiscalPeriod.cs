using RTSErp.Domain.Common;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Accounting;

public class FiscalPeriod : BaseEntity
{
    public string Name { get; set; } = string.Empty;           // e.g. "FY2026-Q1"
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public FiscalPeriodStatus Status { get; set; } = FiscalPeriodStatus.Open;

    public ICollection<JournalEntry> JournalEntries { get; set; } = [];
}
