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

public class CashPaymentDto
{
    public Guid   Id               { get; set; }
    public string PaymentNumber    { get; set; } = string.Empty;
    public DateOnly PaymentDate    { get; set; }
    public string? PaidTo          { get; set; }
    public decimal Amount          { get; set; }
    public string  CurrencyCode    { get; set; } = string.Empty;
    public decimal ExchangeRate    { get; set; }
    public Guid    CashAccountId   { get; set; }
    public string  CashAccountName { get; set; } = string.Empty;
    public Guid    ExpenseAccountId { get; set; }
    public string  ExpenseAccountName { get; set; } = string.Empty;
    public string? Description     { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes           { get; set; }
    public string? PreparedBy      { get; set; }
    public string? ApprovedBy      { get; set; }
    public Guid?   JournalEntryId  { get; set; }
    public CashTransactionStatus Status { get; set; }
    public string  StatusName => Status.ToString();
    public DateTime CreatedAt { get; set; }
}

// ── Queries ───────────────────────────────────────────────────────────────────

public class GetCashPaymentsQuery : IRequest<List<CashPaymentDto>>
{
    public CashTransactionStatus? Status        { get; set; }
    public Guid?                  CashAccountId { get; set; }
    public DateOnly?              FromDate      { get; set; }
    public DateOnly?              ToDate        { get; set; }
    public int                    Page          { get; set; } = 1;
    public int                    PageSize      { get; set; } = 50;
}

public class GetCashPaymentsQueryHandler : IRequestHandler<GetCashPaymentsQuery, List<CashPaymentDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCashPaymentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CashPaymentDto>> Handle(GetCashPaymentsQuery req, CancellationToken ct)
    {
        var q = _db.CashPayments
            .Include(p => p.Currency)
            .Include(p => p.CashAccount)
            .Include(p => p.ExpenseAccount)
            .AsQueryable();

        if (req.Status.HasValue)        q = q.Where(p => p.Status == req.Status.Value);
        if (req.CashAccountId.HasValue) q = q.Where(p => p.CashAccountId == req.CashAccountId.Value);
        if (req.FromDate.HasValue)      q = q.Where(p => p.PaymentDate >= req.FromDate.Value);
        if (req.ToDate.HasValue)        q = q.Where(p => p.PaymentDate <= req.ToDate.Value);

        var skip = (req.Page - 1) * req.PageSize;

        return await q
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(req.PageSize)
            .Select(p => new CashPaymentDto
            {
                Id = p.Id, PaymentNumber = p.PaymentNumber,
                PaymentDate = p.PaymentDate, PaidTo = p.PaidTo,
                Amount = p.Amount, CurrencyCode = p.Currency.Code,
                ExchangeRate = p.ExchangeRate,
                CashAccountId = p.CashAccountId, CashAccountName = p.CashAccount.Name,
                ExpenseAccountId = p.ExpenseAccountId, ExpenseAccountName = p.ExpenseAccount.Name,
                Description = p.Description, ReferenceNumber = p.ReferenceNumber,
                Notes = p.Notes, PreparedBy = p.PreparedBy, ApprovedBy = p.ApprovedBy,
                JournalEntryId = p.JournalEntryId, Status = p.Status,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);
    }
}

public class GetCashPaymentQuery : IRequest<CashPaymentDto?>
{
    public Guid Id { get; set; }
}

public class GetCashPaymentQueryHandler : IRequestHandler<GetCashPaymentQuery, CashPaymentDto?>
{
    private readonly IApplicationDbContext _db;
    public GetCashPaymentQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CashPaymentDto?> Handle(GetCashPaymentQuery req, CancellationToken ct)
    {
        return await _db.CashPayments
            .Include(p => p.Currency)
            .Include(p => p.CashAccount)
            .Include(p => p.ExpenseAccount)
            .Where(p => p.Id == req.Id)
            .Select(p => new CashPaymentDto
            {
                Id = p.Id, PaymentNumber = p.PaymentNumber,
                PaymentDate = p.PaymentDate, PaidTo = p.PaidTo,
                Amount = p.Amount, CurrencyCode = p.Currency.Code,
                ExchangeRate = p.ExchangeRate,
                CashAccountId = p.CashAccountId, CashAccountName = p.CashAccount.Name,
                ExpenseAccountId = p.ExpenseAccountId, ExpenseAccountName = p.ExpenseAccount.Name,
                Description = p.Description, ReferenceNumber = p.ReferenceNumber,
                Notes = p.Notes, PreparedBy = p.PreparedBy, ApprovedBy = p.ApprovedBy,
                JournalEntryId = p.JournalEntryId, Status = p.Status,
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
    }
}

// ── Create (Draft) ────────────────────────────────────────────────────────────

public class CreateCashPaymentCommand : IRequest<Guid>
{
    public DateOnly PaymentDate     { get; set; }
    public string?  PaidTo          { get; set; }
    public decimal  Amount          { get; set; }
    public Guid     CurrencyId      { get; set; }
    public decimal  ExchangeRate    { get; set; } = 1m;
    public Guid     CashAccountId   { get; set; }
    public Guid     ExpenseAccountId { get; set; }
    public string?  Description     { get; set; }
    public string?  ReferenceNumber { get; set; }
    public string?  Notes           { get; set; }
    public string?  PreparedBy      { get; set; }
    public string?  ApprovedBy      { get; set; }
}

public class CreateCashPaymentCommandValidator : AbstractValidator<CreateCashPaymentCommand>
{
    public CreateCashPaymentCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.CashAccountId).NotEmpty();
        RuleFor(x => x.ExpenseAccountId).NotEmpty()
            .NotEqual(x => x.CashAccountId).WithMessage("Expense account must differ from cash account.");
        RuleFor(x => x.PaidTo).MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.PreparedBy).MaximumLength(200);
        RuleFor(x => x.ApprovedBy).MaximumLength(200);
    }
}

