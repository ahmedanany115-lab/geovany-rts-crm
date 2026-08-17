using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Entities.Operational;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Operational.Payments;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class CustomerPaymentDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName => PaymentMethod.ToString();
    public PaymentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? BankAccountName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SupplierPaymentDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName => PaymentMethod.ToString();
    public PaymentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? BankAccountName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChequeDto
{
    public Guid Id { get; set; }
    public string ChequeNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly ReceivedDate { get; set; }
    public ChequeStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BankTransactionDto
{
    public Guid Id { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string BankAccountName { get; set; } = string.Empty;
    public BankTransactionType TransactionType { get; set; }
    public string TransactionTypeName => TransactionType.ToString();
    public DateOnly TransactionDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public string? DestinationBankAccountName { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Customer Payment Queries ──────────────────────────────────────────────────

public class GetCustomerPaymentsQuery : IRequest<List<CustomerPaymentDto>>
{
    public Guid? CustomerId { get; set; }
    public PaymentStatus? Status { get; set; }
}

public class GetCustomerPaymentsQueryHandler : IRequestHandler<GetCustomerPaymentsQuery, List<CustomerPaymentDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCustomerPaymentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CustomerPaymentDto>> Handle(GetCustomerPaymentsQuery req, CancellationToken ct)
    {
        var q = _db.CustomerPayments
            .Include(p => p.Customer).Include(p => p.Currency).Include(p => p.BankAccount)
            .Where(p => !p.IsDeleted);
        if (req.CustomerId.HasValue) q = q.Where(p => p.CustomerId == req.CustomerId.Value);
        if (req.Status.HasValue) q = q.Where(p => p.Status == req.Status.Value);
        return await q.OrderByDescending(p => p.PaymentDate)
            .Select(p => new CustomerPaymentDto
            {
                Id = p.Id, PaymentNumber = p.PaymentNumber, CustomerId = p.CustomerId,
                CustomerName = p.Customer.Name, PaymentDate = p.PaymentDate,
                CurrencyCode = p.Currency.Code, Amount = p.Amount,
                PaymentMethod = p.PaymentMethod, Status = p.Status,
                BankAccountName = p.BankAccount != null ? p.BankAccount.Name : null,
                Notes = p.Notes, CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);
    }
}

// ── Create Customer Payment (Bank) ────────────────────────────────────────────

public class CreateCustomerPaymentCommand : IRequest<Guid>
{
    public Guid CustomerId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Bank;
    public Guid? BankAccountId { get; set; }        // for bank payment
    public Guid? ChequeId { get; set; }             // for cheque payment
    public string? Notes { get; set; }
    public List<Guid> InvoiceIds { get; set; } = [];  // invoices to allocate against
}

public class CreateCustomerPaymentCommandValidator : AbstractValidator<CreateCustomerPaymentCommand>
{
    public CreateCustomerPaymentCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.BankAccountId).NotEmpty().When(x => x.PaymentMethod == PaymentMethod.Bank);
        RuleFor(x => x.ChequeId).NotEmpty().When(x => x.PaymentMethod == PaymentMethod.Cheque);
    }
}

public class CreateCustomerPaymentCommandHandler : IRequestHandler<CreateCustomerPaymentCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IAccountingService _accounting;

    public CreateCustomerPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAccountingService accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task<Guid> Handle(CreateCustomerPaymentCommand req, CancellationToken ct)
    {
        var customer = await _db.BusinessPartners.FirstOrDefaultAsync(b => b.Id == req.CustomerId && !b.IsDeleted, ct)
            ?? throw new NotFoundException("Customer", req.CustomerId);

        var receivableAccountId = customer.ReceivableAccountId
            ?? throw new InvalidOperationException("Customer has no Receivable Account.");

        // Determine bank/credit account
        Guid creditAccountId;
        if (req.PaymentMethod == PaymentMethod.Bank)
        {
            var bankAcc = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == req.BankAccountId!.Value && !b.IsDeleted, ct)
                ?? throw new NotFoundException(nameof(BankAccount), req.BankAccountId!.Value);
            creditAccountId = bankAcc.GlAccountId;
        }
        else if (req.PaymentMethod == PaymentMethod.Cheque)
        {
            // Debit: Cheques Receivable (1303), Credit: Customer AR when cheque is received
            // But here the payment method is cheque-to-invoice — meaning a cheque already received
            var undepositedChequesAcc = await _db.Accounts
                .FirstOrDefaultAsync(a => a.Code == "1303" || a.Name.Contains("Undeposited"), ct)
                ?? await _db.Accounts.FirstOrDefaultAsync(a => a.Code == "1301" && !a.IsDeleted, ct)
                ?? throw new InvalidOperationException("No Cheques Receivable account configured.");
            creditAccountId = undepositedChequesAcc.Id;
        }
        else
        {
            throw new InvalidOperationException($"Unsupported payment method: {req.PaymentMethod}");
        }

        var year = DateTime.UtcNow.Year;
        var prefix = $"RCP{year}-";
        var last = await _db.CustomerPayments.IgnoreQueryFilters()
            .Where(p => p.PaymentNumber.StartsWith(prefix)).OrderByDescending(p => p.PaymentNumber)
            .Select(p => p.PaymentNumber).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;

        var baseAmount = req.Amount * req.ExchangeRate;

        // Journal Entry: Dr Bank / Cr Customer AR
        var jeResult = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            EntryDate = req.PaymentDate,
            Description = $"Customer Payment - {customer.Name}",
            ReferenceType = ReferenceType.Receipt,
            CurrencyId = req.CurrencyId, ExchangeRate = req.ExchangeRate,
            PostImmediately = true,
            Lines = new List<JournalEntryLineRequest>
            {
                new() { AccountId = creditAccountId, Debit = req.Amount, Credit = 0,
                    Description = "Customer receipt", SortOrder = 1 },
                new() { AccountId = receivableAccountId, Debit = 0, Credit = req.Amount,
                    Description = $"Customer AR: {customer.Name}", SortOrder = 2 }
            },
            CreatedBy = _user.UserId
        }, ct);

        if (!jeResult.Succeeded)
            throw new InvalidOperationException($"Payment accounting failed: {string.Join(", ", jeResult.Errors)}");

        var payment = new CustomerPayment
        {
            PaymentNumber = $"{prefix}{seq:D5}",
            CustomerId = req.CustomerId, PaymentDate = req.PaymentDate,
            CurrencyId = req.CurrencyId, ExchangeRate = req.ExchangeRate,
            Amount = req.Amount, AmountBase = baseAmount,
            PaymentMethod = req.PaymentMethod, Status = PaymentStatus.Posted,
            BankAccountId = req.BankAccountId, ChequeId = req.ChequeId,
            Notes = req.Notes?.Trim(), JournalEntryId = jeResult.EntryId,
            CreatedBy = _user.UserId
        };

        // Allocate to invoices
        if (req.InvoiceIds.Any())
        {
            var invoices = await _db.CustomerInvoices
                .Where(i => req.InvoiceIds.Contains(i.Id) && i.CustomerId == req.CustomerId && !i.IsDeleted)
                .ToListAsync(ct);

            var remaining = req.Amount;
            foreach (var inv in invoices.OrderBy(i => i.InvoiceDate))
            {
                if (remaining <= 0) break;
                var apply = Math.Min(remaining, inv.TotalAmount - inv.PaidAmount);
                inv.PaidAmount += apply;
                inv.Status = inv.PaidAmount >= inv.TotalAmount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
                inv.ModifiedAt = DateTime.UtcNow;
                remaining -= apply;
                payment.Invoices.Add(inv);
            }
        }

        // Update bank balance
        if (req.BankAccountId.HasValue)
        {
            var bankAcc = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == req.BankAccountId.Value, ct);
            if (bankAcc is not null) { bankAcc.CurrentBalance += req.Amount; bankAcc.ModifiedAt = DateTime.UtcNow; }
        }

        _db.CustomerPayments.Add(payment);
        await _db.SaveChangesAsync(ct);
        return payment.Id;
    }
}

