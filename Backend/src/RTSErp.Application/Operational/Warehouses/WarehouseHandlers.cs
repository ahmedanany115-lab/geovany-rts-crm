using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Operational;

namespace RTSErp.Application.Operational.Warehouses;

public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class GetWarehousesQuery : IRequest<List<WarehouseDto>>
{
    public bool? IsActive { get; set; }
}

public class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery, List<WarehouseDto>>
{
    private readonly IApplicationDbContext _db;
    public GetWarehousesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken ct)
    {
        var query = _db.Warehouses
            .Include(w => w.InventoryBalances)
            .Where(w => !w.IsDeleted);

        if (request.IsActive.HasValue)
            query = query.Where(w => w.IsActive == request.IsActive.Value);

        return await query.OrderBy(w => w.Name)
            .Select(w => new WarehouseDto
            {
                Id = w.Id, Code = w.Code, Name = w.Name,
                Location = w.Location, Notes = w.Notes, IsActive = w.IsActive,
                ProductCount = w.InventoryBalances.Count(b => b.Quantity > 0)
            })
            .ToListAsync(ct);
    }
}

public class UpsertWarehouseCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class UpsertWarehouseCommandValidator : AbstractValidator<UpsertWarehouseCommand>
{
    public UpsertWarehouseCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class UpsertWarehouseCommandHandler : IRequestHandler<UpsertWarehouseCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public UpsertWarehouseCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<Guid> Handle(UpsertWarehouseCommand request, CancellationToken ct)
    {
        Warehouse? wh = null;
        if (request.Id.HasValue)
            wh = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == request.Id.Value && !w.IsDeleted, ct);

        if (wh is null)
        {
            var exists = await _db.Warehouses.AnyAsync(w => w.Code == request.Code && !w.IsDeleted, ct);
            if (exists) throw new InvalidOperationException($"Warehouse with code '{request.Code}' already exists.");
            wh = new Warehouse { Code = request.Code, IsActive = true, CreatedBy = _user.UserId };
            _db.Warehouses.Add(wh);
        }
        else
        {
            wh.ModifiedAt = DateTime.UtcNow;
            wh.ModifiedBy = _user.UserId;
        }

        wh.Name = request.Name.Trim();
        wh.Location = request.Location?.Trim();
        wh.Notes = request.Notes?.Trim();
        await _db.SaveChangesAsync(ct);
        return wh.Id;
    }
}

public class ToggleWarehouseStatusCommand : IRequest
{
    public Guid Id { get; set; }
}

public class ToggleWarehouseStatusCommandHandler : IRequestHandler<ToggleWarehouseStatusCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public ToggleWarehouseStatusCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task Handle(ToggleWarehouseStatusCommand request, CancellationToken ct)
    {
        var wh = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == request.Id && !w.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Warehouse), request.Id);
        wh.IsActive = !wh.IsActive;
        wh.ModifiedAt = DateTime.UtcNow;
        wh.ModifiedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);
    }
}
