using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Finance;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Finance.Cash;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public class CashReceiptDto
{
    public Guid   Id              { get; set; }
    public string ReceiptNumber   { get; set; } = string.Empty;
    public DateOnly ReceiptDate   { get; set; }
    public string? ReceivedFrom   { get; set; }
    public decimal Amount         { get; set; }
    public string  CurrencyCode   { get; set; } = string.Empty;
    public decimal ExchangeRate   { get; set; }
    public Guid    CashAccountId  { get; set; }
    public string  CashAccountName { get; set; } = string.Empty;
    public Guid    ContraAccountId { get; set; }
    public string  ContraAccountName { get; set; } = string.Empty;
    public string? Description    { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes          { get; set; }
    public string? PreparedBy     { get; set; }
    public string? ApprovedBy     { get; set; }
    public Guid?   JournalEntryId { get; set; }
    public CashTransactionStatus Status { get; set; }
    public string  StatusName => Status.ToString();
    public DateTime CreatedAt  { get; set; }
}

// ── Queries ───────────────────────────────────────────────────────────────────

public class GetCashReceiptsQuery : IRequest<List<CashReceiptDto>>
{
    public CashTransactionStatus? Status        { get; set; }
    public Guid?                  CashAccountId { get; set; }
    public DateOnly?              FromDate      { get; set; }
    public DateOnly?              ToDate        { get; set; }
    public int                    Page          { get; set; } = 1;
    public int                    PageSize      { get; set; } = 50;
}

public class GetCashReceiptsQueryHandler : IRequestHandler<GetCashReceiptsQuery, List<CashReceiptDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCashReceiptsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CashReceiptDto>> Handle(GetCashReceiptsQuery req, CancellationToken ct)
    {
        var q = _db.CashReceipts
            .Include(r => r.Currency)
            .Include(r => r.CashAccount)
            .Include(r => r.ContraAccount)
            .AsQueryable();

        if (req.Status.HasValue)        q = q.Where(r => r.Status == req.Status.Value);
        if (req.CashAccountId.HasValue) q = q.Where(r => r.CashAccountId == req.CashAccountId.Value);
        if (req.FromDate.HasValue)      q = q.Where(r => r.ReceiptDate >= req.FromDate.Value);
        if (req.ToDate.HasValue)        q = q.Where(r => r.ReceiptDate <= req.ToDate.Value);

        var skip = (req.Page - 1) * req.PageSize;

        return await q
            .OrderByDescending(r => r.ReceiptDate)
            .ThenByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(req.PageSize)
            .Select(r => new CashReceiptDto
            {
                Id = r.Id, ReceiptNumber = r.ReceiptNumber,
                ReceiptDate = r.ReceiptDate, ReceivedFrom = r.ReceivedFrom,
                Amount = r.Amount, CurrencyCode = r.Currency.Code,
                ExchangeRate = r.ExchangeRate,
                CashAccountId = r.CashAccountId, CashAccountName = r.CashAccount.Name,
                ContraAccountId = r.ContraAccountId, ContraAccountName = r.ContraAccount.Name,
                Description = r.Description, ReferenceNumber = r.ReferenceNumber,
                Notes = r.Notes, PreparedBy = r.PreparedBy, ApprovedBy = r.ApprovedBy,
                JournalEntryId = r.JournalEntryId, Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);
    }
}

public class GetCashReceiptQuery : IRequest<CashReceiptDto?>
{
    public Guid Id { get; set; }
}

public class GetCashReceiptQueryHandler : IRequestHandler<GetCashReceiptQuery, CashReceiptDto?>
{
    private readonly IApplicationDbContext _db;
    public GetCashReceiptQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CashReceiptDto?> Handle(GetCashReceiptQuery req, CancellationToken ct)
    {
        return await _db.CashReceipts
            .Include(r => r.Currency)
            .Include(r => r.CashAccount)
            .Include(r => r.ContraAccount)
            .Where(r => r.Id == req.Id)
            .Select(r => new CashReceiptDto
            {
                Id = r.Id, ReceiptNumber = r.ReceiptNumber,
                ReceiptDate = r.ReceiptDate, ReceivedFrom = r.ReceivedFrom,
                Amount = r.Amount, CurrencyCode = r.Currency.Code,
                ExchangeRate = r.ExchangeRate,
                CashAccountId = r.CashAccountId, CashAccountName = r.CashAccount.Name,
                ContraAccountId = r.ContraAccountId, ContraAccountName = r.ContraAccount.Name,
                Description = r.Description, ReferenceNumber = r.ReferenceNumber,
                Notes = r.Notes, PreparedBy = r.PreparedBy, ApprovedBy = r.ApprovedBy,
                JournalEntryId = r.JournalEntryId, Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
    }
}

// ── Create (Draft) ────────────────────────────────────────────────────────────

public class CreateCashReceiptCommand : IRequest<Guid>
{
    public DateOnly ReceiptDate    { get; set; }
    public string?  ReceivedFrom   { get; set; }
    public decimal  Amount         { get; set; }
    public Guid     CurrencyId     { get; set; }
    public decimal  ExchangeRate   { get; set; } = 1m;
    public Guid     CashAccountId  { get; set; }
    public Guid     ContraAccountId { get; set; }
    public string?  Description    { get; set; }
    public string?  ReferenceNumber { get; set; }
    public string?  Notes          { get; set; }
    public string?  PreparedBy     { get; set; }
    public string?  ApprovedBy     { get; set; }
}

public class CreateCashReceiptCommandValidator : AbstractValidator<CreateCashReceiptCommand>
{
    public CreateCashReceiptCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.CashAccountId).NotEmpty();
        RuleFor(x => x.ContraAccountId).NotEmpty()
            .NotEqual(x => x.CashAccountId).WithMessage("Contra account must differ from cash account.");
        RuleFor(x => x.ReceivedFrom).MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.PreparedBy).MaximumLength(200);
        RuleFor(x => x.ApprovedBy).MaximumLength(200);
    }
}