// ── Create Supplier Payment ───────────────────────────────────────────────────

public class CreateSupplierPaymentCommand : IRequest<Guid>
{
    public Guid SupplierId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal Amount { get; set; }
    public Guid BankAccountId { get; set; }
    public string? Notes { get; set; }
    public List<Guid> InvoiceIds { get; set; } = [];
}

public class CreateSupplierPaymentCommandValidator : AbstractValidator<CreateSupplierPaymentCommand>
{
    public CreateSupplierPaymentCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.BankAccountId).NotEmpty();
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
    }
}

public class CreateSupplierPaymentCommandHandler : IRequestHandler<CreateSupplierPaymentCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IAccountingService _accounting;
    public CreateSupplierPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAccountingService accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task<Guid> Handle(CreateSupplierPaymentCommand req, CancellationToken ct)
    {
        var supplier = await _db.BusinessPartners.FirstOrDefaultAsync(b => b.Id == req.SupplierId && !b.IsDeleted, ct)
            ?? throw new NotFoundException("Supplier", req.SupplierId);
        var payableAccountId = supplier.PayableAccountId
            ?? throw new InvalidOperationException("Supplier has no Payable Account.");

        var bankAcc = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == req.BankAccountId && !b.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(BankAccount), req.BankAccountId);

        var year = DateTime.UtcNow.Year;
        var prefix = $"PMT{year}-";
        var last = await _db.SupplierPayments.IgnoreQueryFilters()
            .Where(p => p.PaymentNumber.StartsWith(prefix)).OrderByDescending(p => p.PaymentNumber)
            .Select(p => p.PaymentNumber).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;

        // Dr: Supplier Payable / Cr: Bank
        var jeResult = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            EntryDate = req.PaymentDate,
            Description = $"Supplier Payment - {supplier.Name}",
            ReferenceType = ReferenceType.Payment,
            CurrencyId = req.CurrencyId, ExchangeRate = req.ExchangeRate,
            PostImmediately = true,
            Lines = new List<JournalEntryLineRequest>
            {
                new() { AccountId = payableAccountId, Debit = req.Amount, Credit = 0,
                    Description = $"Supplier AP: {supplier.Name}", SortOrder = 1 },
                new() { AccountId = bankAcc.GlAccountId, Debit = 0, Credit = req.Amount,
                    Description = $"Bank: {bankAcc.Name}", SortOrder = 2 }
            },
            CreatedBy = _user.UserId
        }, ct);

        if (!jeResult.Succeeded)
            throw new InvalidOperationException($"Payment accounting failed: {string.Join(", ", jeResult.Errors)}");

        var payment = new SupplierPayment
        {
            PaymentNumber = $"{prefix}{seq:D5}",
            SupplierId = req.SupplierId, PaymentDate = req.PaymentDate,
            CurrencyId = req.CurrencyId, ExchangeRate = req.ExchangeRate,
            Amount = req.Amount, AmountBase = req.Amount * req.ExchangeRate,
            PaymentMethod = PaymentMethod.Bank, Status = PaymentStatus.Posted,
            BankAccountId = req.BankAccountId, Notes = req.Notes?.Trim(),
            JournalEntryId = jeResult.EntryId, CreatedBy = _user.UserId
        };

        if (req.InvoiceIds.Any())
        {
            var invoices = await _db.SupplierInvoices
                .Where(i => req.InvoiceIds.Contains(i.Id) && i.SupplierId == req.SupplierId && !i.IsDeleted)
                .ToListAsync(ct);
            var remaining = req.Amount;
            foreach (var inv in invoices.OrderBy(i => i.InvoiceDate))
            {
                if (remaining <= 0) break;
                var apply = Math.Min(remaining, inv.TotalAmount - inv.PaidAmount);
                inv.PaidAmount += apply;
                inv.Status = inv.PaidAmount >= inv.TotalAmount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
                inv.ModifiedAt = DateTime.UtcNow;
                remaining -= apply;
                payment.Invoices.Add(inv);
            }
        }

        bankAcc.CurrentBalance -= req.Amount;
        bankAcc.ModifiedAt = DateTime.UtcNow;

        _db.SupplierPayments.Add(payment);
        await _db.SaveChangesAsync(ct);
        return payment.Id;
    }
}

