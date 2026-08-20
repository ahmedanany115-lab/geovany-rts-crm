using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Operational;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Operational.PurchaseOrders;

// ── Purchase Receipt ──────────────────────────────────────────────────────────

public class ReceiveLineRequest
{
    public Guid PurchaseOrderLineId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class CreatePurchaseReceiptCommand : IRequest<Guid>
{
    public Guid PurchaseOrderId { get; set; }
    public DateOnly ReceiptDate { get; set; }
    public string? Notes { get; set; }
    public List<ReceiveLineRequest> Lines { get; set; } = [];
}

public class CreatePurchaseReceiptCommandValidator : AbstractValidator<CreatePurchaseReceiptCommand>
{
    public CreatePurchaseReceiptCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.Quantity).GreaterThan(0);
            l.RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreatePurchaseReceiptCommandHandler : IRequestHandler<CreatePurchaseReceiptCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IInventoryService _inventory;

    public CreatePurchaseReceiptCommandHandler(IApplicationDbContext db, ICurrentUserService user, IInventoryService inventory)
        => (_db, _user, _inventory) = (db, user, inventory);

    public async Task<Guid> Handle(CreatePurchaseReceiptCommand request, CancellationToken ct)
    {
        var order = await _db.PurchaseOrders
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Include(o => o.Currency)
            .FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId && !o.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrder), request.PurchaseOrderId);

        if (order.Status == PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Cannot receive against a Draft purchase order. Approve it first.");
        if (order.Status == PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot receive against a cancelled purchase order.");

        // Generate receipt number
        var year = DateTime.UtcNow.Year;
        var prefix = $"GRN{year}-";
        var last = await _db.PurchaseReceipts.IgnoreQueryFilters()
            .Where(r => r.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReceiptNumber)
            .Select(r => r.ReceiptNumber)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;
        var receiptNumber = $"{prefix}{seq:D5}";

        var receipt = new PurchaseReceipt
        {
            ReceiptNumber = receiptNumber,
            PurchaseOrderId = order.Id,
            SupplierId = order.SupplierId,
            WarehouseId = order.WarehouseId,
            ReceiptDate = request.ReceiptDate,
            CurrencyId = order.CurrencyId,
            ExchangeRate = order.ExchangeRate,
            Notes = request.Notes?.Trim(),
            CreatedBy = _user.UserId
        };

        decimal total = 0;

        foreach (var lineReq in request.Lines)
        {
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == lineReq.PurchaseOrderLineId && !l.IsDeleted)
                ?? throw new NotFoundException(nameof(PurchaseOrderLine), lineReq.PurchaseOrderLineId);

            var pendingQty = orderLine.Quantity - orderLine.ReceivedQuantity;
            if (lineReq.Quantity > pendingQty)
                throw new InvalidOperationException(
                    $"Cannot receive {lineReq.Quantity} units of {orderLine.Product.Name}. Pending: {pendingQty}.");

            var lineCost = lineReq.Quantity * lineReq.UnitCost;
            receipt.Lines.Add(new PurchaseReceiptLine
            {
                PurchaseOrderLineId = lineReq.PurchaseOrderLineId,
                ProductId = orderLine.ProductId,
                Quantity = lineReq.Quantity,
                UnitCost = lineReq.UnitCost,
                TotalCost = lineCost,
                CreatedBy = _user.UserId
            });

            // Update order line received quantity
            orderLine.ReceivedQuantity += lineReq.Quantity;
            orderLine.ModifiedAt = DateTime.UtcNow;

            // Move inventory
            await _inventory.MoveInventoryAsync(
                orderLine.ProductId, order.WarehouseId,
                lineReq.Quantity, lineReq.UnitCost,
                InventoryMovementType.PurchaseReceipt,
                request.ReceiptDate,
                referenceType: "PurchaseReceipt",
                referenceId: receipt.Id,
                referenceNumber: receiptNumber,
                notes: $"GRN from PO {order.PONumber}",
                createdBy: _user.UserId,
                cancellationToken: ct);

            total += lineCost;
        }

        receipt.TotalAmount = total;
        _db.PurchaseReceipts.Add(receipt);

        // Update PO status
        var allReceived = order.Lines.Where(l => !l.IsDeleted).All(l => l.ReceivedQuantity >= l.Quantity);
        var anyReceived = order.Lines.Where(l => !l.IsDeleted).Any(l => l.ReceivedQuantity > 0);
        order.Status = allReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
        order.ModifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return receipt.Id;
    }
}

// ── Supplier Invoice ──────────────────────────────────────────────────────────

public class CreateSupplierInvoiceLineRequest
{
    public Guid ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxRate { get; set; }
}

public class CreateSupplierInvoiceCommand : IRequest<Guid>
{
    public Guid SupplierId { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public Guid? PurchaseReceiptId { get; set; }
    public List<CreateSupplierInvoiceLineRequest> Lines { get; set; } = [];
}

public class CreateSupplierInvoiceCommandValidator : AbstractValidator<CreateSupplierInvoiceCommand>
{
    public CreateSupplierInvoiceCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.InvoiceDate);
        RuleFor(x => x.Lines).NotEmpty();
    }
}

