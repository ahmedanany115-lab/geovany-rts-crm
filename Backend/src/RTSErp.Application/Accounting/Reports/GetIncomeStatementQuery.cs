using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.Reports;

// ── DTO ───────────────────────────────────────────────────────────────────────

public class IncomeStatementLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? AccountNameAr { get; set; }
    public decimal Amount { get; set; }   // positive = revenue, negative = expense
}

public class IncomeStatementDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }

    public List<IncomeStatementLineDto> RevenueLines      { get; set; } = [];
    public List<IncomeStatementLineDto> CostOfSalesLines  { get; set; } = [];
    public List<IncomeStatementLineDto> ExpenseLines      { get; set; } = [];

    public decimal TotalRevenue     => RevenueLines.Sum(l => l.Amount);
    public decimal TotalCOGS        => CostOfSalesLines.Sum(l => l.Amount);
    public decimal GrossProfit      => TotalRevenue - TotalCOGS;
    public decimal TotalExpenses    => ExpenseLines.Sum(l => l.Amount);
    public decimal OperatingProfit  => GrossProfit - TotalExpenses;
    public decimal NetProfit        => OperatingProfit;

    public decimal GrossMarginPct   =>
        TotalRevenue == 0 ? 0 : Math.Round(GrossProfit / TotalRevenue * 100, 2);
    public decimal NetMarginPct     =>
        TotalRevenue == 0 ? 0 : Math.Round(NetProfit / TotalRevenue * 100, 2);
}

// ── Query ─────────────────────────────────────────────────────────────────────

public class GetIncomeStatementQuery : IRequest<IncomeStatementDto>
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

public class GetIncomeStatementQueryHandler
    : IRequestHandler<GetIncomeStatementQuery, IncomeStatementDto>
{
    private readonly IApplicationDbContext _db;

    public GetIncomeStatementQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IncomeStatementDto> Handle(
        GetIncomeStatementQuery request, CancellationToken cancellationToken)
    {
        // Pull all POSTED JE lines in the period, grouped by account
        var lines = await _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Include(l => l.Account)
            .Where(l => !l.IsDeleted
                     && !l.JournalEntry.IsDeleted
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate >= request.FromDate
                     && l.JournalEntry.EntryDate <= request.ToDate)
            .GroupBy(l => new { l.AccountId, l.Account.Code, l.Account.Name, l.Account.NameAr, l.Account.AccountType })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.Code,
                g.Key.Name,
                g.Key.NameAr,
                g.Key.AccountType,
                NetCredit = g.Sum(l => l.CreditBase - l.DebitBase)
            })
            .ToListAsync(cancellationToken);

        var result = new IncomeStatementDto
        {
            FromDate = request.FromDate,
            ToDate   = request.ToDate,
        };

        foreach (var line in lines)
        {
            var dto = new IncomeStatementLineDto
            {
                AccountId    = line.AccountId,
                AccountCode  = line.Code,
                AccountName  = line.Name,
                AccountNameAr = line.NameAr,
                // Revenue: credit-normal accounts; positive when net credit > 0
                // Expense / CoS: debit-normal accounts; positive when net debit > 0
                Amount = line.AccountType switch
                {
                    AccountType.Revenue     => line.NetCredit,       // Cr > Dr = revenue
                    AccountType.CostOfSales => -line.NetCredit,      // Dr > Cr = cost
                    AccountType.Expense     => -line.NetCredit,      // Dr > Cr = expense
                    _ => 0
                }
            };

            if (dto.Amount == 0) continue;

            switch (line.AccountType)
            {
                case AccountType.Revenue:
                    result.RevenueLines.Add(dto);
                    break;
                case AccountType.CostOfSales:
                    result.CostOfSalesLines.Add(dto);
                    break;
                case AccountType.Expense:
                    result.ExpenseLines.Add(dto);
                    break;
            }
        }

        // Sort each section by account code
        result.RevenueLines.Sort((a, b) => string.Compare(a.AccountCode, b.AccountCode, StringComparison.Ordinal));
        result.CostOfSalesLines.Sort((a, b) => string.Compare(a.AccountCode, b.AccountCode, StringComparison.Ordinal));
        result.ExpenseLines.Sort((a, b) => string.Compare(a.AccountCode, b.AccountCode, StringComparison.Ordinal));

        return result;
    }
}
