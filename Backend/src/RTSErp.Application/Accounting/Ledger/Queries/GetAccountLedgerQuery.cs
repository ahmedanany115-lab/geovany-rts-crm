using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;

namespace RTSErp.Application.Accounting.Ledger.Queries;

public class LedgerLineDto
{
    public Guid JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? LineDescription { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

public class AccountLedgerDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<LedgerLineDto> Lines { get; set; } = [];
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class GetAccountLedgerQuery : IRequest<AccountLedgerDto>
{
    public Guid AccountId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class GetAccountLedgerQueryHandler : IRequestHandler<GetAccountLedgerQuery, AccountLedgerDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IAccountingService _accounting;

    public GetAccountLedgerQueryHandler(IApplicationDbContext db, IAccountingService accounting)
    {
        _db = db;
        _accounting = accounting;
    }

    public async Task<AccountLedgerDto> Handle(GetAccountLedgerQuery request, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && !a.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Accounting.Account), request.AccountId);

        // Opening balance = everything before FromDate
        decimal openingDebit = 0, openingCredit = 0;
        if (request.FromDate.HasValue)
        {
            var openingLines = await _db.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == request.AccountId
                    && l.JournalEntry.Status == Domain.Enums.JournalEntryStatus.Posted
                    && l.JournalEntry.EntryDate < request.FromDate.Value
                    && !l.IsDeleted && !l.JournalEntry.IsDeleted)
                .ToListAsync(cancellationToken);

            openingDebit = openingLines.Sum(l => l.DebitBase);
            openingCredit = openingLines.Sum(l => l.CreditBase);
        }

        // Period lines
        var periodQuery = _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == request.AccountId
                && l.JournalEntry.Status == Domain.Enums.JournalEntryStatus.Posted
                && !l.IsDeleted && !l.JournalEntry.IsDeleted);

        if (request.FromDate.HasValue)
            periodQuery = periodQuery.Where(l => l.JournalEntry.EntryDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            periodQuery = periodQuery.Where(l => l.JournalEntry.EntryDate <= request.ToDate.Value);

        var periodLines = await periodQuery
            .OrderBy(l => l.JournalEntry.EntryDate)
            .ThenBy(l => l.JournalEntry.EntryNumber)
            .Select(l => new
            {
                l.JournalEntryId,
                l.JournalEntry.EntryNumber,
                l.JournalEntry.EntryDate,
                EntryDescription = l.JournalEntry.Description,
                l.Description,
                l.DebitBase,
                l.CreditBase
            })
            .ToListAsync(cancellationToken);

        var openingBalance = openingDebit - openingCredit;
        var runningBalance = openingBalance;
        var ledgerLines = new List<LedgerLineDto>();

        foreach (var line in periodLines)
        {
            runningBalance += line.DebitBase - line.CreditBase;
            ledgerLines.Add(new LedgerLineDto
            {
                JournalEntryId = line.JournalEntryId,
                EntryNumber = line.EntryNumber,
                EntryDate = line.EntryDate,
                Description = line.EntryDescription,
                LineDescription = line.Description,
                Debit = line.DebitBase,
                Credit = line.CreditBase,
                RunningBalance = runningBalance
            });
        }

        return new AccountLedgerDto
        {
            AccountId = account.Id,
            AccountCode = account.Code,
            AccountName = account.Name,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            OpeningBalance = openingBalance,
            Lines = ledgerLines,
            TotalDebit = ledgerLines.Sum(l => l.Debit),
            TotalCredit = ledgerLines.Sum(l => l.Credit),
            ClosingBalance = runningBalance
        };
    }
}

// ─── Account Balance query ────────────────────────────────────────────────────

public class GetAccountBalanceQuery : IRequest<AccountBalanceResult>
{
    public Guid AccountId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, AccountBalanceResult>
{
    private readonly IAccountingService _accounting;

    public GetAccountBalanceQueryHandler(IAccountingService accounting) => _accounting = accounting;

    public Task<AccountBalanceResult> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
        => _accounting.GetAccountBalanceAsync(request.AccountId, request.FromDate, request.ToDate, cancellationToken);
}