public class CreateSupplierInvoiceCommandHandler : IRequestHandler<CreateSupplierInvoiceCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public CreateSupplierInvoiceCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<Guid> Handle(CreateSupplierInvoiceCommand request, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"SINV{year}-";
        var last = await _db.SupplierInvoices.IgnoreQueryFilters()
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;

        var invoice = new SupplierInvoice
        {
            InvoiceNumber = $"{prefix}{seq:D5}",
            SupplierInvoiceNumber = request.SupplierInvoiceNumber?.Trim(),
            SupplierId = request.SupplierId,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            CurrencyId = request.CurrencyId,
            ExchangeRate = request.ExchangeRate,
            PurchaseReceiptId = request.PurchaseReceiptId,
            Status = InvoiceStatus.Draft,
            CreatedBy = _user.UserId
        };

        decimal subTotal = 0, taxTotal = 0, discountTotal = 0;
        int sort = 0;

        foreach (var lineReq in request.Lines)
        {
            var lineTotal = lineReq.UnitPrice * lineReq.Quantity;
            var taxAmount = lineTotal * lineReq.TaxRate;       // VAT on gross (before discount)
            var discountAmount = lineTotal * (lineReq.DiscountPercent / 100m);
            var netAmount = lineTotal - discountAmount + taxAmount;

            invoice.Lines.Add(new SupplierInvoiceLine
            {
                ProductId = lineReq.ProductId,
                Description = lineReq.Description.Trim(),
                Quantity = lineReq.Quantity,
                UnitPrice = lineReq.UnitPrice,
                DiscountPercent = lineReq.DiscountPercent,
                DiscountAmount = discountAmount,
                TaxRate = lineReq.TaxRate,
                TaxAmount = taxAmount,
                LineTotal = lineTotal,
                NetAmount = netAmount,
                SortOrder = ++sort,
                CreatedBy = _user.UserId
            });

            subTotal += lineTotal;
            taxTotal += taxAmount;
            discountTotal += discountAmount;
        }

        invoice.SubTotal = subTotal;
        invoice.TaxAmount = taxTotal;
        invoice.DiscountAmount = discountTotal;
        invoice.TotalAmount = subTotal - discountTotal + taxTotal;

        _db.SupplierInvoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        return invoice.Id;
    }
}

public class PostSupplierInvoiceCommand : IRequest
{
    public Guid Id { get; set; }
}

public class PostSupplierInvoiceCommandHandler : IRequestHandler<PostSupplierInvoiceCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IAccountingService _accounting;

    public PostSupplierInvoiceCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAccountingService accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task Handle(PostSupplierInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _db.SupplierInvoices
            .Include(i => i.Lines).ThenInclude(l => l.Product)
            .Include(i => i.Supplier)
            .Include(i => i.Currency)
            .FirstOrDefaultAsync(i => i.Id == request.Id && !i.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(SupplierInvoice), request.Id);

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException($"Cannot post invoice in status {invoice.Status}.");

        // Get accounts
        var supplier = invoice.Supplier;
        var payableAccountId = supplier.PayableAccountId
            ?? throw new InvalidOperationException("Supplier has no Payable Account configured.");

        // Get the first product's purchase account (or fall back to a generic inventory account)
        var purchaseAccountIds = invoice.Lines
            .Where(l => !l.IsDeleted)
            .Select(l => l.Product.PurchaseAccountId ?? l.Product.InventoryAccountId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        // Get VAT input account
        var vatInputAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "1400" && !a.IsDeleted, ct);

        // Build journal entry lines
        var jeLines = new List<JournalEntryLineRequest>();
        int sort = 0;

        // Debit: Purchase/Inventory accounts per line
        foreach (var line in invoice.Lines.Where(l => !l.IsDeleted))
        {
            var accountId = line.Product.PurchaseAccountId ?? line.Product.InventoryAccountId;
            if (!accountId.HasValue) continue;

            jeLines.Add(new JournalEntryLineRequest
            {
                AccountId = accountId.Value,
                Debit = line.LineTotal,   // gross before discount and tax
                Credit = 0,
                Description = $"Purchase: {line.Product.Name} x{line.Quantity}",
                SortOrder = ++sort
            });
        }

        // Debit: Input VAT
        if (invoice.TaxAmount > 0 && vatInputAccount is not null)
        {
            jeLines.Add(new JournalEntryLineRequest
            {
                AccountId = vatInputAccount.Id,
                Debit = invoice.TaxAmount,
                Credit = 0,
                Description = "Input VAT",
                SortOrder = ++sort
            });
        }

        // Credit: Supplier Payable (total net)
        jeLines.Add(new JournalEntryLineRequest
        {
            AccountId = payableAccountId,
            Debit = 0,
            Credit = invoice.TotalAmount,
            Description = $"Supplier invoice {invoice.InvoiceNumber}",
            SortOrder = ++sort
        });

        // If there's a discount, credit it back from purchase account
        if (invoice.DiscountAmount > 0 && purchaseAccountIds.Any())
        {
            jeLines.Add(new JournalEntryLineRequest
            {
                AccountId = purchaseAccountIds.First(),
                Debit = 0,
                Credit = invoice.DiscountAmount,
                Description = "Purchase discount",
                SortOrder = ++sort
            });
        }

        var jeResult = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            EntryDate = invoice.InvoiceDate,
            Description = $"Supplier Invoice {invoice.InvoiceNumber} - {supplier.Name}",
            ReferenceType = ReferenceType.PurchaseInvoice,
            ReferenceId = invoice.Id,
            ReferenceNumber = invoice.InvoiceNumber,
            CurrencyId = invoice.CurrencyId,
            ExchangeRate = invoice.ExchangeRate,
            PostImmediately = true,
            Lines = jeLines,
            CreatedBy = _user.UserId
        }, ct);

        if (!jeResult.Succeeded)
            throw new InvalidOperationException($"Failed to post accounting entry: {string.Join(", ", jeResult.Errors)}");

        invoice.JournalEntryId = jeResult.EntryId;
        invoice.Status = InvoiceStatus.Posted;
        invoice.ModifiedAt = DateTime.UtcNow;
        invoice.ModifiedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
    }
}

