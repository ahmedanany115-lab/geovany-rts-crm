using RTSErp.Domain.Common;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Operational;

/// <summary>
/// Current stock level per product per warehouse.
/// Updated atomically with every InventoryMovement.
/// </summary>
public class InventoryBalance : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AverageCost { get; set; }

    public decimal AvailableQuantity => Quantity - ReservedQuantity;
}

/// <summary>
/// Immutable ledger of every stock movement. Never deleted, only reversed.
/// </summary>
public class InventoryMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public InventoryMovementType MovementType { get; set; }

    /// <summary>Positive = in, negative = out.</summary>
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public DateOnly MovementDate { get; set; }
    public string? Notes { get; set; }

    // Reference to source document
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
}
