using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Operational;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Operational.SalesOrders;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class SalesOrderLineDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public decimal PendingQuantity => Quantity - DeliveredQuantity;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal NetAmount { get; set; }
}

public class SalesOrderDto
{
    public Guid Id { get; set; }
    public string SONumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string? SalespersonName { get; set; }
    public SalesOrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SalesOrderLineDto> Lines { get; set; } = [];
}

public class CustomerInvoiceLineDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal NetAmount { get; set; }
}

public class CustomerInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public string? SalespersonName { get; set; }
    public InvoiceStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CustomerInvoiceLineDto> Lines { get; set; } = [];
}

// ── Sales Order Queries ───────────────────────────────────────────────────────

public class GetSalesOrdersQuery : IRequest<List<SalesOrderDto>>
{
    public SalesOrderStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class GetSalesOrdersQueryHandler : IRequestHandler<GetSalesOrdersQuery, List<SalesOrderDto>>
{
    private readonly IApplicationDbContext _db;
    public GetSalesOrdersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<SalesOrderDto>> Handle(GetSalesOrdersQuery req, CancellationToken ct)
    {
        var q = _db.SalesOrders
            .Include(o => o.Customer).Include(o => o.Currency).Include(o => o.Warehouse)
            .Include(o => o.Salesperson)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Where(o => !o.IsDeleted);
        if (req.Status.HasValue) q = q.Where(o => o.Status == req.Status.Value);
        if (req.CustomerId.HasValue) q = q.Where(o => o.CustomerId == req.CustomerId.Value);
        if (req.FromDate.HasValue) q = q.Where(o => o.OrderDate >= req.FromDate.Value);
        if (req.ToDate.HasValue) q = q.Where(o => o.OrderDate <= req.ToDate.Value);
        var list = await q.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.CreatedAt).ToListAsync(ct);
        return list.Select(SalesOrderMapper.MapToDto).ToList();
    }
}

public class GetSalesOrderQuery : IRequest<SalesOrderDto>
{
    public Guid Id { get; set; }
}

public class GetSalesOrderQueryHandler : IRequestHandler<GetSalesOrderQuery, SalesOrderDto>
{
    private readonly IApplicationDbContext _db;
    public GetSalesOrderQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<SalesOrderDto> Handle(GetSalesOrderQuery req, CancellationToken ct)
    {
        var order = await _db.SalesOrders
            .Include(o => o.Customer).Include(o => o.Currency).Include(o => o.Warehouse)
            .Include(o => o.Salesperson)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == req.Id && !o.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(SalesOrder), req.Id);
        return SalesOrderMapper.MapToDto(order);
    }
}

internal static class SalesOrderMapper
{
    internal static SalesOrderDto MapToDto(SalesOrder o) => new()
    {
        Id = o.Id, SONumber = o.SONumber, CustomerId = o.CustomerId,
        CustomerName = o.Customer.Name, OrderDate = o.OrderDate,
        CurrencyCode = o.Currency.Code, ExchangeRate = o.ExchangeRate,
        WarehouseName = o.Warehouse.Name,
        SalespersonName = o.Salesperson != null ? $"{o.Salesperson.FirstName} {o.Salesperson.LastName}" : null,
        Status = o.Status, SubTotal = o.SubTotal, TaxAmount = o.TaxAmount, TotalAmount = o.TotalAmount,
        Notes = o.Notes, CreatedAt = o.CreatedAt,
        Lines = o.Lines.Where(l => !l.IsDeleted).Select(l => new SalesOrderLineDto
        {
            Id = l.Id, ProductId = l.ProductId, ProductName = l.Product.Name, ProductSKU = l.Product.SKU,
            Quantity = l.Quantity, DeliveredQuantity = l.DeliveredQuantity,
            UnitPrice = l.UnitPrice, DiscountPercent = l.DiscountPercent,
            TaxRate = l.TaxRate, TaxAmount = l.TaxAmount, LineTotal = l.LineTotal, NetAmount = l.NetAmount
        }).ToList()
    };

