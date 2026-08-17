using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Entities.Operational;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Operational.PurchaseOrders;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class PurchaseOrderLineDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal PendingQuantity => Quantity - ReceivedQuantity;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal NetAmount { get; set; }
}

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PurchaseOrderLineDto> Lines { get; set; } = [];
}

// ── Queries ───────────────────────────────────────────────────────────────────

public class GetPurchaseOrdersQuery : IRequest<List<PurchaseOrderDto>>
{
    public PurchaseOrderStatus? Status { get; set; }
    public Guid? SupplierId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, List<PurchaseOrderDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPurchaseOrdersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken ct)
    {
        var query = _db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Currency)
            .Include(o => o.Warehouse)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Where(o => !o.IsDeleted);

        if (request.Status.HasValue) query = query.Where(o => o.Status == request.Status.Value);
        if (request.SupplierId.HasValue) query = query.Where(o => o.SupplierId == request.SupplierId.Value);
        if (request.FromDate.HasValue) query = query.Where(o => o.OrderDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(o => o.OrderDate <= request.ToDate.Value);

        var orders = await query.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.CreatedAt).ToListAsync(ct);
        return orders.Select(PurchaseOrderMapper.MapToDto).ToList();
    }
}

public class GetPurchaseOrderQuery : IRequest<PurchaseOrderDto>
{
    public Guid Id { get; set; }
}

public class GetPurchaseOrderQueryHandler : IRequestHandler<GetPurchaseOrderQuery, PurchaseOrderDto>
{
    private readonly IApplicationDbContext _db;
    public GetPurchaseOrderQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PurchaseOrderDto> Handle(GetPurchaseOrderQuery request, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders
            .Include(o => o.Supplier).Include(o => o.Currency).Include(o => o.Warehouse)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == request.Id && !o.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrder), request.Id);
        return PurchaseOrderMapper.MapToDto(order);
    }
}

internal static class PurchaseOrderMapper
{
    internal static PurchaseOrderDto MapToDto(PurchaseOrder o) => new()
    {
        Id = o.Id, PONumber = o.PONumber, SupplierId = o.SupplierId,
        SupplierName = o.Supplier.Name, OrderDate = o.OrderDate,
        CurrencyCode = o.Currency.Code, ExchangeRate = o.ExchangeRate,
        WarehouseName = o.Warehouse.Name, Status = o.Status,
        SubTotal = o.SubTotal, TaxAmount = o.TaxAmount, TotalAmount = o.TotalAmount,
        Notes = o.Notes, CreatedAt = o.CreatedAt,
        Lines = o.Lines.Where(l => !l.IsDeleted).Select(l => new PurchaseOrderLineDto
        {
            Id = l.Id, ProductId = l.ProductId, ProductName = l.Product.Name, ProductSKU = l.Product.SKU,
            Quantity = l.Quantity, ReceivedQuantity = l.ReceivedQuantity, UnitPrice = l.UnitPrice,
            DiscountPercent = l.DiscountPercent, TaxRate = l.TaxRate, TaxAmount = l.TaxAmount,
            LineTotal = l.LineTotal, NetAmount = l.NetAmount
        }).ToList()
    };
}

// ── Commands ──────────────────────────────────────────────────────────────────

public class CreatePurchaseOrderLineRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal? TaxRateOverride { get; set; }  // if null, use product's tax rate
}

public class CreatePurchaseOrderCommand : IRequest<Guid>
{
    public Guid SupplierId { get; set; }
    public DateOnly OrderDate { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public Guid WarehouseId { get; set; }
    public string? Notes { get; set; }
    public List<CreatePurchaseOrderLineRequest> Lines { get; set; } = [];
}

public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Purchase order must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.DiscountPercent).InclusiveBetween(0, 100);
        });
    }
}

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public CreatePurchaseOrderCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<Guid> Handle(CreatePurchaseOrderCommand request, CancellationToken ct)
    {
        // Generate PO number
        var year = DateTime.UtcNow.Year;
        var prefix = $"PO{year}-";
        var last = await _db.PurchaseOrders.IgnoreQueryFilters()
            .Where(o => o.PONumber.StartsWith(prefix))
            .OrderByDescending(o => o.PONumber)
            .Select(o => o.PONumber)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;
        var poNumber = $"{prefix}{seq:D5}";

        // Load products with tax rates for calculation
        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Include(p => p.TaxRate)
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync(ct);

        var order = new PurchaseOrder
        {
            PONumber = poNumber,
            SupplierId = request.SupplierId,
            OrderDate = request.OrderDate,
            CurrencyId = request.CurrencyId,
            ExchangeRate = request.ExchangeRate,
            WarehouseId = request.WarehouseId,
            Notes = request.Notes?.Trim(),
            Status = PurchaseOrderStatus.Draft,
            CreatedBy = _user.UserId
        };

        decimal subTotal = 0, taxTotal = 0;
        int sort = 0;

        foreach (var lineReq in request.Lines)
        {
            var product = products.First(p => p.Id == lineReq.ProductId);
            var taxRate = request.Lines.First(l => l.ProductId == lineReq.ProductId).TaxRateOverride
                ?? product.TaxRate?.Rate ?? 0;

            // Tax base = price × qty (BEFORE discount, per spec)
            var lineTotal = lineReq.UnitPrice * lineReq.Quantity;
            var taxAmount = lineTotal * taxRate;         // VAT on gross amount
            var discountAmount = lineTotal * (lineReq.DiscountPercent / 100m);
            var netAmount = lineTotal - discountAmount + taxAmount;

            order.Lines.Add(new PurchaseOrderLine
            {
                ProductId = lineReq.ProductId,
                Quantity = lineReq.Quantity,
                UnitPrice = lineReq.UnitPrice,
                DiscountPercent = lineReq.DiscountPercent,
                DiscountAmount = discountAmount,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                LineTotal = lineTotal,
                NetAmount = netAmount,
                SortOrder = ++sort,
                CreatedBy = _user.UserId
            });

            subTotal += lineTotal;
            taxTotal += taxAmount;
        }

        order.SubTotal = subTotal;
        order.TaxAmount = taxTotal;
        order.TotalAmount = subTotal - order.Lines.Sum(l => l.DiscountAmount) + taxTotal;

        _db.PurchaseOrders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order.Id;
    }
}

public class ApprovePurchaseOrderCommand : IRequest
{
    public Guid Id { get; set; }
}

public class ApprovePurchaseOrderCommandHandler : IRequestHandler<ApprovePurchaseOrderCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public ApprovePurchaseOrderCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task Handle(ApprovePurchaseOrderCommand request, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == request.Id && !o.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrder), request.Id);

        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Only Draft orders can be approved.");

        order.Status = PurchaseOrderStatus.Approved;
        order.ModifiedAt = DateTime.UtcNow;
        order.ModifiedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);
    }
}