// ── Supplier Payments Query ───────────────────────────────────────────────────

public class GetSupplierPaymentsQuery : IRequest<List<SupplierPaymentDto>>
{
    public Guid? SupplierId { get; set; }
}

public class GetSupplierPaymentsQueryHandler : IRequestHandler<GetSupplierPaymentsQuery, List<SupplierPaymentDto>>
{
    private readonly IApplicationDbContext _db;
    public GetSupplierPaymentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<SupplierPaymentDto>> Handle(GetSupplierPaymentsQuery req, CancellationToken ct)
    {
        var q = _db.SupplierPayments
            .Include(p => p.Supplier).Include(p => p.Currency).Include(p => p.BankAccount)
            .Where(p => !p.IsDeleted);
        if (req.SupplierId.HasValue) q = q.Where(p => p.SupplierId == req.SupplierId.Value);
        return await q.OrderByDescending(p => p.PaymentDate)
            .Select(p => new SupplierPaymentDto
            {
                Id = p.Id, PaymentNumber = p.PaymentNumber, SupplierId = p.SupplierId,
                SupplierName = p.Supplier.Name, PaymentDate = p.PaymentDate,
                CurrencyCode = p.Currency.Code, Amount = p.Amount,
                PaymentMethod = p.PaymentMethod, Status = p.Status,
                BankAccountName = p.BankAccount != null ? p.BankAccount.Name : null,
                Notes = p.Notes, CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);
    }
}

// ── Cheque Handlers ───────────────────────────────────────────────────────────

public class GetChequesQuery : IRequest<List<ChequeDto>>
{
    public ChequeStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
}

public class GetChequesQueryHandler : IRequestHandler<GetChequesQuery, List<ChequeDto>>
{
    private readonly IApplicationDbContext _db;
    public GetChequesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ChequeDto>> Handle(GetChequesQuery req, CancellationToken ct)
    {
        var q = _db.Cheques.Include(c => c.Customer).Include(c => c.Currency).Where(c => !c.IsDeleted);
        if (req.Status.HasValue) q = q.Where(c => c.Status == req.Status.Value);
        if (req.CustomerId.HasValue) q = q.Where(c => c.CustomerId == req.CustomerId.Value);
        return await q.OrderByDescending(c => c.DueDate)
            .Select(c => new ChequeDto
            {
                Id = c.Id, ChequeNumber = c.ChequeNumber, CustomerName = c.Customer.Name,
                BankName = c.BankName, CurrencyCode = c.Currency.Code, Amount = c.Amount,
                IssueDate = c.IssueDate, DueDate = c.DueDate, ReceivedDate = c.ReceivedDate,
                Status = c.Status, Notes = c.Notes, CreatedAt = c.CreatedAt
            }).ToListAsync(ct);
    }
}

public class ReceiveChequeCommand : IRequest<Guid>
{
    public Guid CustomerId { get; set; }
    public string ChequeNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public Guid CurrencyId { get; set; }
    public decimal Amount { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly ReceivedDate { get; set; }
    public string? Notes { get; set; }
}

public class ReceiveChequeCommandValidator : AbstractValidator<ReceiveChequeCommand>
{
    public ReceiveChequeCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ChequeNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.IssueDate);
    }
}