    internal static CustomerInvoiceDto MapInvoiceToDto(CustomerInvoice i) => new()
    {
        Id = i.Id, InvoiceNumber = i.InvoiceNumber, CustomerId = i.CustomerId,
        CustomerName = i.Customer.Name, InvoiceDate = i.InvoiceDate, DueDate = i.DueDate,
        CurrencyCode = i.Currency.Code, ExchangeRate = i.ExchangeRate,
        SalespersonName = i.Salesperson != null ? $"{i.Salesperson.FirstName} {i.Salesperson.LastName}" : null,
        Status = i.Status, SubTotal = i.SubTotal, DiscountAmount = i.DiscountAmount,
        TaxAmount = i.TaxAmount, TotalAmount = i.TotalAmount, PaidAmount = i.PaidAmount,
        BalanceDue = i.TotalAmount - i.PaidAmount, CreatedAt = i.CreatedAt,
        Lines = i.Lines.Where(l => !l.IsDeleted).Select(l => new CustomerInvoiceLineDto
        {
            Id = l.Id, ProductId = l.ProductId, ProductName = l.Product.Name,
            Description = l.Description, Quantity = l.Quantity, UnitPrice = l.UnitPrice,
            DiscountPercent = l.DiscountPercent, TaxRate = l.TaxRate, TaxAmount = l.TaxAmount,
            LineTotal = l.LineTotal, NetAmount = l.NetAmount
        }).ToList()
    };
}

// ── Create Sales Order ────────────────────────────────────────────────────────

public class CreateSalesOrderLineRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal? TaxRateOverride { get; set; }
}

public class CreateSalesOrderCommand : IRequest<Guid>
{
    public Guid CustomerId { get; set; }
    public DateOnly OrderDate { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public Guid WarehouseId { get; set; }
    public Guid? SalespersonId { get; set; }
    public string? Notes { get; set; }
    public List<CreateSalesOrderLineRequest> Lines { get; set; } = [];
}

public class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    public CreateSalesOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Sales order must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.ProductId).NotEmpty();
            l.RuleFor(x => x.Quantity).GreaterThan(0);
            l.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        });
    }
}

public class CreateSalesOrderCommandHandler : IRequestHandler<CreateSalesOrderCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public CreateSalesOrderCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<Guid> Handle(CreateSalesOrderCommand req, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"SO{year}-";
        var last = await _db.SalesOrders.IgnoreQueryFilters()
            .Where(o => o.SONumber.StartsWith(prefix)).OrderByDescending(o => o.SONumber)
            .Select(o => o.SONumber).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;

        var productIds = req.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _db.Products.Include(p => p.TaxRate)
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted).ToListAsync(ct);

        var order = new SalesOrder
        {
            SONumber = $"{prefix}{seq:D5}",
            CustomerId = req.CustomerId, OrderDate = req.OrderDate,
            CurrencyId = req.CurrencyId, ExchangeRate = req.ExchangeRate,
            WarehouseId = req.WarehouseId, SalespersonId = req.SalespersonId,
            Notes = req.Notes?.Trim(), Status = SalesOrderStatus.Draft,
            CreatedBy = _user.UserId
        };

        decimal subTotal = 0, taxTotal = 0;
        int sort = 0;
        foreach (var lineReq in req.Lines)
        {
            var product = products.First(p => p.Id == lineReq.ProductId);
            var taxRate = lineReq.TaxRateOverride ?? product.TaxRate?.Rate ?? 0;
            // VAT on gross (BEFORE discount) per spec
            var lineTotal = lineReq.UnitPrice * lineReq.Quantity;
            var taxAmount = lineTotal * taxRate;
            var discountAmount = lineTotal * (lineReq.DiscountPercent / 100m);
            var netAmount = lineTotal - discountAmount + taxAmount;

            order.Lines.Add(new SalesOrderLine
            {
                ProductId = lineReq.ProductId, Quantity = lineReq.Quantity,
                UnitPrice = lineReq.UnitPrice, DiscountPercent = lineReq.DiscountPercent,
                DiscountAmount = discountAmount, TaxRate = taxRate,
                TaxAmount = taxAmount, LineTotal = lineTotal, NetAmount = netAmount,
                SortOrder = ++sort, CreatedBy = _user.UserId
            });
            subTotal += lineTotal;
            taxTotal += taxAmount;
        }

        order.SubTotal = subTotal;
        order.TaxAmount = taxTotal;
        order.TotalAmount = subTotal - order.Lines.Sum(l => l.DiscountAmount) + taxTotal;
        _db.SalesOrders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order.Id;
    }
}

public class ApproveSalesOrderCommand : IRequest
{
    public Guid Id { get; set; }
}

