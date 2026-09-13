using RTSErp.Domain.Common;
using RTSErp.Domain.Entities.Accounting;

namespace RTSErp.Domain.Entities.Maintenance;

/// <summary>
/// Annual service contract with a customer covering N visits per quarter.
/// Financial: on creation a deferred revenue JE is posted; each quarter
/// is recognised on the quarter start date.
/// </summary>
public class MaintenanceContract : BaseEntity
{
    public string ContractNumber { get; set; } = string.Empty;

    // ── Parties ───────────────────────────────────────────────────────────────
    public Guid CustomerId { get; set; }                 // FK → BusinessPartner (Customer)
    public BusinessPartner Customer { get; set; } = null!;

    // ── Coverage ──────────────────────────────────────────────────────────────
    public DateOnly StartDate  { get; set; }
    public DateOnly EndDate    { get; set; }
    public int      TotalVisitsPerQuarter { get; set; } = 1;   // included visits each quarter

    // ── Financial ─────────────────────────────────────────────────────────────
    public Guid    CurrencyId    { get; set; }
    public Currency Currency     { get; set; } = null!;
    public decimal ContractValue { get; set; }                  // total annual contract value
    public Guid?   RevenueAccountId   { get; set; }             // credit: maintenance revenue
    public Account? RevenueAccount    { get; set; }
    public Guid?   ReceivableAccountId { get; set; }            // debit: customer AR
    public Account? ReceivableAccount  { get; set; }
    public Guid?   DeferredRevenueAccountId { get; set; }       // liability: unearned revenue
    public Account? DeferredRevenueAccount  { get; set; }
    public Guid?   InitialJournalEntryId { get; set; }          // JE posted on contract creation

    // ── Status ────────────────────────────────────────────────────────────────
    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    public string? Notes { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public ICollection<ContractQuarter>   Quarters   { get; set; } = [];
    public ICollection<ContractEquipment> Equipment  { get; set; } = [];
}

public enum ContractStatus
{
    Draft     = 1,
    Active    = 2,
    Suspended = 3,
    Completed = 4,
    Cancelled = 5,
}