public class ReceiveChequeCommandHandler : IRequestHandler<ReceiveChequeCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IAccountingService _accounting;
    public ReceiveChequeCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAccountingService accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task<Guid> Handle(ReceiveChequeCommand req, CancellationToken ct)
    {
        var customer = await _db.BusinessPartners.FirstOrDefaultAsync(b => b.Id == req.CustomerId && !b.IsDeleted, ct)
            ?? throw new NotFoundException("Customer", req.CustomerId);
        var receivableAccountId = customer.ReceivableAccountId
            ?? throw new InvalidOperationException("Customer has no Receivable Account.");

        // Cheques Receivable account — use code 1303 or create a lookup
        var chequesReceivableAcc = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "1302" && !a.IsDeleted, ct)
            ?? await _db.Accounts.FirstOrDefaultAsync(a => a.Code == "1301" && !a.IsDeleted, ct)
            ?? throw new InvalidOperationException("No Cheques Receivable account (1302) found. Check Chart of Accounts.");

        // Dr: Cheques Receivable / Cr: Customer AR
        var jeResult = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            EntryDate = req.ReceivedDate,
            Description = $"Cheque Received #{req.ChequeNumber} from {customer.Name}",
            ReferenceType = ReferenceType.ChequeReceipt,
            CurrencyId = req.CurrencyId, ExchangeRate = req.ExchangeRate,
            PostImmediately = true,
            Lines = new List<JournalEntryLineRequest>
            {
                new() { AccountId = chequesReceivableAcc.Id, Debit = req.Amount, Credit = 0,
                    Description = $"Cheque #{req.ChequeNumber}", SortOrder = 1 },
                new() { AccountId = receivableAccountId, Debit = 0, Credit = req.Amount,
                    Description = $"Customer AR: {customer.Name}", SortOrder = 2 }
            },
            CreatedBy = _user.UserId
        }, ct);

        if (!jeResult.Succeeded)
            throw new InvalidOperationException($"Cheque accounting failed: {string.Join(", ", jeResult.Errors)}");

        var cheque = new Cheque
        {
            ChequeNumber = req.ChequeNumber, CustomerId = req.CustomerId,
            BankName = req.BankName, CurrencyId = req.CurrencyId,
            Amount = req.Amount, AmountBase = req.Amount * req.ExchangeRate,
            IssueDate = req.IssueDate, DueDate = req.DueDate, ReceivedDate = req.ReceivedDate,
            Status = ChequeStatus.Received, Notes = req.Notes?.Trim(),
            ReceiptJournalEntryId = jeResult.EntryId, CreatedBy = _user.UserId
        };
        _db.Cheques.Add(cheque);
        await _db.SaveChangesAsync(ct);
        return cheque.Id;
    }
}