public class ApproveSalesOrderCommandHandler : IRequestHandler<ApproveSalesOrderCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public ApproveSalesOrderCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);
    public async Task Handle(ApproveSalesOrderCommand req, CancellationToken ct)
    {
        var order = await _db.SalesOrders.FirstOrDefaultAsync(o => o.Id == req.Id && !o.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(SalesOrder), req.Id);
        if (order.Status != SalesOrderStatus.Draft) throw new InvalidOperationException("Only Draft orders can be approved.");
        order.Status = SalesOrderStatus.Approved;
        order.ModifiedAt = DateTime.UtcNow; order.ModifiedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);
    }
}

// ── Create Sales Delivery ─────────────────────────────────────────────────────

public class DeliverLineRequest
{
    public Guid SalesOrderLineId { get; set; }
    public decimal Quantity { get; set; }
}

public class CreateSalesDeliveryCommand : IRequest<Guid>
{
    public Guid SalesOrderId { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public string? Notes { get; set; }
    public List<DeliverLineRequest> Lines { get; set; } = [];
}

public class CreateSalesDeliveryCommandValidator : AbstractValidator<CreateSalesDeliveryCommand>
{
    public CreateSalesDeliveryCommandValidator()
    {
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l => l.RuleFor(x => x.Quantity).GreaterThan(0));
    }
}

public class CreateSalesDeliveryCommandHandler : IRequestHandler<CreateSalesDeliveryCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IInventoryService _inventory;
    private readonly IAccountingService _accounting;

    public CreateSalesDeliveryCommandHandler(IApplicationDbContext db, ICurrentUserService user,
        IInventoryService inventory, IAccountingService accounting)
        => (_db, _user, _inventory, _accounting) = (db, user, inventory, accounting);

    public async Task<Guid> Handle(CreateSalesDeliveryCommand req, CancellationToken ct)
    {
        var order = await _db.SalesOrders
            .Include(o => o.Lines).ThenInclude(l => l.Product).ThenInclude(p => p.COGSAccount)
            .Include(o => o.Lines).ThenInclude(l => l.Product).ThenInclude(p => p.InventoryAccount)
            .Include(o => o.Currency)
            .FirstOrDefaultAsync(o => o.Id == req.SalesOrderId && !o.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(SalesOrder), req.SalesOrderId);

        if (order.Status == SalesOrderStatus.Draft) throw new InvalidOperationException("Approve the sales order before delivery.");
        if (order.Status == SalesOrderStatus.Cancelled) throw new InvalidOperationException("Cannot deliver from a cancelled order.");

        var year = DateTime.UtcNow.Year;
        var prefix = $"DN{year}-";
        var last = await _db.SalesDeliveries.IgnoreQueryFilters()
            .Where(d => d.DeliveryNumber.StartsWith(prefix)).OrderByDescending(d => d.DeliveryNumber)
            .Select(d => d.DeliveryNumber).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;

        var delivery = new SalesDelivery
        {
            DeliveryNumber = $"{prefix}{seq:D5}",
            SalesOrderId = order.Id, CustomerId = order.CustomerId,
            WarehouseId = order.WarehouseId, DeliveryDate = req.DeliveryDate,
            Notes = req.Notes?.Trim(), CreatedBy = _user.UserId
        };

        decimal totalCOGS = 0;
        var cogsLines = new List<(Guid cogsAccountId, Guid inventoryAccountId, decimal amount, string productName)>();

        foreach (var lineReq in req.Lines)
        {
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == lineReq.SalesOrderLineId && !l.IsDeleted)
                ?? throw new NotFoundException(nameof(SalesOrderLine), lineReq.SalesOrderLineId);

            var pendingQty = orderLine.Quantity - orderLine.DeliveredQuantity;
            if (lineReq.Quantity > pendingQty)
                throw new InvalidOperationException(
                    $"Cannot deliver {lineReq.Quantity} of {orderLine.Product.Name}. Pending: {pendingQty}.");

            var (_, avgCost) = await _inventory.GetBalanceAsync(orderLine.ProductId, order.WarehouseId, ct);

            // Move inventory out
            await _inventory.MoveInventoryAsync(
                orderLine.ProductId, order.WarehouseId,
                -lineReq.Quantity, avgCost,
                InventoryMovementType.SalesIssue, req.DeliveryDate,
                referenceType: "SalesDelivery", referenceId: delivery.Id,
                referenceNumber: delivery.DeliveryNumber,
                notes: $"Delivery for SO {order.SONumber}",
                createdBy: _user.UserId, cancellationToken: ct);

            var lineCOGS = lineReq.Quantity * avgCost;
            delivery.Lines.Add(new SalesDeliveryLine
            {
                SalesOrderLineId = lineReq.SalesOrderLineId,
                ProductId = orderLine.ProductId, Quantity = lineReq.Quantity,
                UnitCost = avgCost, TotalCost = lineCOGS, CreatedBy = _user.UserId
            });

            orderLine.DeliveredQuantity += lineReq.Quantity;
            orderLine.ModifiedAt = DateTime.UtcNow;
            totalCOGS += lineCOGS;

            // Collect for COGS journal
            var cogsAccId = orderLine.Product.COGSAccountId;
            var invAccId = orderLine.Product.InventoryAccountId;
            if (cogsAccId.HasValue && invAccId.HasValue)
                cogsLines.Add((cogsAccId.Value, invAccId.Value, lineCOGS, orderLine.Product.Name));
        }

        delivery.TotalCOGS = totalCOGS;

        // Post COGS journal entry: Dr COGS / Cr Inventory
        if (cogsLines.Any() && totalCOGS > 0)
        {
            var jeLines = new List<JournalEntryLineRequest>();
            int sort = 0;
            foreach (var (cogsId, invId, amount, name) in cogsLines)
            {
                jeLines.Add(new JournalEntryLineRequest { AccountId = cogsId, Debit = amount, Credit = 0, Description = $"COGS: {name}", SortOrder = ++sort });
                jeLines.Add(new JournalEntryLineRequest { AccountId = invId, Debit = 0, Credit = amount, Description = $"Inventory: {name}", SortOrder = ++sort });
            }

            var jeResult = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
            {
                EntryDate = req.DeliveryDate,
                Description = $"COGS - Delivery {delivery.DeliveryNumber}",
                ReferenceType = ReferenceType.SalesDelivery,
                ReferenceId = delivery.Id, ReferenceNumber = delivery.DeliveryNumber,
                CurrencyId = order.CurrencyId, ExchangeRate = order.ExchangeRate,
                PostImmediately = true, Lines = jeLines, CreatedBy = _user.UserId
            }, ct);

            if (!jeResult.Succeeded)
                throw new InvalidOperationException($"COGS journal failed: {string.Join(", ", jeResult.Errors)}");

            delivery.JournalEntryId = jeResult.EntryId;
        }

        _db.SalesDeliveries.Add(delivery);

        // Update SO status
        var allDelivered = order.Lines.Where(l => !l.IsDeleted).All(l => l.DeliveredQuantity >= l.Quantity);
        order.Status = allDelivered ? SalesOrderStatus.Delivered : SalesOrderStatus.PartiallyDelivered;
        order.ModifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return delivery.Id;
    }
}

