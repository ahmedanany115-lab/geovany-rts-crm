using RTSErp.Domain.Common;
using RTSErp.Domain.Entities.Accounting;

namespace RTSErp.Domain.Entities.Maintenance;

/// <summary>
/// One quarter of a MaintenanceContract.  
/// Tracks how many included visits remain and auto-generates visit slots.
/// Financial: recognises revenue for that quarter by Dr DeferredRevenue / Cr Revenue.
/// </summary>
public class ContractQuarter : BaseEntity
{
    public Guid ContractId { get; set; }
    public MaintenanceContract Contract { get; set; } = null!;

    public int      QuarterNumber   { get; set; }    // 1–4
    public DateOnly StartDate       { get; set; }
    public DateOnly EndDate         { get; set; }
    public int      AllocatedVisits { get; set; }    // copied from contract on creation
    public int      UsedVisits      { get; set; }    // incremented on visit completion
    public int      RemainingVisits => AllocatedVisits - UsedVisits;

    public QuarterStatus Status { get; set; } = QuarterStatus.Upcoming;

    // Revenue recognition JE posted when quarter becomes Active
    public Guid? RevenueJournalEntryId { get; set; }
    public decimal QuarterValue { get; set; }        // ContractValue / 4

    public ICollection<MaintenanceVisit> Visits { get; set; } = [];
}

public enum QuarterStatus
{
    Upcoming  = 1,
    Active    = 2,
    Completed = 3,
    Skipped   = 4,
}