public class DepositChequeCommand : IRequest
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public DateOnly DepositDate { get; set; }
}

public class DepositChequeCommandHandler : IRequestHandler<DepositChequeCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IAccountingService _accounting;
    public DepositChequeCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAccountingService accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task Handle(DepositChequeCommand req, CancellationToken ct)
    {
        var cheque = await _db.Cheques.FirstOrDefaultAsync(c => c.Id == req.Id && !c.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Cheque), req.Id);
        if (cheque.Status != ChequeStatus.Received)
            throw new InvalidOperationException($"Cheque is {cheque.Status}, cannot deposit.");

        var bankAcc = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == req.BankAccountId && !b.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(BankAccount), req.BankAccountId);

        var chequesReceivableAcc = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "1302" && !a.IsDeleted, ct)
            ?? await _db.Accounts.FirstOrDefaultAsync(a => a.Code == "1301" && !a.IsDeleted, ct)
            ?? throw new InvalidOperationException("Cheques Receivable account not found.");

        // Dr: Bank / Cr: Cheques Receivable
        var jeResult = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            EntryDate = req.DepositDate,
            Description = $"Cheque Deposit #{cheque.ChequeNumber}",
            ReferenceType = ReferenceType.ChequeDeposit,
            CurrencyId = cheque.CurrencyId, ExchangeRate = 1m,
            PostImmediately = true,
            Lines = new List<JournalEntryLineRequest>
            {
                new() { AccountId = bankAcc.GlAccountId, Debit = cheque.Amount, Credit = 0,
                    Description = $"Bank: {bankAcc.Name}", SortOrder = 1 },
                new() { AccountId = chequesReceivableAcc.Id, Debit = 0, Credit = cheque.Amount,
                    Description = $"Cheque #{cheque.ChequeNumber}", SortOrder = 2 }
            },
            CreatedBy = _user.UserId
        }, ct);

        if (!jeResult.Succeeded)
            throw new InvalidOperationException($"Deposit accounting failed: {string.Join(", ", jeResult.Errors)}");

        cheque.Status = ChequeStatus.Deposited;
        cheque.BankAccountId = req.BankAccountId;
        cheque.DepositJournalEntryId = jeResult.EntryId;
        cheque.ModifiedAt = DateTime.UtcNow; cheque.ModifiedBy = _user.UserId;

        bankAcc.CurrentBalance += cheque.Amount;
        bankAcc.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

public class BounceChequeCommand : IRequest
{
    public Guid Id { get; set; }
    public DateOnly BounceDate { get; set; }
}

