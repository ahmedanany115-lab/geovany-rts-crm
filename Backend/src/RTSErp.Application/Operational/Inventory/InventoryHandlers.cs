using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Operational;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Operational.Inventory;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class InventoryMovementDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public InventoryMovementType MovementType { get; set; }
    public string MovementTypeName => MovementType.ToString();
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public DateOnly MovementDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

// ── Queries ───────────────────────────────────────────────────────────────────

public class GetInventoryMovementsQuery : IRequest<List<InventoryMovementDto>>
{
    public Guid? ProductId { get; set; }
    public Guid? WarehouseId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public InventoryMovementType? MovementType { get; set; }
}

public class GetInventoryMovementsQueryHandler : IRequestHandler<GetInventoryMovementsQuery, List<InventoryMovementDto>>
{
    private readonly IApplicationDbContext _db;
    public GetInventoryMovementsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<InventoryMovementDto>> Handle(GetInventoryMovementsQuery request, CancellationToken ct)
    {
        var query = _db.InventoryMovements
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Where(m => !m.IsDeleted);

        if (request.ProductId.HasValue)   query = query.Where(m => m.ProductId   == request.ProductId.Value);
        if (request.WarehouseId.HasValue) query = query.Where(m => m.WarehouseId == request.WarehouseId.Value);
        if (request.FromDate.HasValue)    query = query.Where(m => m.MovementDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)      query = query.Where(m => m.MovementDate <= request.ToDate.Value);
        if (request.MovementType.HasValue) query = query.Where(m => m.MovementType == request.MovementType.Value);

        return await query.OrderByDescending(m => m.MovementDate).ThenByDescending(m => m.CreatedAt)
            .Select(m => new InventoryMovementDto
            {
                Id = m.Id, ProductName = m.Product.Name, WarehouseName = m.Warehouse.Name,
                MovementType = m.MovementType, Quantity = m.Quantity,
                UnitCost = m.UnitCost, TotalCost = m.TotalCost,
                MovementDate = m.MovementDate, ReferenceNumber = m.ReferenceNumber, Notes = m.Notes
            })
            .ToListAsync(ct);
    }
}

// ── Commands ──────────────────────────────────────────────────────────────────

/// <summary>
/// Adjusts inventory up or down. Creates a movement record and updates balance.
/// </summary>
public class AdjustInventoryCommand : IRequest<Guid>
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }  // positive = in, negative = out
    public decimal UnitCost { get; set; }
    public DateOnly MovementDate { get; set; }
    public string? Notes { get; set; }
}

public class AdjustInventoryCommandValidator : AbstractValidator<AdjustInventoryCommand>
{
    public AdjustInventoryCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Quantity).NotEqual(0).WithMessage("Adjustment quantity cannot be zero.");
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
    }
}

public class AdjustInventoryCommandHandler : IRequestHandler<AdjustInventoryCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IInventoryService _inventoryService;
    public AdjustInventoryCommandHandler(IApplicationDbContext db, ICurrentUserService user, IInventoryService inventoryService)
        => (_db, _user, _inventoryService) = (db, user, inventoryService);

    public async Task<Guid> Handle(AdjustInventoryCommand request, CancellationToken ct)
    {
        // Validate product and warehouse exist
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId && !w.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

        var movType = request.Quantity > 0 ? InventoryMovementType.AdjustmentIn : InventoryMovementType.AdjustmentOut;

        await _inventoryService.MoveInventoryAsync(
            request.ProductId, request.WarehouseId,
            request.Quantity, request.UnitCost,
            movType, request.MovementDate,
            notes: request.Notes,
            createdBy: _user.UserId,
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);
        return Guid.NewGuid(); // movement ID not easily returned here; caller can query movements
    }
}

/// <summary>
/// Warehouse transfer — moves stock from one warehouse to another atomically.
/// </summary>
public class TransferInventoryCommand : IRequest
{
    public Guid ProductId { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public DateOnly TransferDate { get; set; }
    public string? Notes { get; set; }
}

public class TransferInventoryCommandValidator : AbstractValidator<TransferInventoryCommand>
{
    public TransferInventoryCommandValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.FromWarehouseId).NotEmpty();
        RuleFor(x => x.ToWarehouseId).NotEmpty()
            .NotEqual(x => x.FromWarehouseId).WithMessage("Source and destination warehouses must differ.");
    }
}

public class TransferInventoryCommandHandler : IRequestHandler<TransferInventoryCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IInventoryService _inventory;
    public TransferInventoryCommandHandler(IApplicationDbContext db, ICurrentUserService user, IInventoryService inventory)
        => (_db, _user, _inventory) = (db, user, inventory);

    public async Task Handle(TransferInventoryCommand request, CancellationToken ct)
    {
        var (available, unitCost) = await _inventory.GetBalanceAsync(request.ProductId, request.FromWarehouseId, ct);

        if (available < request.Quantity)
            throw new InvalidOperationException(
                $"Insufficient stock in source warehouse. Available: {available:N2}, Requested: {request.Quantity:N2}");

        var refNum = $"TRF-{request.TransferDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

        await _inventory.MoveInventoryAsync(
            request.ProductId, request.FromWarehouseId, -request.Quantity, unitCost,
            InventoryMovementType.TransferOut, request.TransferDate,
            referenceType: "Transfer", referenceNumber: refNum,
            notes: request.Notes, createdBy: _user.UserId, cancellationToken: ct);

        await _inventory.MoveInventoryAsync(
            request.ProductId, request.ToWarehouseId, request.Quantity, unitCost,
            InventoryMovementType.TransferIn, request.TransferDate,
            referenceType: "Transfer", referenceNumber: refNum,
            notes: request.Notes, createdBy: _user.UserId, cancellationToken: ct);

        await _db.SaveChangesAsync(ct);
    }
}
