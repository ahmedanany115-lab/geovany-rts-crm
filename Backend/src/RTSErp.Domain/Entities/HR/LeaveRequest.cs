using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.HR;

public class LeaveRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public string EmployeeEmail { get; set; } = string.Empty;
    public string EmployeeName  { get; set; } = string.Empty;

    public LeaveType Type      { get; set; } = LeaveType.Vacation;
    public DateOnly  StartDate { get; set; }
    public DateOnly  EndDate   { get; set; }
    public int       DaysCount { get; set; }
    public string    Reason    { get; set; } = string.Empty;

    public LeaveStatus Status         { get; set; } = LeaveStatus.Pending;
    public string?     ReviewedByName { get; set; }
    public DateTime?   ReviewedAt     { get; set; }
    public string?     ReviewNote     { get; set; }
}

public enum LeaveType
{
    Vacation  = 1,
    SickLeave = 2,
    Permission = 3,
    Emergency  = 4,
}

public enum LeaveStatus
{
    Pending  = 1,
    Approved = 2,
    Rejected = 3,
}