public class BounceChequeCommandHandler : IRequestHandler<BounceChequeCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IAccountingService _accounting;
    public BounceChequeCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAccountingService accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task Handle(BounceChequeCommand req, CancellationToken ct)
    {
        var cheque = await _db.Cheques
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == req.Id && !c.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Cheque), req.Id);

        if (cheque.Status != ChequeStatus.Deposited)
            throw new InvalidOperationException("Only deposited cheques can bounce.");

        // Reverse deposit: Dr Cheques Receivable / Cr Bank (reverse of deposit)
        // Then restore customer receivable: Dr Customer AR / Cr Cheques Receivable
        // Net effect: Dr Customer AR / Cr Bank
        var bankAcc = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == cheque.BankAccountId!.Value, ct)
            ?? throw new InvalidOperationException("Bank account for cheque not found.");
        var receivableAcc = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == cheque.Customer.ReceivableAccountId, ct)
            ?? throw new InvalidOperationException("Customer receivable account not found.");

        if (cheque.DepositJournalEntryId.HasValue)
        {
            await _accounting.ReverseJournalEntryAsync(
                cheque.DepositJournalEntryId.Value,
                $"Cheque bounce #{cheque.ChequeNumber}",
                req.BounceDate, ct);
        }

        cheque.Status = ChequeStatus.Bounced;
        cheque.ModifiedAt = DateTime.UtcNow; cheque.ModifiedBy = _user.UserId;
        bankAcc.CurrentBalance -= cheque.Amount;
        bankAcc.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

// ── Bank Transactions ─────────────────────────────────────────────────────────

public class GetBankTransactionsQuery : IRequest<List<BankTransactionDto>>
{
    public Guid? BankAccountId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class GetBankTransactionsQueryHandler : IRequestHandler<GetBankTransactionsQuery, List<BankTransactionDto>>
{
    private readonly IApplicationDbContext _db;
    public GetBankTransactionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<BankTransactionDto>> Handle(GetBankTransactionsQuery req, CancellationToken ct)
    {
        var q = _db.BankTransactions
            .Include(t => t.BankAccount).Include(t => t.Currency).Include(t => t.DestinationBankAccount)
            .Where(t => !t.IsDeleted);
        if (req.BankAccountId.HasValue)
            q = q.Where(t => t.BankAccountId == req.BankAccountId.Value || t.DestinationBankAccountId == req.BankAccountId.Value);
        if (req.FromDate.HasValue) q = q.Where(t => t.TransactionDate >= req.FromDate.Value);
        if (req.ToDate.HasValue) q = q.Where(t => t.TransactionDate <= req.ToDate.Value);
        return await q.OrderByDescending(t => t.TransactionDate)
            .Select(t => new BankTransactionDto
            {
                Id = t.Id, TransactionNumber = t.TransactionNumber,
                BankAccountName = t.BankAccount.Name, TransactionType = t.TransactionType,
                TransactionDate = t.TransactionDate, CurrencyCode = t.Currency.Code,
                Amount = t.Amount, Description = t.Description, Reference = t.Reference,
                DestinationBankAccountName = t.DestinationBankAccount != null ? t.DestinationBankAccount.Name : null,
                CreatedAt = t.CreatedAt
            }).ToListAsync(ct);
    }
}

public class CreateBankTransactionCommand : IRequest<Guid>
{
    public Guid BankAccountId { get; set; }
    public BankTransactionType TransactionType { get; set; }
    public DateOnly TransactionDate { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public Guid? DestinationBankAccountId { get; set; }  // for transfers
}

public class CreateBankTransactionCommandValidator : AbstractValidator<CreateBankTransactionCommand>
{
    public CreateBankTransactionCommandValidator()
    {
        RuleFor(x => x.BankAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.DestinationBankAccountId).NotEmpty()
            .When(x => x.TransactionType == BankTransactionType.Transfer)
            .WithMessage("Destination bank account is required for transfers.");
    }
}

public class CreateBankTransactionCommandHandler : IRequestHandler<CreateBankTransactionCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IAccountingService _accounting;
    public CreateBankTransactionCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAccountingService accounting)
        => (_db, _user, _accounting) = (db, user, accounting);

    public async Task<Guid> Handle(CreateBankTransactionCommand req, CancellationToken ct)
    {
        var bankAcc = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == req.BankAccountId && !b.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(BankAccount), req.BankAccountId);

        var year = DateTime.UtcNow.Year;
        var prefix = $"BTX{year}-";
        var last = await _db.BankTransactions.IgnoreQueryFilters()
            .Where(t => t.TransactionNumber.StartsWith(prefix)).OrderByDescending(t => t.TransactionNumber)
            .Select(t => t.TransactionNumber).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;

