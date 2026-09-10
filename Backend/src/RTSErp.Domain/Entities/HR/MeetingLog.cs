using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.HR;

public class MeetingLog : BaseEntity
{
    public Guid   EmployeeId    { get; set; }
    public string EmployeeEmail { get; set; } = string.Empty;
    public string EmployeeName  { get; set; } = string.Empty;

    public string    Title       { get; set; } = string.Empty;
    public string?   Description { get; set; }
    public MeetingType Type      { get; set; } = MeetingType.InternalMeeting;
    public DateTime  StartTime   { get; set; }
    public DateTime  EndTime     { get; set; }
    public string?   Location    { get; set; }
    public string?   Attendees   { get; set; }   // comma-separated names
    public string?   Outcome     { get; set; }
}

public enum MeetingType
{
    InternalMeeting = 1,
    ClientVisit     = 2,
    SiteVisit       = 3,
    Training        = 4,
    Other           = 5,
}
