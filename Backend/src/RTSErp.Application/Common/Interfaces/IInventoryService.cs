using RTSErp.Domain.Enums;

namespace RTSErp.Application.Common.Interfaces;

/// <summary>
/// Internal service for moving inventory — used by purchase receipts,
/// sales deliveries, and adjustment commands. All methods are transactional
/// (caller is responsible for calling SaveChangesAsync).
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Moves inventory: creates movement record, updates balance.
    /// Throws if quantity would make stock negative (for out movements).
    /// </summary>
    Task MoveInventoryAsync(
        Guid productId,
        Guid warehouseId,
        decimal quantity,          // positive = in, negative = out
        decimal unitCost,
        InventoryMovementType movementType,
        DateOnly movementDate,
        string? referenceType = null,
        Guid? referenceId = null,
        string? referenceNumber = null,
        string? notes = null,
        Guid? createdBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the average cost and available quantity for a product/warehouse.</summary>
    Task<(decimal AvailableQty, decimal AverageCost)> GetBalanceAsync(
        Guid productId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>Generates a sequential document number with given prefix, e.g. PO2026-00001.</summary>
    Task<string> GenerateDocumentNumberAsync(
        string prefix,
        CancellationToken cancellationToken = default);
}
