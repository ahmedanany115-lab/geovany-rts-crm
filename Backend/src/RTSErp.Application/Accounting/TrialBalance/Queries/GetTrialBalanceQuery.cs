using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.TrialBalance.Queries;

public class TrialBalanceLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public string AccountTypeName => AccountType.ToString();
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }
}

public class TrialBalanceDto
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public List<TrialBalanceLineDto> Lines { get; set; } = [];
    public decimal TotalOpeningDebit => Lines.Sum(l => l.OpeningDebit);
    public decimal TotalOpeningCredit => Lines.Sum(l => l.OpeningCredit);
    public decimal TotalPeriodDebit => Lines.Sum(l => l.PeriodDebit);
    public decimal TotalPeriodCredit => Lines.Sum(l => l.PeriodCredit);
    public decimal TotalClosingDebit => Lines.Sum(l => l.ClosingDebit);
    public decimal TotalClosingCredit => Lines.Sum(l => l.ClosingCredit);
    public bool IsBalanced => Math.Abs(TotalClosingDebit - TotalClosingCredit) < 0.001m;
}

public class GetTrialBalanceQuery : IRequest<TrialBalanceDto>
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class GetTrialBalanceQueryHandler : IRequestHandler<GetTrialBalanceQuery, TrialBalanceDto>
{
    private readonly IApplicationDbContext _db;

    public GetTrialBalanceQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<TrialBalanceDto> Handle(GetTrialBalanceQuery request, CancellationToken cancellationToken)
    {
        // Get all posting accounts
        var accounts = await _db.Accounts
            .Where(a => !a.IsGroup && a.IsActive && !a.IsDeleted)
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);

        // Opening balance lines (before FromDate)
        var openingLines = new List<(Guid AccountId, decimal Debit, decimal Credit)>();
        if (request.FromDate.HasValue)
        {
            var opening = await _db.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                    && l.JournalEntry.EntryDate < request.FromDate.Value
                    && !l.IsDeleted && !l.JournalEntry.IsDeleted)
                .GroupBy(l => l.AccountId)
                .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitBase), Credit = g.Sum(x => x.CreditBase) })
                .ToListAsync(cancellationToken);
            openingLines = opening.Select(x => (x.AccountId, x.Debit, x.Credit)).ToList();
        }

        // Period lines
        var periodQuery = _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                && !l.IsDeleted && !l.JournalEntry.IsDeleted);

        if (request.FromDate.HasValue)
            periodQuery = periodQuery.Where(l => l.JournalEntry.EntryDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            periodQuery = periodQuery.Where(l => l.JournalEntry.EntryDate <= request.ToDate.Value);

        var periodGrouped = await periodQuery
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitBase), Credit = g.Sum(x => x.CreditBase) })
            .ToListAsync(cancellationToken);

        var openingMap = openingLines.ToDictionary(x => x.AccountId, x => (x.Debit, x.Credit));
        var periodMap = periodGrouped.ToDictionary(x => x.AccountId, x => (x.Debit, x.Credit));

        // Only include accounts with activity
        var activeAccountIds = openingMap.Keys.Union(periodMap.Keys).ToHashSet();
        var lines = accounts
            .Where(a => activeAccountIds.Contains(a.Id))
            .Select(a =>
            {
                openingMap.TryGetValue(a.Id, out var op);
                periodMap.TryGetValue(a.Id, out var per);

                var closingDebit = op.Debit + per.Debit;
                var closingCredit = op.Credit + per.Credit;

                // Normalize: only show net on appropriate side
                decimal cd, cc;
                if (closingDebit >= closingCredit)
                {
                    cd = closingDebit - closingCredit;
                    cc = 0;
                }
                else
                {
                    cd = 0;
                    cc = closingCredit - closingDebit;
                }

                return new TrialBalanceLineDto
                {
                    AccountId = a.Id,
                    AccountCode = a.Code,
                    AccountName = a.Name,
                    AccountType = a.AccountType,
                    OpeningDebit = op.Debit,
                    OpeningCredit = op.Credit,
                    PeriodDebit = per.Debit,
                    PeriodCredit = per.Credit,
                    ClosingDebit = cd,
                    ClosingCredit = cc
                };
            })
            .ToList();

        return new TrialBalanceDto
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Lines = lines
        };
    }
}