public class CreateCashReceiptCommandHandler : IRequestHandler<CreateCashReceiptCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;

    public CreateCashReceiptCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<Guid> Handle(CreateCashReceiptCommand req, CancellationToken ct)
    {
        var year   = DateTime.UtcNow.Year;
        var prefix = $"CR{year}-";

        var last = await _db.CashReceipts
            .IgnoreQueryFilters()
            .Where(r => r.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReceiptNumber)
            .Select(r => r.ReceiptNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && last.Length > prefix.Length
            && int.TryParse(last[prefix.Length..], out var n))
            seq = n + 1;

        var receipt = new CashReceipt
        {
            ReceiptNumber    = $"{prefix}{seq:D5}",
            ReceiptDate      = req.ReceiptDate,
            ReceivedFrom     = req.ReceivedFrom?.Trim(),
            Amount           = req.Amount,
            CurrencyId       = req.CurrencyId,
            ExchangeRate     = req.ExchangeRate,
            CashAccountId    = req.CashAccountId,
            ContraAccountId  = req.ContraAccountId,
            Description      = req.Description?.Trim(),
            ReferenceNumber  = req.ReferenceNumber?.Trim(),
            Notes            = req.Notes?.Trim(),
            PreparedBy       = req.PreparedBy?.Trim(),
            ApprovedBy       = req.ApprovedBy?.Trim(),
            Status           = CashTransactionStatus.Draft,
            CreatedBy        = _user.UserId
        };

        _db.CashReceipts.Add(receipt);
        await _db.SaveChangesAsync(ct);
        return receipt.Id;
    }
}

// ── Post ──────────────────────────────────────────────────────────────────────

public class PostCashReceiptCommand : IRequest
{
    public Guid Id { get; set; }
}

