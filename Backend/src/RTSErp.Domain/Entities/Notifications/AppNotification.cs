using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Notifications;

public class AppNotification : BaseEntity
{
    public Guid     UserId       { get; set; }     // recipient
    public string   Title        { get; set; } = string.Empty;
    public string   Body         { get; set; } = string.Empty;
    public string   Type         { get; set; } = "info";  // info/success/warning/error
    public string?  RelatedRoute { get; set; }    // e.g. "/hr/leaves"
    public Guid?    RelatedId    { get; set; }
    public bool     IsRead       { get; set; }
    public DateTime? ReadAt      { get; set; }
}