// ── Get Supplier Invoices ─────────────────────────────────────────────────────

public class SupplierInvoiceListDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? SupplierInvoiceNumber { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GetSupplierInvoicesQuery : IRequest<List<SupplierInvoiceListDto>>
{
    public int? Status { get; set; }
    public Guid? SupplierId { get; set; }
}

public class GetSupplierInvoicesQueryHandler : IRequestHandler<GetSupplierInvoicesQuery, List<SupplierInvoiceListDto>>
{
    private readonly IApplicationDbContext _db;
    public GetSupplierInvoicesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<SupplierInvoiceListDto>> Handle(GetSupplierInvoicesQuery request, CancellationToken ct)
    {
        var q = _db.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Currency)
            .Where(i => !i.IsDeleted);

        if (request.SupplierId.HasValue) q = q.Where(i => i.SupplierId == request.SupplierId.Value);
        if (request.Status.HasValue)     q = q.Where(i => (int)i.Status == request.Status.Value);

        return await q.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.CreatedAt)
            .Select(i => new SupplierInvoiceListDto
            {
                Id = i.Id, InvoiceNumber = i.InvoiceNumber,
                SupplierInvoiceNumber = i.SupplierInvoiceNumber,
                SupplierName = i.Supplier.Name,
                InvoiceDate = i.InvoiceDate, DueDate = i.DueDate,
                CurrencyCode = i.Currency.Code,
                SubTotal = i.SubTotal, TaxAmount = i.TaxAmount, DiscountAmount = i.DiscountAmount,
                TotalAmount = i.TotalAmount, PaidAmount = i.PaidAmount,
                BalanceDue = i.TotalAmount - i.PaidAmount,
                Status = (int)i.Status, StatusName = i.Status.ToString(),
                CreatedAt = i.CreatedAt
            }).ToListAsync(ct);
    }
}

// ── Get Purchase Receipts ─────────────────────────────────────────────────────

public class PurchaseReceiptListDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? PONumber { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateOnly ReceiptDate { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetPurchaseReceiptsQuery : IRequest<List<PurchaseReceiptListDto>>
{
    public Guid? PurchaseOrderId { get; set; }
    public Guid? SupplierId { get; set; }
}

public class GetPurchaseReceiptsQueryHandler : IRequestHandler<GetPurchaseReceiptsQuery, List<PurchaseReceiptListDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPurchaseReceiptsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<PurchaseReceiptListDto>> Handle(GetPurchaseReceiptsQuery request, CancellationToken ct)
    {
        var q = _db.PurchaseReceipts
            .Include(r => r.Supplier)
            .Include(r => r.Warehouse)
            .Include(r => r.PurchaseOrder)
            .Where(r => !r.IsDeleted);

        if (request.PurchaseOrderId.HasValue) q = q.Where(r => r.PurchaseOrderId == request.PurchaseOrderId.Value);
        if (request.SupplierId.HasValue)      q = q.Where(r => r.SupplierId      == request.SupplierId.Value);

        return await q.OrderByDescending(r => r.ReceiptDate).ThenByDescending(r => r.CreatedAt)
            .Select(r => new PurchaseReceiptListDto
            {
                Id = r.Id, ReceiptNumber = r.ReceiptNumber,
                SupplierName = r.Supplier.Name,
                PONumber = r.PurchaseOrder != null ? r.PurchaseOrder.PONumber : null,
                WarehouseName = r.Warehouse.Name,
                ReceiptDate = r.ReceiptDate, TotalAmount = r.TotalAmount, CreatedAt = r.CreatedAt
            }).ToListAsync(ct);
    }
}