public class PostCashReceiptCommandHandler : IRequestHandler<PostCashReceiptCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;
    private readonly IAccountingService    _accounting;

    public PostCashReceiptCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService   user,
        IAccountingService    accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task Handle(PostCashReceiptCommand req, CancellationToken ct)
    {
        var receipt = await _db.CashReceipts
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new NotFoundException(nameof(CashReceipt), req.Id);

        if (receipt.Status != CashTransactionStatus.Draft)
            throw new InvalidOperationException(
                $"Only Draft receipts can be posted. Current status: {receipt.Status}.");

        // Dr CashAccount / Cr ContraAccount
        var jeResult = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            EntryDate       = receipt.ReceiptDate,
            Description     = $"Cash Receipt: {receipt.ReceiptNumber} - {receipt.Description}".TrimEnd(' ', '-'),
            ReferenceType   = ReferenceType.Receipt,
            ReferenceId     = receipt.Id,
            ReferenceNumber = receipt.ReceiptNumber,
            CurrencyId      = receipt.CurrencyId,
            ExchangeRate    = receipt.ExchangeRate,
            PostImmediately = true,
            Lines = new List<JournalEntryLineRequest>
            {
                new()
                {
                    AccountId = receipt.CashAccountId,
                    Debit     = receipt.Amount,
                    Credit    = 0m,
                    Description = $"Cash in: {receipt.ReceivedFrom ?? receipt.ReceiptNumber}",
                    SortOrder = 1
                },
                new()
                {
                    AccountId = receipt.ContraAccountId,
                    Debit     = 0m,
                    Credit    = receipt.Amount,
                    Description = receipt.Description ?? receipt.ReceiptNumber,
                    SortOrder = 2
                }
            },
            CreatedBy = _user.UserId
        }, ct);

        if (!jeResult.Succeeded)
            throw new InvalidOperationException(
                $"Cash receipt posting failed: {string.Join(", ", jeResult.Errors)}");

        receipt.Status         = CashTransactionStatus.Posted;
        receipt.JournalEntryId = jeResult.EntryId;
        receipt.ModifiedAt     = DateTime.UtcNow;
        receipt.ModifiedBy     = _user.UserId;

        await _db.SaveChangesAsync(ct);
    }
}

// ── Void ──────────────────────────────────────────────────────────────────────

public class VoidCashReceiptCommand : IRequest
{
    public Guid   Id     { get; set; }
    public string Reason { get; set; } = "Voided";
}

public class VoidCashReceiptCommandHandler : IRequestHandler<VoidCashReceiptCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;
    private readonly IAccountingService    _accounting;

    public VoidCashReceiptCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService   user,
        IAccountingService    accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task Handle(VoidCashReceiptCommand req, CancellationToken ct)
    {
        var receipt = await _db.CashReceipts
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new NotFoundException(nameof(CashReceipt), req.Id);

        if (receipt.Status != CashTransactionStatus.Posted)
            throw new InvalidOperationException(
                $"Only Posted receipts can be voided. Current status: {receipt.Status}.");

        if (receipt.JournalEntryId.HasValue)
        {
            var reverseResult = await _accounting.ReverseJournalEntryAsync(
                receipt.JournalEntryId.Value,
                req.Reason,
                DateOnly.FromDateTime(DateTime.UtcNow),
                ct);

            if (!reverseResult.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to reverse journal entry: {string.Join(", ", reverseResult.Errors)}");
        }

        receipt.Status     = CashTransactionStatus.Voided;
        receipt.ModifiedAt = DateTime.UtcNow;
        receipt.ModifiedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
    }
}

// ── Delete (Draft only) ───────────────────────────────────────────────────────

public class DeleteCashReceiptCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteCashReceiptCommandHandler : IRequestHandler<DeleteCashReceiptCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;

    public DeleteCashReceiptCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task Handle(DeleteCashReceiptCommand req, CancellationToken ct)
    {
        var receipt = await _db.CashReceipts
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct)
            ?? throw new NotFoundException(nameof(CashReceipt), req.Id);

        if (receipt.Status != CashTransactionStatus.Draft)
            throw new InvalidOperationException(
                "Only Draft receipts can be deleted.");

        receipt.IsDeleted  = true;
        receipt.ModifiedAt = DateTime.UtcNow;
        receipt.ModifiedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
    }
}