// ── Customer Invoice Queries ──────────────────────────────────────────────────

public class GetCustomerInvoicesQuery : IRequest<List<CustomerInvoiceDto>>
{
    public InvoiceStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class GetCustomerInvoicesQueryHandler : IRequestHandler<GetCustomerInvoicesQuery, List<CustomerInvoiceDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCustomerInvoicesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CustomerInvoiceDto>> Handle(GetCustomerInvoicesQuery req, CancellationToken ct)
    {
        var q = _db.CustomerInvoices
            .Include(i => i.Customer).Include(i => i.Currency).Include(i => i.Salesperson)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Where(i => !i.IsDeleted);
        if (req.Status.HasValue) q = q.Where(i => i.Status == req.Status.Value);
        if (req.CustomerId.HasValue) q = q.Where(i => i.CustomerId == req.CustomerId.Value);
        if (req.FromDate.HasValue) q = q.Where(i => i.InvoiceDate >= req.FromDate.Value);
        if (req.ToDate.HasValue) q = q.Where(i => i.InvoiceDate <= req.ToDate.Value);
        var list = await q.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.CreatedAt).ToListAsync(ct);
        return list.Select(SalesOrderMapper.MapInvoiceToDto).ToList();
    }
}

public class GetCustomerInvoiceQuery : IRequest<CustomerInvoiceDto>
{
    public Guid Id { get; set; }
}