        var jeLines = new List<JournalEntryLineRequest>();

        if (req.TransactionType == BankTransactionType.Deposit)
        {
            // Dr Bank / Cr ? (manual — no counterpart required for cash deposits)
            // For now use a suspense/cash account — caller should specify
            jeLines.Add(new() { AccountId = bankAcc.GlAccountId, Debit = req.Amount, Credit = 0, Description = "Bank Deposit", SortOrder = 1 });
            // Find cash/suspense account
            var cashAcc = await _db.Accounts.FirstOrDefaultAsync(a => a.Code == "1101" && !a.IsDeleted, ct);
            if (cashAcc is not null)
                jeLines.Add(new() { AccountId = cashAcc.Id, Debit = 0, Credit = req.Amount, Description = "Cash/Source", SortOrder = 2 });
            bankAcc.CurrentBalance += req.Amount;
        }
        else if (req.TransactionType == BankTransactionType.Withdrawal)
        {
            var cashAcc = await _db.Accounts.FirstOrDefaultAsync(a => a.Code == "1101" && !a.IsDeleted, ct);
            if (cashAcc is not null)
                jeLines.Add(new() { AccountId = cashAcc.Id, Debit = req.Amount, Credit = 0, Description = "Cash/Destination", SortOrder = 1 });
            jeLines.Add(new() { AccountId = bankAcc.GlAccountId, Debit = 0, Credit = req.Amount, Description = "Bank Withdrawal", SortOrder = 2 });
            bankAcc.CurrentBalance -= req.Amount;
        }
        else if (req.TransactionType == BankTransactionType.Transfer)
        {
            var destAcc = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == req.DestinationBankAccountId!.Value && !b.IsDeleted, ct)
                ?? throw new NotFoundException("Destination BankAccount", req.DestinationBankAccountId!.Value);
            // Dr Destination Bank / Cr Source Bank
            jeLines.Add(new() { AccountId = destAcc.GlAccountId, Debit = req.Amount, Credit = 0, Description = $"Transfer to {destAcc.Name}", SortOrder = 1 });
            jeLines.Add(new() { AccountId = bankAcc.GlAccountId, Debit = 0, Credit = req.Amount, Description = $"Transfer from {bankAcc.Name}", SortOrder = 2 });
            bankAcc.CurrentBalance -= req.Amount;
            destAcc.CurrentBalance += req.Amount;
            destAcc.ModifiedAt = DateTime.UtcNow;
        }

        Guid? jeId = null;
        if (jeLines.Count >= 2)
        {
            var jeResult = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
            {
                EntryDate = req.TransactionDate,
                Description = req.Description ?? $"Bank Transaction - {req.TransactionType}",
                ReferenceType = ReferenceType.BankTransfer,
                CurrencyId = req.CurrencyId, ExchangeRate = req.ExchangeRate,
                PostImmediately = true, Lines = jeLines, CreatedBy = _user.UserId
            }, ct);
            if (!jeResult.Succeeded)
                throw new InvalidOperationException($"Bank transaction accounting failed: {string.Join(", ", jeResult.Errors)}");
            jeId = jeResult.EntryId;
        }

        bankAcc.ModifiedAt = DateTime.UtcNow;

        var txn = new BankTransaction
        {
            TransactionNumber = $"{prefix}{seq:D5}",
            BankAccountId = req.BankAccountId, TransactionType = req.TransactionType,
            TransactionDate = req.TransactionDate, CurrencyId = req.CurrencyId,
            ExchangeRate = req.ExchangeRate, Amount = req.Amount, AmountBase = req.Amount * req.ExchangeRate,
            Description = req.Description?.Trim(), Reference = req.Reference?.Trim(),
            DestinationBankAccountId = req.DestinationBankAccountId,
            JournalEntryId = jeId, CreatedBy = _user.UserId
        };
        _db.BankTransactions.Add(txn);
        await _db.SaveChangesAsync(ct);
        return txn.Id;
    }
}

// ── Sales Commission Queries ──────────────────────────────────────────────────

