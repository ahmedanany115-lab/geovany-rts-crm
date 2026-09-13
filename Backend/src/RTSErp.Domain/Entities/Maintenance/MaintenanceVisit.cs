using RTSErp.Domain.Common;
using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Domain.Entities.Maintenance;

/// <summary>
/// A single technician visit under a ContractQuarter.
/// Completing a visit decrements ContractQuarter.UsedVisits and,
/// if extra charges apply, posts a JE for billable labour / spares.
/// </summary>
public class MaintenanceVisit : BaseEntity
{
    public Guid QuarterId  { get; set; }
    public ContractQuarter Quarter { get; set; } = null!;

    public DateOnly ScheduledDate  { get; set; }
    public DateOnly? ActualDate    { get; set; }

    // Technician (optional FK to ApplicationUser)
    public Guid?   TechnicianId    { get; set; }
    public string? TechnicianName  { get; set; }

    public VisitStatus Status { get; set; } = VisitStatus.Scheduled;

    // Findings & work done
    public string? WorkDescription { get; set; }
    public string? CustomerFeedback { get; set; }

    // Extra billable charges (spares, extra labour, travel)
    public decimal ExtraChargesAmount  { get; set; }
    public string? ExtraChargesNotes   { get; set; }
    public Guid?   ExtraChargesJournalEntryId { get; set; }

    public string? Notes { get; set; }
}

public enum VisitStatus
{
    Scheduled  = 1,
    InProgress = 2,
    Completed  = 3,
    Cancelled  = 4,
}
