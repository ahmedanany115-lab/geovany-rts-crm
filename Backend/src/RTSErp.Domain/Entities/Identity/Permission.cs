using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Identity;

public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;      // e.g. "crm.customers.write"
    public string Description { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;     // e.g. "CRM"

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public ApplicationRole Role { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