public class SalesCommissionDto
{
    public Guid Id { get; set; }
    public string? SalespersonName { get; set; }
    public decimal Rate { get; set; }
    public decimal BaseSalesAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public CommissionStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetSalesCommissionsQuery : IRequest<List<SalesCommissionDto>>
{
    public Guid? SalespersonId { get; set; }
    public CommissionStatus? Status { get; set; }
}

public class GetSalesCommissionsQueryHandler : IRequestHandler<GetSalesCommissionsQuery, List<SalesCommissionDto>>
{
    private readonly IApplicationDbContext _db;
    public GetSalesCommissionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<SalesCommissionDto>> Handle(GetSalesCommissionsQuery req, CancellationToken ct)
    {
        var q = _db.SalesCommissions
            .Include(c => c.Currency)
            .Include(c => c.CustomerInvoice)
            .Where(c => !c.IsDeleted);
        if (req.SalespersonId.HasValue) q = q.Where(c => c.SalespersonId == req.SalespersonId.Value);
        if (req.Status.HasValue) q = q.Where(c => c.Status == req.Status.Value);
        return await q.OrderByDescending(c => c.CreatedAt)
            .Select(c => new SalesCommissionDto
            {
                Id = c.Id, SalespersonName = c.SalespersonName, Rate = c.Rate,
                BaseSalesAmount = c.BaseSalesAmount, CommissionAmount = c.CommissionAmount,
                CurrencyCode = c.Currency.Code, Status = c.Status,
                InvoiceNumber = c.CustomerInvoice != null ? c.CustomerInvoice.InvoiceNumber : null,
                CreatedAt = c.CreatedAt
            }).ToListAsync(ct);
    }
}

// ── Bank Account Queries ──────────────────────────────────────────────────────

public class BankAccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IBAN { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; }
}

public class GetBankAccountsQuery : IRequest<List<BankAccountDto>>
{
    public bool? IsActive { get; set; }
}

public class GetBankAccountsQueryHandler : IRequestHandler<GetBankAccountsQuery, List<BankAccountDto>>
{
    private readonly IApplicationDbContext _db;
    public GetBankAccountsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<BankAccountDto>> Handle(GetBankAccountsQuery req, CancellationToken ct)
    {
        var q = _db.BankAccounts.Include(b => b.Currency).Where(b => !b.IsDeleted);
        if (req.IsActive.HasValue) q = q.Where(b => b.IsActive == req.IsActive.Value);
        return await q.OrderBy(b => b.Name)
            .Select(b => new BankAccountDto
            {
                Id = b.Id, Code = b.Code, Name = b.Name, BankName = b.BankName,
                AccountNumber = b.AccountNumber, IBAN = b.IBAN,
                CurrencyCode = b.Currency.Code,
                OpeningBalance = b.OpeningBalance, CurrentBalance = b.CurrentBalance,
                IsActive = b.IsActive
            }).ToListAsync(ct);
    }
}

public class UpsertBankAccountCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public BankAccountType AccountType { get; set; } = BankAccountType.Bank;
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IBAN { get; set; }
    public Guid GlAccountId { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal OpeningBalance { get; set; }
}

public class UpsertBankAccountCommandHandler : IRequestHandler<UpsertBankAccountCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public UpsertBankAccountCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<Guid> Handle(UpsertBankAccountCommand req, CancellationToken ct)
    {
        BankAccount? acc = null;
        if (req.Id.HasValue)
            acc = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == req.Id.Value && !b.IsDeleted, ct);
        if (acc is null)
        {
            acc = new BankAccount
            {
                Code = req.Code, AccountType = req.AccountType,
                OpeningBalance = req.OpeningBalance, CurrentBalance = req.OpeningBalance,
                IsActive = true, CreatedBy = _user.UserId
            };
            _db.BankAccounts.Add(acc);
        }
        else { acc.ModifiedAt = DateTime.UtcNow; acc.ModifiedBy = _user.UserId; }
        acc.Name = req.Name.Trim(); acc.BankName = req.BankName?.Trim();
        acc.AccountNumber = req.AccountNumber?.Trim(); acc.IBAN = req.IBAN?.Trim();
        acc.GlAccountId = req.GlAccountId; acc.CurrencyId = req.CurrencyId;
        await _db.SaveChangesAsync(ct);
        return acc.Id;
    }
}