public class CreateCashPaymentCommandHandler : IRequestHandler<CreateCashPaymentCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;

    public CreateCashPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<Guid> Handle(CreateCashPaymentCommand req, CancellationToken ct)
    {
        var year   = DateTime.UtcNow.Year;
        var prefix = $"CPMT{year}-";

        var last = await _db.CashPayments
            .IgnoreQueryFilters()
            .Where(p => p.PaymentNumber.StartsWith(prefix))
            .OrderByDescending(p => p.PaymentNumber)
            .Select(p => p.PaymentNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && last.Length > prefix.Length
            && int.TryParse(last[prefix.Length..], out var n))
            seq = n + 1;

        var payment = new CashPayment
        {
            PaymentNumber    = $"{prefix}{seq:D5}",
            PaymentDate      = req.PaymentDate,
            PaidTo           = req.PaidTo?.Trim(),
            Amount           = req.Amount,
            CurrencyId       = req.CurrencyId,
            ExchangeRate     = req.ExchangeRate,
            CashAccountId    = req.CashAccountId,
            ExpenseAccountId = req.ExpenseAccountId,
            Description      = req.Description?.Trim(),
            ReferenceNumber  = req.ReferenceNumber?.Trim(),
            Notes            = req.Notes?.Trim(),
            PreparedBy       = req.PreparedBy?.Trim(),
            ApprovedBy       = req.ApprovedBy?.Trim(),
            Status           = CashTransactionStatus.Draft,
            CreatedBy        = _user.UserId
        };

        _db.CashPayments.Add(payment);
        await _db.SaveChangesAsync(ct);
        return payment.Id;
    }
}

// ── Post ──────────────────────────────────────────────────────────────────────

public class PostCashPaymentCommand : IRequest
{
    public Guid Id { get; set; }
}

public class PostCashPaymentCommandHandler : IRequestHandler<PostCashPaymentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;
    private readonly IAccountingService    _accounting;

    public PostCashPaymentCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService   user,
        IAccountingService    accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task Handle(PostCashPaymentCommand req, CancellationToken ct)
    {
        var payment = await _db.CashPayments
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new NotFoundException(nameof(CashPayment), req.Id);

        if (payment.Status != CashTransactionStatus.Draft)
            throw new InvalidOperationException(
                $"Only Draft payments can be posted. Current status: {payment.Status}.");

        // Dr ExpenseAccount / Cr CashAccount
        var jeResult = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            EntryDate       = payment.PaymentDate,
            Description     = $"Cash Payment: {payment.PaymentNumber} - {payment.Description}".TrimEnd(' ', '-'),
            ReferenceType   = ReferenceType.Payment,
            ReferenceId     = payment.Id,
            ReferenceNumber = payment.PaymentNumber,
            CurrencyId      = payment.CurrencyId,
            ExchangeRate    = payment.ExchangeRate,
            PostImmediately = true,
            Lines = new List<JournalEntryLineRequest>
            {
                new()
                {
                    AccountId   = payment.ExpenseAccountId,
                    Debit       = payment.Amount,
                    Credit      = 0m,
                    Description = payment.Description ?? payment.PaymentNumber,
                    SortOrder   = 1
                },
                new()
                {
                    AccountId   = payment.CashAccountId,
                    Debit       = 0m,
                    Credit      = payment.Amount,
                    Description = $"Cash out: {payment.PaidTo ?? payment.PaymentNumber}",
                    SortOrder   = 2
                }
            },
            CreatedBy = _user.UserId
        }, ct);

        if (!jeResult.Succeeded)
            throw new InvalidOperationException(
                $"Cash payment posting failed: {string.Join(", ", jeResult.Errors)}");

        payment.Status         = CashTransactionStatus.Posted;
        payment.JournalEntryId = jeResult.EntryId;
        payment.ModifiedAt     = DateTime.UtcNow;
        payment.ModifiedBy     = _user.UserId;

        await _db.SaveChangesAsync(ct);
    }
}