public class GetCustomerInvoiceQueryHandler : IRequestHandler<GetCustomerInvoiceQuery, CustomerInvoiceDto>
{
    private readonly IApplicationDbContext _db;
    public GetCustomerInvoiceQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CustomerInvoiceDto> Handle(GetCustomerInvoiceQuery req, CancellationToken ct)
    {
        var inv = await _db.CustomerInvoices
            .Include(i => i.Customer).Include(i => i.Currency).Include(i => i.Salesperson)
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(i => i.Id == req.Id && !i.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(CustomerInvoice), req.Id);
        return SalesOrderMapper.MapInvoiceToDto(inv);
    }
}

// ── Create Customer Invoice ───────────────────────────────────────────────────

public class CreateCustomerInvoiceLineRequest
{
    public Guid ProductId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal? TaxRateOverride { get; set; }
}

public class CreateCustomerInvoiceCommand : IRequest<Guid>
{
    public Guid CustomerId { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public Guid? SalespersonId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public List<CreateCustomerInvoiceLineRequest> Lines { get; set; } = [];
}

public class CreateCustomerInvoiceCommandValidator : AbstractValidator<CreateCustomerInvoiceCommand>
{
    public CreateCustomerInvoiceCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.InvoiceDate);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.ProductId).NotEmpty();
            l.RuleFor(x => x.Quantity).GreaterThan(0);
            l.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        });
    }
}

public class CreateCustomerInvoiceCommandHandler : IRequestHandler<CreateCustomerInvoiceCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public CreateCustomerInvoiceCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<Guid> Handle(CreateCustomerInvoiceCommand req, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV{year}-";
        var last = await _db.CustomerInvoices.IgnoreQueryFilters()
            .Where(i => i.InvoiceNumber.StartsWith(prefix)).OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;

        var productIds = req.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _db.Products.Include(p => p.TaxRate)
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted).ToListAsync(ct);

        var invoice = new CustomerInvoice
        {
            InvoiceNumber = $"{prefix}{seq:D5}",
            CustomerId = req.CustomerId, InvoiceDate = req.InvoiceDate, DueDate = req.DueDate,
            CurrencyId = req.CurrencyId, ExchangeRate = req.ExchangeRate,
            SalespersonId = req.SalespersonId, SalesOrderId = req.SalesOrderId,
            Status = InvoiceStatus.Draft, CreatedBy = _user.UserId
        };

        decimal subTotal = 0, taxTotal = 0, discountTotal = 0;
        int sort = 0;
        foreach (var lineReq in req.Lines)
        {
            var product = products.First(p => p.Id == lineReq.ProductId);
            var taxRate = lineReq.TaxRateOverride ?? product.TaxRate?.Rate ?? 0;
            // VAT on gross BEFORE discount per spec
            var lineTotal = lineReq.UnitPrice * lineReq.Quantity;
            var taxAmount = lineTotal * taxRate;
            var discountAmount = lineTotal * (lineReq.DiscountPercent / 100m);
            var netAmount = lineTotal - discountAmount + taxAmount;

            invoice.Lines.Add(new CustomerInvoiceLine
            {
                ProductId = lineReq.ProductId,
                Description = (lineReq.Description?.Trim() ?? product.Name),
                Quantity = lineReq.Quantity, UnitPrice = lineReq.UnitPrice,
                DiscountPercent = lineReq.DiscountPercent, DiscountAmount = discountAmount,
                TaxRate = taxRate, TaxAmount = taxAmount,
                LineTotal = lineTotal, NetAmount = netAmount,
                SortOrder = ++sort, CreatedBy = _user.UserId
            });
            subTotal += lineTotal; taxTotal += taxAmount; discountTotal += discountAmount;
        }

        invoice.SubTotal = subTotal;
        invoice.TaxAmount = taxTotal;
        invoice.DiscountAmount = discountTotal;
        invoice.TotalAmount = subTotal - discountTotal + taxTotal;
        _db.CustomerInvoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        return invoice.Id;
    }
}

// ── Post Customer Invoice ─────────────────────────────────────────────────────

public class PostCustomerInvoiceCommand : IRequest
{
    public Guid Id { get; set; }
}

