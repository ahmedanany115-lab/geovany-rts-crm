using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Identity;

public class Employee : BaseEntity
{
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }

    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
}
