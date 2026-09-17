using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Helpdesk;

public class Ticket : BaseEntity
{
    public string   TicketNumber  { get; set; } = string.Empty;
    public string   Title         { get; set; } = string.Empty;
    public string?  Description   { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketStatus  Status   { get; set; } = TicketStatus.Open;
    public TicketCategory Category { get; set; } = TicketCategory.General;

    // Reporter
    public Guid?   ReportedById   { get; set; }
    public string  ReportedByName  { get; set; } = string.Empty;
    public string  ReportedByEmail { get; set; } = string.Empty;

    // Assignee
    public Guid?   AssignedToId   { get; set; }
    public string? AssignedToName { get; set; }

    // Linked entity (optional)
    public Guid?   RelatedCustomerId { get; set; }
    public string? RelatedEntityRef  { get; set; }  // e.g. "SO-2025-001"

    public string?   Resolution  { get; set; }
    public DateTime? ResolvedAt  { get; set; }
    public DateTime? ClosedAt    { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = [];
}

public class TicketComment : BaseEntity
{
    public Guid   TicketId  { get; set; }
    public Ticket Ticket    { get; set; } = null!;
    public string Body      { get; set; } = string.Empty;
    public Guid?  AuthorId  { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public bool   IsInternal { get; set; }  // internal note vs customer-visible
}

public enum TicketPriority  { Low = 1, Medium = 2, High = 3, Critical = 4 }
public enum TicketStatus    { Open = 1, InProgress = 2, Pending = 3, Resolved = 4, Closed = 5 }
public enum TicketCategory  { General = 1, Technical = 2, Billing = 3, Delivery = 4, Maintenance = 5, Other = 6 }
