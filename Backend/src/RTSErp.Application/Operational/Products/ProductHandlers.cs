using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Operational;

namespace RTSErp.Application.Operational.Products;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class ProductDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalesPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? TaxRateName { get; set; }
    public decimal TaxRatePercent { get; set; }
    public bool IsActive { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal TotalQuantity { get; set; }    // sum across warehouses
    public DateTime CreatedAt { get; set; }
}

public class ProductStockDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal AverageCost { get; set; }
}

// ── Queries ───────────────────────────────────────────────────────────────────

public class GetProductsQuery : IRequest<List<ProductDto>>
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly IApplicationDbContext _db;
    public GetProductsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var query = _db.Products
            .Include(p => p.Currency)
            .Include(p => p.TaxRate)
            .Include(p => p.InventoryBalances)
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.SKU.ToLower().Contains(s)
                || (p.Barcode != null && p.Barcode.Contains(s)));
        }
        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(p => p.Category == request.Category);
        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        return await query.OrderBy(p => p.Name)
            .Select(p => new ProductDto
            {
                Id = p.Id, SKU = p.SKU, Name = p.Name, Description = p.Description,
                Category = p.Category, Unit = p.Unit, Barcode = p.Barcode,
                PurchasePrice = p.PurchasePrice, SalesPrice = p.SalesPrice,
                CurrencyCode = p.Currency.Code,
                TaxRateName = p.TaxRate != null ? p.TaxRate.Name : null,
                TaxRatePercent = p.TaxRate != null ? p.TaxRate.Rate * 100 : 0,
                IsActive = p.IsActive, MinimumStock = p.MinimumStock,
                TotalQuantity = p.InventoryBalances.Sum(b => b.Quantity),
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);
    }
}

public class GetProductQuery : IRequest<ProductDto>
{
    public Guid Id { get; set; }
}

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductDto>
{
    private readonly IApplicationDbContext _db;
    public GetProductQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken ct)
    {
        return await _db.Products
            .Include(p => p.Currency)
            .Include(p => p.TaxRate)
            .Include(p => p.InventoryBalances)
            .Where(p => p.Id == request.Id && !p.IsDeleted)
            .Select(p => new ProductDto
            {
                Id = p.Id, SKU = p.SKU, Name = p.Name, Description = p.Description,
                Category = p.Category, Unit = p.Unit, Barcode = p.Barcode,
                PurchasePrice = p.PurchasePrice, SalesPrice = p.SalesPrice,
                CurrencyCode = p.Currency.Code,
                TaxRateName = p.TaxRate != null ? p.TaxRate.Name : null,
                TaxRatePercent = p.TaxRate != null ? p.TaxRate.Rate * 100 : 0,
                IsActive = p.IsActive, MinimumStock = p.MinimumStock,
                TotalQuantity = p.InventoryBalances.Sum(b => b.Quantity),
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Product), request.Id);
    }
}

public class GetProductStockQuery : IRequest<List<ProductStockDto>>
{
    public Guid? ProductId { get; set; }
    public Guid? WarehouseId { get; set; }
}

public class GetProductStockQueryHandler : IRequestHandler<GetProductStockQuery, List<ProductStockDto>>
{
    private readonly IApplicationDbContext _db;
    public GetProductStockQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ProductStockDto>> Handle(GetProductStockQuery request, CancellationToken ct)
    {
        var query = _db.InventoryBalances
            .Include(b => b.Product)
            .Include(b => b.Warehouse)
            .Where(b => !b.IsDeleted);

        if (request.ProductId.HasValue)
            query = query.Where(b => b.ProductId == request.ProductId.Value);
        if (request.WarehouseId.HasValue)
            query = query.Where(b => b.WarehouseId == request.WarehouseId.Value);

        return await query
            .Select(b => new ProductStockDto
            {
                ProductId = b.ProductId, ProductName = b.Product.Name,
                WarehouseId = b.WarehouseId, WarehouseName = b.Warehouse.Name,
                Quantity = b.Quantity, ReservedQuantity = b.ReservedQuantity,
                AvailableQuantity = b.Quantity - b.ReservedQuantity,
                AverageCost = b.AverageCost
            })
            .ToListAsync(ct);
    }
}

// ── Commands ──────────────────────────────────────────────────────────────────

public class UpsertProductCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Unit { get; set; } = "Piece";
    public string? Barcode { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalesPrice { get; set; }
    public Guid CurrencyId { get; set; }
    public Guid? TaxRateId { get; set; }
    public decimal MinimumStock { get; set; }
    public Guid? InventoryAccountId { get; set; }
    public Guid? COGSAccountId { get; set; }
    public Guid? SalesAccountId { get; set; }
    public Guid? PurchaseAccountId { get; set; }
}

public class UpsertProductCommandValidator : AbstractValidator<UpsertProductCommand>
{
    public UpsertProductCommandValidator()
    {
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalesPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CurrencyId).NotEmpty();
    }
}

public class UpsertProductCommandHandler : IRequestHandler<UpsertProductCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public UpsertProductCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<Guid> Handle(UpsertProductCommand request, CancellationToken ct)
    {
        Product? product = null;

        if (request.Id.HasValue)
            product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.Id.Value && !p.IsDeleted, ct);

        if (product is null)
        {
            var exists = await _db.Products.AnyAsync(p => p.SKU == request.SKU && !p.IsDeleted, ct);
            if (exists) throw new InvalidOperationException($"Product with SKU '{request.SKU}' already exists.");
            product = new Product { SKU = request.SKU, IsActive = true, CreatedBy = _user.UserId };
            _db.Products.Add(product);
        }
        else
        {
            product.ModifiedAt = DateTime.UtcNow;
            product.ModifiedBy = _user.UserId;
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Category = request.Category?.Trim();
        product.Unit = request.Unit.Trim();
        product.Barcode = request.Barcode?.Trim();
        product.PurchasePrice = request.PurchasePrice;
        product.SalesPrice = request.SalesPrice;
        product.CurrencyId = request.CurrencyId;
        product.TaxRateId = request.TaxRateId;
        product.MinimumStock = request.MinimumStock;
        product.InventoryAccountId = request.InventoryAccountId;
        product.COGSAccountId = request.COGSAccountId;
        product.SalesAccountId = request.SalesAccountId;
        product.PurchaseAccountId = request.PurchaseAccountId;

        await _db.SaveChangesAsync(ct);
        return product.Id;
    }
}

public class ToggleProductStatusCommand : IRequest
{
    public Guid Id { get; set; }
}

public class ToggleProductStatusCommandHandler : IRequestHandler<ToggleProductStatusCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public ToggleProductStatusCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task Handle(ToggleProductStatusCommand request, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        product.IsActive = !product.IsActive;
        product.ModifiedAt = DateTime.UtcNow;
        product.ModifiedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);
    }
}
