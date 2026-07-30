using Microsoft.AspNetCore.Identity;

namespace RTSErp.Domain.Entities.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
