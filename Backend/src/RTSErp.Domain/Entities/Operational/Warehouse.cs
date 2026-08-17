using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Operational;

public class Warehouse : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<InventoryBalance> InventoryBalances { get; set; } = [];
    public ICollection<InventoryMovement> Movements { get; set; } = [];
}