// ── Void ──────────────────────────────────────────────────────────────────────

public class VoidCashPaymentCommand : IRequest
{
    public Guid   Id     { get; set; }
    public string Reason { get; set; } = "Voided";
}

public class VoidCashPaymentCommandHandler : IRequestHandler<VoidCashPaymentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;
    private readonly IAccountingService    _accounting;

    public VoidCashPaymentCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService   user,
        IAccountingService    accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task Handle(VoidCashPaymentCommand req, CancellationToken ct)
    {
        var payment = await _db.CashPayments
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new NotFoundException(nameof(CashPayment), req.Id);

        if (payment.Status != CashTransactionStatus.Posted)
            throw new InvalidOperationException(
                $"Only Posted payments can be voided. Current status: {payment.Status}.");

        if (payment.JournalEntryId.HasValue)
        {
            var reverseResult = await _accounting.ReverseJournalEntryAsync(
                payment.JournalEntryId.Value,
                req.Reason,
                DateOnly.FromDateTime(DateTime.UtcNow),
                ct);

            if (!reverseResult.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to reverse journal entry: {string.Join(", ", reverseResult.Errors)}");
        }

        payment.Status     = CashTransactionStatus.Voided;
        payment.ModifiedAt = DateTime.UtcNow;
        payment.ModifiedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
    }
}

// ── Delete (Draft only) ───────────────────────────────────────────────────────

public class DeleteCashPaymentCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteCashPaymentCommandHandler : IRequestHandler<DeleteCashPaymentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;

    public DeleteCashPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task Handle(DeleteCashPaymentCommand req, CancellationToken ct)
    {
        var payment = await _db.CashPayments
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new NotFoundException(nameof(CashPayment), req.Id);

        if (payment.Status != CashTransactionStatus.Draft)
            throw new InvalidOperationException(
                "Only Draft payments can be deleted.");

        payment.IsDeleted  = true;
        payment.ModifiedAt = DateTime.UtcNow;
        payment.ModifiedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
    }
}

// ── Update (Draft only) ───────────────────────────────────────────────────────

public class UpdateCashPaymentCommand : IRequest
{
    public Guid     Id               { get; set; }
    public DateOnly PaymentDate      { get; set; }
    public string?  PaidTo           { get; set; }
    public decimal  Amount           { get; set; }
    public Guid     CurrencyId       { get; set; }
    public decimal  ExchangeRate     { get; set; } = 1m;
    public Guid     CashAccountId    { get; set; }
    public Guid     ExpenseAccountId { get; set; }
    public string?  Description      { get; set; }
    public string?  ReferenceNumber  { get; set; }
    public string?  Notes            { get; set; }
    public string?  PreparedBy       { get; set; }
    public string?  ApprovedBy       { get; set; }
}

public class UpdateCashPaymentCommandValidator : AbstractValidator<UpdateCashPaymentCommand>
{
    public UpdateCashPaymentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.CashAccountId).NotEmpty();
        RuleFor(x => x.ExpenseAccountId).NotEmpty()
            .NotEqual(x => x.CashAccountId).WithMessage("Expense account must differ from cash account.");
        RuleFor(x => x.PaidTo).MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.PreparedBy).MaximumLength(200);
        RuleFor(x => x.ApprovedBy).MaximumLength(200);
    }
}

public class UpdateCashPaymentCommandHandler : IRequestHandler<UpdateCashPaymentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;

    public UpdateCashPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task Handle(UpdateCashPaymentCommand req, CancellationToken ct)
    {
        var payment = await _db.CashPayments
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new NotFoundException(nameof(CashPayment), req.Id);

        if (payment.Status != CashTransactionStatus.Draft)
            throw new InvalidOperationException(
                "Only Draft payments can be edited.");

        payment.PaymentDate      = req.PaymentDate;
        payment.PaidTo           = req.PaidTo?.Trim();
        payment.Amount           = req.Amount;
        payment.CurrencyId       = req.CurrencyId;
        payment.ExchangeRate     = req.ExchangeRate;
        payment.CashAccountId    = req.CashAccountId;
        payment.ExpenseAccountId = req.ExpenseAccountId;
        payment.Description      = req.Description?.Trim();
        payment.ReferenceNumber  = req.ReferenceNumber?.Trim();
        payment.Notes            = req.Notes?.Trim();
        payment.PreparedBy       = req.PreparedBy?.Trim();
        payment.ApprovedBy       = req.ApprovedBy?.Trim();
        payment.ModifiedAt       = DateTime.UtcNow;
        payment.ModifiedBy       = _user.UserId;

        await _db.SaveChangesAsync(ct);
    }
}