public class PostCustomerInvoiceCommandHandler : IRequestHandler<PostCustomerInvoiceCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IAccountingService _accounting;

    public PostCustomerInvoiceCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAccountingService accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task Handle(PostCustomerInvoiceCommand req, CancellationToken ct)
    {
        var invoice = await _db.CustomerInvoices
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Include(i => i.Customer)
            .Include(i => i.Currency)
            .FirstOrDefaultAsync(i => i.Id == req.Id && !i.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(CustomerInvoice), req.Id);

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException($"Cannot post invoice in status {invoice.Status}.");

        var receivableAccountId = invoice.Customer.ReceivableAccountId
            ?? throw new InvalidOperationException("Customer has no Receivable Account configured.");

        // Get VAT output account
        var vatOutputAcc = await _db.Accounts.FirstOrDefaultAsync(a => a.Code == "2200" && !a.IsDeleted, ct);

        var jeLines = new List<JournalEntryLineRequest>();
        int sort = 0;

        // Dr: Customer Receivable (total net)
        jeLines.Add(new JournalEntryLineRequest
        {
            AccountId = receivableAccountId, Debit = invoice.TotalAmount, Credit = 0,
            Description = $"Customer Invoice {invoice.InvoiceNumber}", SortOrder = ++sort
        });

        // Cr: Sales Revenue per line (gross line total) + Cr: Discount adjustment
        foreach (var line in invoice.Lines.Where(l => !l.IsDeleted))
        {
            var salesAccountId = line.Product.SalesAccountId;
            if (!salesAccountId.HasValue) continue;
            // Credit the gross line amount
            jeLines.Add(new JournalEntryLineRequest
            {
                AccountId = salesAccountId.Value, Debit = 0, Credit = line.LineTotal,
                Description = $"Sales: {line.Product.Name} x{line.Quantity}", SortOrder = ++sort
            });
            // Debit discount (reduces revenue)
            if (line.DiscountAmount > 0)
            {
                jeLines.Add(new JournalEntryLineRequest
                {
                    AccountId = salesAccountId.Value, Debit = line.DiscountAmount, Credit = 0,
                    Description = $"Discount: {line.Product.Name}", SortOrder = ++sort
                });
            }
        }

        // Cr: Output VAT
        if (invoice.TaxAmount > 0 && vatOutputAcc is not null)
        {
            jeLines.Add(new JournalEntryLineRequest
            {
                AccountId = vatOutputAcc.Id, Debit = 0, Credit = invoice.TaxAmount,
                Description = "Output VAT 14%", SortOrder = ++sort
            });
        }

        var jeResult = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            EntryDate = invoice.InvoiceDate,
            Description = $"Customer Invoice {invoice.InvoiceNumber} - {invoice.Customer.Name}",
            ReferenceType = ReferenceType.SalesInvoice,
            ReferenceId = invoice.Id, ReferenceNumber = invoice.InvoiceNumber,
            CurrencyId = invoice.CurrencyId, ExchangeRate = invoice.ExchangeRate,
            PostImmediately = true, Lines = jeLines, CreatedBy = _user.UserId
        }, ct);

        if (!jeResult.Succeeded)
            throw new InvalidOperationException($"Accounting failed: {string.Join(", ", jeResult.Errors)}");

        invoice.JournalEntryId = jeResult.EntryId;
        invoice.Status = InvoiceStatus.Posted;
        invoice.ModifiedAt = DateTime.UtcNow;
        invoice.ModifiedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);

        // Auto-create sales commission if salesperson configured
        if (invoice.SalespersonId.HasValue)
        {
            var commissionRate = await _db.CommissionRates
                .FirstOrDefaultAsync(c => c.IsDefault && c.IsActive && !c.IsDeleted, ct);
            if (commissionRate is not null)
            {
                var salesperson = await _db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == invoice.SalespersonId.Value, ct);
                var commission = new SalesCommission
                {
                    SalespersonId = invoice.SalespersonId,
                    SalespersonName = salesperson != null ? $"{salesperson.FirstName} {salesperson.LastName}" : "Unknown",
                    CommissionRateId = commissionRate.Id,
                    Rate = commissionRate.Rate,
                    BaseSalesAmount = invoice.TotalAmount,
                    CommissionAmount = invoice.TotalAmount * commissionRate.Rate,
                    CurrencyId = invoice.CurrencyId,
                    ExchangeRate = invoice.ExchangeRate,
                    Status = CommissionStatus.Pending,
                    CustomerInvoiceId = invoice.Id,
                    CreatedBy = _user.UserId
                };
                _db.SalesCommissions.Add(commission);
                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
