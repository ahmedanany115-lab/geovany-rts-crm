using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.Reports;

public class BalanceSheetLineDto
{
    public Guid   AccountId   { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? AccountNameAr { get; set; }
    public decimal Balance    { get; set; }  // positive = normal balance direction
}

public class BalanceSheetDto
{
    public DateOnly AsOfDate { get; set; }

    public List<BalanceSheetLineDto> AssetLines     { get; set; } = [];
    public List<BalanceSheetLineDto> LiabilityLines { get; set; } = [];
    public List<BalanceSheetLineDto> EquityLines    { get; set; } = [];

    public decimal TotalAssets      => AssetLines.Sum(l => l.Balance);
    public decimal TotalLiabilities => LiabilityLines.Sum(l => l.Balance);
    public decimal TotalEquity      => EquityLines.Sum(l => l.Balance);
    public decimal TotalLiabAndEquity => TotalLiabilities + TotalEquity;

    public decimal ImbalanceAmount => TotalAssets - TotalLiabAndEquity;
    public bool IsBalanced => Math.Abs(ImbalanceAmount) < 0.01m;
}

public class GetBalanceSheetQuery : IRequest<BalanceSheetDto>
{
    public DateOnly AsOfDate { get; set; }
}

public class GetBalanceSheetQueryHandler : IRequestHandler<GetBalanceSheetQuery, BalanceSheetDto>
{
    private readonly IApplicationDbContext _db;
    public GetBalanceSheetQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<BalanceSheetDto> Handle(GetBalanceSheetQuery request, CancellationToken ct)
    {
        // Cumulative balance for BS accounts: all posted JEs from the beginning up to AsOfDate
        var lines = await _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Include(l => l.Account)
            .Where(l => !l.IsDeleted
                     && !l.JournalEntry.IsDeleted
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate <= request.AsOfDate
                     && (l.Account.AccountType == AccountType.Asset
                      || l.Account.AccountType == AccountType.Liability
                      || l.Account.AccountType == AccountType.Equity))
            .GroupBy(l => new
            {
                l.AccountId,
                l.Account.Code, l.Account.Name, l.Account.NameAr,
                l.Account.AccountType,
            })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.Code,
                g.Key.Name,
                g.Key.NameAr,
                g.Key.AccountType,
                // Net debits − credits (debit-normal for assets; credit-normal for liabilities/equity)
                NetDebit = g.Sum(l => l.DebitBase - l.CreditBase),
            })
            .ToListAsync(ct);

        // Add retained earnings: sum of all P&L accounts (Revenue - CostOfSales - Expense) through AsOfDate
        var plLines = await _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Include(l => l.Account)
            .Where(l => !l.IsDeleted
                     && !l.JournalEntry.IsDeleted
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate <= request.AsOfDate
                     && (l.Account.AccountType == AccountType.Revenue
                      || l.Account.AccountType == AccountType.CostOfSales
                      || l.Account.AccountType == AccountType.Expense))
            .SumAsync(l => l.CreditBase - l.DebitBase, ct);

        var result = new BalanceSheetDto { AsOfDate = request.AsOfDate };

        foreach (var line in lines)
        {
            var dto = new BalanceSheetLineDto
            {
                AccountId    = line.AccountId,
                AccountCode  = line.Code,
                AccountName  = line.Name,
                AccountNameAr = line.NameAr,
                Balance = line.AccountType switch
                {
                    AccountType.Asset     => line.NetDebit,          // debit-normal: Dr-Cr
                    AccountType.Liability => -line.NetDebit,         // credit-normal: Cr-Dr
                    AccountType.Equity    => -line.NetDebit,         // credit-normal
                    _ => 0
                }
            };

            if (dto.Balance == 0) continue;

            switch (line.AccountType)
            {
                case AccountType.Asset:     result.AssetLines.Add(dto);     break;
                case AccountType.Liability: result.LiabilityLines.Add(dto); break;
                case AccountType.Equity:    result.EquityLines.Add(dto);    break;
            }
        }

        // Add retained earnings line (net income/loss from P&L accounts)
        if (plLines != 0)
        {
            result.EquityLines.Add(new BalanceSheetLineDto
            {
                AccountCode  = "RE",
                AccountName  = "Retained Earnings (Net Income/Loss)",
                AccountNameAr = "الأرباح المحتجزة",
                Balance = plLines,
            });
        }

        // Sort by code
        result.AssetLines.Sort((a, b) => string.Compare(a.AccountCode, b.AccountCode, StringComparison.Ordinal));
        result.LiabilityLines.Sort((a, b) => string.Compare(a.AccountCode, b.AccountCode, StringComparison.Ordinal));
        result.EquityLines.Sort((a, b) => string.Compare(a.AccountCode, b.AccountCode, StringComparison.Ordinal));

        return result;
    }
}
