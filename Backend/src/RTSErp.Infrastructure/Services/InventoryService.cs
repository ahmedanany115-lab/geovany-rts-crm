using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Operational;
using RTSErp.Domain.Enums;
using RTSErp.Infrastructure.Persistence;

namespace RTSErp.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _db;

    public InventoryService(ApplicationDbContext db) => _db = db;

    public async Task MoveInventoryAsync(
        Guid productId, Guid warehouseId,
        decimal quantity, decimal unitCost,
        InventoryMovementType movementType,
        DateOnly movementDate,
        string? referenceType = null, Guid? referenceId = null, string? referenceNumber = null,
        string? notes = null, Guid? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        // Prevent negative stock for out movements
        if (quantity < 0)
        {
            var (available, _) = await GetBalanceAsync(productId, warehouseId, cancellationToken);
            if (available + quantity < 0)
                throw new InvalidOperationException(
                    $"Insufficient stock. Available: {available:N4}, Requested out: {Math.Abs(quantity):N4}");
        }

        _db.InventoryMovements.Add(new InventoryMovement
        {
            ProductId = productId, WarehouseId = warehouseId,
            MovementType = movementType, Quantity = quantity,
            UnitCost = unitCost, TotalCost = Math.Abs(quantity) * unitCost,
            MovementDate = movementDate, Notes = notes,
            ReferenceType = referenceType, ReferenceId = referenceId, ReferenceNumber = referenceNumber,
            CreatedBy = createdBy
        });

        // Update or create balance
        var balance = await _db.InventoryBalances
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.ProductId == productId && b.WarehouseId == warehouseId, cancellationToken);

        if (balance is null)
        {
            balance = new InventoryBalance
            {
                ProductId = productId, WarehouseId = warehouseId,
                Quantity = 0, ReservedQuantity = 0, AverageCost = unitCost > 0 ? unitCost : 0,
                CreatedBy = createdBy
            };
            _db.InventoryBalances.Add(balance);
        }

        // Weighted average cost update (only for incoming)
        if (quantity > 0 && unitCost > 0)
        {
            var totalCost = (balance.Quantity * balance.AverageCost) + (quantity * unitCost);
            var totalQty = balance.Quantity + quantity;
            balance.AverageCost = totalQty > 0 ? totalCost / totalQty : unitCost;
        }

        balance.Quantity += quantity;
        balance.ModifiedAt = DateTime.UtcNow;
        balance.ModifiedBy = createdBy;
    }

    public async Task<(decimal AvailableQty, decimal AverageCost)> GetBalanceAsync(
        Guid productId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var balance = await _db.InventoryBalances
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.ProductId == productId && b.WarehouseId == warehouseId, cancellationToken);
        return (balance?.AvailableQuantity ?? 0, balance?.AverageCost ?? 0);
    }

    public Task<string> GenerateDocumentNumberAsync(string prefix, CancellationToken cancellationToken = default)
        => Task.FromResult($"{prefix}{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}");
}
