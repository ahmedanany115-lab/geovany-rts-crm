using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Warehouse;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Warehouse.GoodsReceipts;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public class GoodsReceiptLineDto
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? SerialNumber { get; set; }
    public string? Unit { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

public class GoodsReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateOnly ReceiptDate { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public string? PreparedBy { get; set; }
    public string? ReceivedBy { get; set; }
    public string? Notes { get; set; }
    public GoodsReceiptStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime CreatedAt { get; set; }
    public List<GoodsReceiptLineDto> Lines { get; set; } = [];
}

public class GoodsReceiptListDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateOnly ReceiptDate { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid WarehouseId { get; set; }
    public string? PreparedBy { get; set; }
    public string? ReceivedBy { get; set; }
    public GoodsReceiptStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime CreatedAt { get; set; }
}

// ── Line Request ──────────────────────────────────────────────────────────────

public class GoodsReceiptLineRequest
{
    public Guid? ProductId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? SerialNumber { get; set; }
    public string? Unit { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

// ── Queries ───────────────────────────────────────────────────────────────────

public class GetGoodsReceiptsQuery : IRequest<List<GoodsReceiptListDto>>
{
    public GoodsReceiptStatus? Status { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetGoodsReceiptsQueryHandler : IRequestHandler<GetGoodsReceiptsQuery, List<GoodsReceiptListDto>>
{
    private readonly IApplicationDbContext _db;
    public GetGoodsReceiptsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<GoodsReceiptListDto>> Handle(GetGoodsReceiptsQuery req, CancellationToken ct)
    {
        var q = _db.GoodsReceipts.Where(r => !r.IsDeleted);
        if (req.Status.HasValue)      q = q.Where(r => r.Status == req.Status.Value);
        if (req.WarehouseId.HasValue) q = q.Where(r => r.WarehouseId == req.WarehouseId.Value);
        if (req.SupplierId.HasValue)  q = q.Where(r => r.SupplierId == req.SupplierId.Value);
        if (req.FromDate.HasValue)    q = q.Where(r => r.ReceiptDate >= req.FromDate.Value);
        if (req.ToDate.HasValue)      q = q.Where(r => r.ReceiptDate <= req.ToDate.Value);

        var skip = (req.Page - 1) * req.PageSize;
        return await q
            .OrderByDescending(r => r.ReceiptDate)
            .ThenByDescending(r => r.CreatedAt)
            .Skip(skip).Take(req.PageSize)
            .Select(r => new GoodsReceiptListDto
            {
                Id = r.Id, ReceiptNumber = r.ReceiptNumber, ReceiptDate = r.ReceiptDate,
                SupplierId = r.SupplierId, WarehouseId = r.WarehouseId,
                PreparedBy = r.PreparedBy, ReceivedBy = r.ReceivedBy,
                Status = r.Status, CreatedAt = r.CreatedAt
            }).ToListAsync(ct);
    }
}

public class GetGoodsReceiptQuery : IRequest<GoodsReceiptDto>
{
    public Guid Id { get; set; }
}

public class GetGoodsReceiptQueryHandler : IRequestHandler<GetGoodsReceiptQuery, GoodsReceiptDto>
{
    private readonly IApplicationDbContext _db;
    public GetGoodsReceiptQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<GoodsReceiptDto> Handle(GetGoodsReceiptQuery req, CancellationToken ct)
    {
        var receipt = await _db.GoodsReceipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == req.Id && !r.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(GoodsReceipt), req.Id);

        return MapToDto(receipt);
    }

    private static GoodsReceiptDto MapToDto(GoodsReceipt r) => new()
    {
        Id = r.Id, ReceiptNumber = r.ReceiptNumber, ReceiptDate = r.ReceiptDate,
        SupplierId = r.SupplierId, PurchaseOrderId = r.PurchaseOrderId,
        WarehouseId = r.WarehouseId, PreparedBy = r.PreparedBy, ReceivedBy = r.ReceivedBy,
        Notes = r.Notes, Status = r.Status, CreatedAt = r.CreatedAt,
        Lines = r.Lines.Where(l => !l.IsDeleted).OrderBy(l => l.SortOrder).Select(l => new GoodsReceiptLineDto
        {
            Id = l.Id, ProductId = l.ProductId, ItemDescription = l.ItemDescription,
            Quantity = l.Quantity, SerialNumber = l.SerialNumber, Unit = l.Unit,
            Notes = l.Notes, SortOrder = l.SortOrder
        }).ToList()
    };
}

// ── Create ────────────────────────────────────────────────────────────────────

public class CreateGoodsReceiptCommand : IRequest<CreateGoodsReceiptResult>
{
    public DateOnly ReceiptDate { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public string? PreparedBy { get; set; }
    public string? ReceivedBy { get; set; }
    public string? Notes { get; set; }
    public List<GoodsReceiptLineRequest> Lines { get; set; } = [];
}

public class CreateGoodsReceiptResult
{
    public bool Succeeded { get; set; }
    public Guid ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
}

public class CreateGoodsReceiptCommandValidator : AbstractValidator<CreateGoodsReceiptCommand>
{
    public CreateGoodsReceiptCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.ItemDescription).NotEmpty().MaximumLength(500);
            l.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public class CreateGoodsReceiptCommandHandler : IRequestHandler<CreateGoodsReceiptCommand, CreateGoodsReceiptResult>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public CreateGoodsReceiptCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<CreateGoodsReceiptResult> Handle(CreateGoodsReceiptCommand req, CancellationToken ct)
    {
        // Validate warehouse exists
        var warehouseExists = await _db.Warehouses.AnyAsync(w => w.Id == req.WarehouseId && !w.IsDeleted, ct);
        if (!warehouseExists)
            throw new NotFoundException("Warehouse", req.WarehouseId);

        // Generate receipt number GR{year}-{seq:D5}
        var year = DateTime.UtcNow.Year;
        var prefix = $"GR{year}-";
        var last = await _db.GoodsReceipts.IgnoreQueryFilters()
            .Where(r => r.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReceiptNumber)
            .Select(r => r.ReceiptNumber)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;

        var receipt = new GoodsReceipt
        {
            ReceiptNumber = $"{prefix}{seq:D5}",
            ReceiptDate = req.ReceiptDate,
            SupplierId = req.SupplierId,
            PurchaseOrderId = req.PurchaseOrderId,
            WarehouseId = req.WarehouseId,
            PreparedBy = req.PreparedBy?.Trim(),
            ReceivedBy = req.ReceivedBy?.Trim(),
            Notes = req.Notes?.Trim(),
            Status = GoodsReceiptStatus.Draft,
            CreatedBy = _user.UserId
        };

        int sort = 0;
        foreach (var lineReq in req.Lines)
        {
            receipt.Lines.Add(new GoodsReceiptLine
            {
                ProductId = lineReq.ProductId,
                ItemDescription = lineReq.ItemDescription.Trim(),
                Quantity = lineReq.Quantity,
                SerialNumber = lineReq.SerialNumber?.Trim(),
                Unit = lineReq.Unit?.Trim(),
                Notes = lineReq.Notes?.Trim(),
                SortOrder = lineReq.SortOrder > 0 ? lineReq.SortOrder : ++sort,
                CreatedBy = _user.UserId
            });
        }

        _db.GoodsReceipts.Add(receipt);
        await _db.SaveChangesAsync(ct);

        return new CreateGoodsReceiptResult
        {
            Succeeded = true,
            ReceiptId = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber
        };
    }
}

// ── Update ────────────────────────────────────────────────────────────────────

public class UpdateGoodsReceiptCommand : IRequest
{
    public Guid Id { get; set; }
    public DateOnly ReceiptDate { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public string? PreparedBy { get; set; }
    public string? ReceivedBy { get; set; }
    public string? Notes { get; set; }
    public List<GoodsReceiptLineRequest> Lines { get; set; } = [];
}

public class UpdateGoodsReceiptCommandValidator : AbstractValidator<UpdateGoodsReceiptCommand>
{
    public UpdateGoodsReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.ItemDescription).NotEmpty().MaximumLength(500);
            l.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public class UpdateGoodsReceiptCommandHandler : IRequestHandler<UpdateGoodsReceiptCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public UpdateGoodsReceiptCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task Handle(UpdateGoodsReceiptCommand req, CancellationToken ct)
    {
        var receipt = await _db.GoodsReceipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == req.Id && !r.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(GoodsReceipt), req.Id);

        if (receipt.Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException("Only Draft receipts can be updated.");

        receipt.ReceiptDate = req.ReceiptDate;
        receipt.SupplierId = req.SupplierId;
        receipt.PurchaseOrderId = req.PurchaseOrderId;
        receipt.WarehouseId = req.WarehouseId;
        receipt.PreparedBy = req.PreparedBy?.Trim();
        receipt.ReceivedBy = req.ReceivedBy?.Trim();
        receipt.Notes = req.Notes?.Trim();
        receipt.ModifiedAt = DateTime.UtcNow;
        receipt.ModifiedBy = _user.UserId;

        // Soft-delete existing lines
        foreach (var line in receipt.Lines.Where(l => !l.IsDeleted))
        {
            line.IsDeleted = true;
            line.ModifiedAt = DateTime.UtcNow;
            line.ModifiedBy = _user.UserId;
        }

        // Add new lines
        int sort = 0;
        foreach (var lineReq in req.Lines)
        {
            receipt.Lines.Add(new GoodsReceiptLine
            {
                ProductId = lineReq.ProductId,
                ItemDescription = lineReq.ItemDescription.Trim(),
                Quantity = lineReq.Quantity,
                SerialNumber = lineReq.SerialNumber?.Trim(),
                Unit = lineReq.Unit?.Trim(),
                Notes = lineReq.Notes?.Trim(),
                SortOrder = lineReq.SortOrder > 0 ? lineReq.SortOrder : ++sort,
                CreatedBy = _user.UserId
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────

public class DeleteGoodsReceiptCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteGoodsReceiptCommandHandler : IRequestHandler<DeleteGoodsReceiptCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public DeleteGoodsReceiptCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task Handle(DeleteGoodsReceiptCommand req, CancellationToken ct)
    {
        var receipt = await _db.GoodsReceipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == req.Id && !r.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(GoodsReceipt), req.Id);

        if (receipt.Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException("Only Draft receipts can be deleted.");

        receipt.IsDeleted = true;
        receipt.ModifiedAt = DateTime.UtcNow;
        receipt.ModifiedBy = _user.UserId;

        foreach (var line in receipt.Lines.Where(l => !l.IsDeleted))
        {
            line.IsDeleted = true;
            line.ModifiedAt = DateTime.UtcNow;
            line.ModifiedBy = _user.UserId;
        }

        await _db.SaveChangesAsync(ct);
    }
}
