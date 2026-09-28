using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Finance.CashJournal;

// ── Result types ──────────────────────────────────────────────────────────────

public class CashJournalEntry
{
    public DateOnly Date          { get; set; }
    public string   DocumentNumber { get; set; } = string.Empty;
    public string   Description   { get; set; } = string.Empty;
    public string   ContraAccount { get; set; } = string.Empty;
    /// <summary>Cash IN (debit on the cash account).</summary>
    public decimal  Debit         { get; set; }
    /// <summary>Cash OUT (credit on the cash account).</summary>
    public decimal  Credit        { get; set; }
    public decimal  RunningBalance { get; set; }
    /// <summary>JournalEntry, CashReceipt, CashPayment, etc.</summary>
    public string   SourceType    { get; set; } = string.Empty;
    public Guid?    SourceId      { get; set; }
}

public class CashJournalResult
{
    public Guid     CashAccountId   { get; set; }
    public string   CashAccountName { get; set; } = string.Empty;
    public string   CashAccountCode { get; set; } = string.Empty;
    public DateOnly FromDate        { get; set; }
    public DateOnly ToDate          { get; set; }
    public decimal  OpeningBalance  { get; set; }
    public decimal  TotalDebit      { get; set; }
    public decimal  TotalCredit     { get; set; }
    public decimal  ClosingBalance  { get; set; }
    public List<CashJournalEntry> Entries { get; set; } = [];
}

// ── Query ─────────────────────────────────────────────────────────────────────

public class GetCashJournalQuery : IRequest<CashJournalResult>
{
    public Guid     CashAccountId { get; set; }
    public DateOnly FromDate      { get; set; }
    public DateOnly ToDate        { get; set; }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetCashJournalQueryHandler : IRequestHandler<GetCashJournalQuery, CashJournalResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IAccountingService    _accounting;

    public GetCashJournalQueryHandler(IApplicationDbContext db, IAccountingService accounting)
        => (_db, _accounting) = (db, accounting);

    public async Task<CashJournalResult> Handle(GetCashJournalQuery req, CancellationToken ct)
    {
        // 1. Verify account exists
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == req.CashAccountId && !a.IsDeleted, ct)
            ?? throw new NotFoundException("Account", req.CashAccountId);

        // 2. Opening balance = sum of posted JE lines for this account BEFORE FromDate
        //    Using AccountingService.GetAccountBalanceAsync for the pre-period balance
        var balanceResult = await _accounting.GetAccountBalanceAsync(
            req.CashAccountId,
            req.FromDate,
            req.ToDate,
            ct);

        var openingBalance = balanceResult.OpeningBalance; // OpeningDebit - OpeningCredit

        // 3. Load period JE lines for this cash account, with their parent JE info
        var periodLines = await _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l =>
                l.AccountId == req.CashAccountId
                && l.JournalEntry.Status == JournalEntryStatus.Posted
                && l.JournalEntry.EntryDate >= req.FromDate
                && l.JournalEntry.EntryDate <= req.ToDate
                && !l.IsDeleted
                && !l.JournalEntry.IsDeleted)
            .OrderBy(l => l.JournalEntry.EntryDate)
            .ThenBy(l => l.JournalEntry.EntryNumber)
            .ThenBy(l => l.SortOrder)
            .Select(l => new
            {
                Date          = l.JournalEntry.EntryDate,
                DocumentNumber = l.JournalEntry.EntryNumber,
                Description   = l.JournalEntry.Description,
                l.Debit,
                l.Credit,
                // Identify the contra account(s) in the same JE
                // (for display, we pick the first "other" account name)
                JournalEntryId = l.JournalEntry.Id,
                ReferenceType  = l.JournalEntry.ReferenceType,
                ReferenceId    = l.JournalEntry.ReferenceId
            })
            .ToListAsync(ct);

        // 4. For each JE that contains our account, find the contra account names
        //    We do this in a second pass using the already-loaded data
        var jeIds = periodLines.Select(l => l.JournalEntryId).Distinct().ToList();

        // Fetch all lines of those JEs that are NOT the cash account
        var contraLines = await _db.JournalEntryLines
            .Include(l => l.Account)
            .Where(l => jeIds.Contains(l.JournalEntryId)
                        && l.AccountId != req.CashAccountId
                        && !l.IsDeleted)
            .Select(l => new { l.JournalEntryId, AccountName = l.Account.Name })
            .ToListAsync(ct);

        var contraByJe = contraLines
            .GroupBy(c => c.JournalEntryId)
            .ToDictionary(g => g.Key,
                          g => string.Join(" / ", g.Select(c => c.AccountName).Distinct()));

        // 5. Build journal entries with running balance
        var entries = new List<CashJournalEntry>();
        var running = openingBalance;

        foreach (var line in periodLines)
        {
            running += line.Debit - line.Credit;

            // Determine source type from ReferenceType
            var sourceType = line.ReferenceType switch
            {
                ReferenceType.Receipt => "CashReceipt",
                ReferenceType.Payment => "CashPayment",
                ReferenceType.Reversal => "Reversal",
                _ => "JournalEntry"
            };

            entries.Add(new CashJournalEntry
            {
                Date           = line.Date,
                DocumentNumber = line.DocumentNumber,
                Description    = line.Description,
                ContraAccount  = contraByJe.GetValueOrDefault(line.JournalEntryId, string.Empty),
                Debit          = line.Debit,
                Credit         = line.Credit,
                RunningBalance = running,
                SourceType     = sourceType,
                SourceId       = line.ReferenceId
            });
        }

        var totalDebit  = entries.Sum(e => e.Debit);
        var totalCredit = entries.Sum(e => e.Credit);

        return new CashJournalResult
        {
            CashAccountId   = account.Id,
            CashAccountName = account.Name,
            CashAccountCode = account.Code,
            FromDate        = req.FromDate,
            ToDate          = req.ToDate,
            OpeningBalance  = openingBalance,
            TotalDebit      = totalDebit,
            TotalCredit     = totalCredit,
            ClosingBalance  = openingBalance + totalDebit - totalCredit,
            Entries         = entries
        };
    }
}
