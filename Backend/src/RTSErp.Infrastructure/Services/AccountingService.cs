using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;
using RTSErp.Infrastructure.Persistence;

namespace RTSErp.Infrastructure.Services;

public class AccountingService : IAccountingService
{
    private readonly ApplicationDbContext _db;

    public AccountingService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ─── Create Journal Entry ──────────────────────────────────────────────────

    public async Task<JournalEntryResult> CreateJournalEntryAsync(
        CreateJournalEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate balance
        var totalDebit = request.Lines.Sum(l => l.Debit);
        var totalCredit = request.Lines.Sum(l => l.Credit);
        if (Math.Abs(totalDebit - totalCredit) > 0.001m)
            return JournalEntryResult.Failure(
                $"Journal entry does not balance. Debit: {totalDebit:N2}, Credit: {totalCredit:N2}.");

        // Validate fiscal period
        if (!await IsFiscalPeriodOpenAsync(request.EntryDate, cancellationToken))
            return JournalEntryResult.Failure(
                $"No open fiscal period found for date {request.EntryDate:yyyy-MM-dd}. " +
                "Please create and open a fiscal period first.");

        // Validate all accounts exist and are posting accounts
        var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts
            .Where(a => accountIds.Contains(a.Id) && !a.IsDeleted)
            .ToListAsync(cancellationToken);

        if (accounts.Count != accountIds.Count)
            return JournalEntryResult.Failure("One or more accounts not found.");

        var groupAccounts = accounts.Where(a => a.IsGroup).ToList();
        if (groupAccounts.Any())
            return JournalEntryResult.Failure(
                $"Cannot post to group accounts: {string.Join(", ", groupAccounts.Select(a => a.Code))}.");

        var inactiveAccounts = accounts.Where(a => !a.IsActive).ToList();
        if (inactiveAccounts.Any())
            return JournalEntryResult.Failure(
                $"Cannot post to inactive accounts: {string.Join(", ", inactiveAccounts.Select(a => a.Code))}.");

        // Validate currency
        var currency = await _db.Currencies
            .FirstOrDefaultAsync(c => c.Id == request.CurrencyId && !c.IsDeleted, cancellationToken);
        if (currency is null)
            return JournalEntryResult.Failure("Invalid currency.");

        if (request.ExchangeRate <= 0)
            return JournalEntryResult.Failure("Exchange rate must be greater than zero.");

        // Get fiscal period
        var fiscalPeriodId = await GetOrCreateFiscalPeriodForDateAsync(request.EntryDate, cancellationToken);

        // Generate entry number
        var entryNumber = await GenerateEntryNumberAsync(cancellationToken);

        var entry = new JournalEntry
        {
            EntryNumber = entryNumber,
            EntryDate = request.EntryDate,
            Description = request.Description,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            ReferenceNumber = request.ReferenceNumber,
            CurrencyId = request.CurrencyId,
            ExchangeRate = request.ExchangeRate,
            FiscalPeriodId = fiscalPeriodId,
            Status = JournalEntryStatus.Draft,
            CreatedBy = request.CreatedBy
        };

        int sort = 0;
        foreach (var lineReq in request.Lines)
        {
            var lineCurrencyId = lineReq.CurrencyId ?? request.CurrencyId;
            var lineExRate = lineReq.ExchangeRate ?? request.ExchangeRate;

            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = lineReq.AccountId,
                Debit = lineReq.Debit,
                Credit = lineReq.Credit,
                CurrencyId = lineCurrencyId,
                ExchangeRate = lineExRate,
                DebitBase = lineReq.Debit * lineExRate,
                CreditBase = lineReq.Credit * lineExRate,
                Description = lineReq.Description,
                SortOrder = lineReq.SortOrder > 0 ? lineReq.SortOrder : ++sort,
                CreatedBy = request.CreatedBy
            });
        }

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        if (request.PostImmediately)
        {
            var postResult = await PostJournalEntryAsync(entry.Id, cancellationToken);
            if (!postResult.Succeeded)
                return postResult;
        }

        return JournalEntryResult.Success(entry.Id, entry.EntryNumber);
    }

    // ─── Post Journal Entry ────────────────────────────────────────────────────

    public async Task<JournalEntryResult> PostJournalEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _db.JournalEntries
            .Include(e => e.Lines)
            .Include(e => e.FiscalPeriod)
            .FirstOrDefaultAsync(e => e.Id == entryId && !e.IsDeleted, cancellationToken);

        if (entry is null)
            return JournalEntryResult.Failure("Journal entry not found.");

        if (entry.Status != JournalEntryStatus.Draft)
            return JournalEntryResult.Failure(
                $"Only draft entries can be posted. Current status: {entry.Status}.");

        if (entry.FiscalPeriod.Status == FiscalPeriodStatus.Closed)
            return JournalEntryResult.Failure(
                $"Cannot post into closed fiscal period '{entry.FiscalPeriod.Name}'.");

        // Re-verify balance
        var totalDebit = entry.Lines.Sum(l => l.Debit);
        var totalCredit = entry.Lines.Sum(l => l.Credit);
        if (Math.Abs(totalDebit - totalCredit) > 0.001m)
            return JournalEntryResult.Failure(
                $"Entry does not balance. Debit: {totalDebit:N2}, Credit: {totalCredit:N2}.");

        entry.Status = JournalEntryStatus.Posted;
        entry.ModifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return JournalEntryResult.Success(entry.Id, entry.EntryNumber);
    }

    // ─── Reverse Journal Entry ─────────────────────────────────────────────────

    public async Task<JournalEntryResult> ReverseJournalEntryAsync(
        Guid entryId,
        string reason,
        DateOnly reversalDate,
        CancellationToken cancellationToken = default)
    {
        var original = await _db.JournalEntries
            .Include(e => e.Lines)
            .Include(e => e.Currency)
            .FirstOrDefaultAsync(e => e.Id == entryId && !e.IsDeleted, cancellationToken);

        if (original is null)
            return JournalEntryResult.Failure("Journal entry not found.");

        if (original.Status != JournalEntryStatus.Posted)
            return JournalEntryResult.Failure("Only posted entries can be reversed.");

        if (original.ReversedByEntryId.HasValue)
            return JournalEntryResult.Failure("This entry has already been reversed.");

        if (!await IsFiscalPeriodOpenAsync(reversalDate, cancellationToken))
            return JournalEntryResult.Failure(
                $"No open fiscal period for reversal date {reversalDate:yyyy-MM-dd}.");

        var fiscalPeriodId = await GetOrCreateFiscalPeriodForDateAsync(reversalDate, cancellationToken);
        var reversalNumber = await GenerateEntryNumberAsync(cancellationToken);

        var reversal = new JournalEntry
        {
            EntryNumber = reversalNumber,
            EntryDate = reversalDate,
            Description = $"Reversal of {original.EntryNumber}: {reason}",
            ReferenceType = ReferenceType.Reversal,
            ReferenceId = original.Id,
            ReferenceNumber = original.EntryNumber,
            CurrencyId = original.CurrencyId,
            ExchangeRate = original.ExchangeRate,
            FiscalPeriodId = fiscalPeriodId,
            Status = JournalEntryStatus.Posted,
            ReversesEntryId = original.Id
        };

        int sort = 0;
        foreach (var line in original.Lines)
        {
            reversal.Lines.Add(new JournalEntryLine
            {
                AccountId = line.AccountId,
                // Swap debit/credit
                Debit = line.Credit,
                Credit = line.Debit,
                DebitBase = line.CreditBase,
                CreditBase = line.DebitBase,
                CurrencyId = line.CurrencyId,
                ExchangeRate = line.ExchangeRate,
                Description = $"Reversal: {line.Description}",
                SortOrder = ++sort
            });
        }

        _db.JournalEntries.Add(reversal);

        original.Status = JournalEntryStatus.Reversed;
        original.ReversedByEntryId = reversal.Id;
        original.ModifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return JournalEntryResult.Success(reversal.Id, reversal.EntryNumber);
    }

    // ─── Account Balance ───────────────────────────────────────────────────────

    public async Task<AccountBalanceResult> GetAccountBalanceAsync(
        Guid accountId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && !a.IsDeleted, cancellationToken);

        if (account is null)
            return new AccountBalanceResult { AccountId = accountId };

        // Opening (before fromDate)
        decimal openingDebit = 0, openingCredit = 0;
        if (fromDate.HasValue)
        {
            var opening = await _db.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId
                    && l.JournalEntry.Status == JournalEntryStatus.Posted
                    && l.JournalEntry.EntryDate < fromDate.Value
                    && !l.IsDeleted && !l.JournalEntry.IsDeleted)
                .GroupBy(l => l.AccountId)
                .Select(g => new { D = g.Sum(x => x.DebitBase), C = g.Sum(x => x.CreditBase) })
                .FirstOrDefaultAsync(cancellationToken);
            openingDebit = opening?.D ?? 0;
            openingCredit = opening?.C ?? 0;
        }

        // Period
        var periodQuery = _db.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId
                && l.JournalEntry.Status == JournalEntryStatus.Posted
                && !l.IsDeleted && !l.JournalEntry.IsDeleted);

        if (fromDate.HasValue) periodQuery = periodQuery.Where(l => l.JournalEntry.EntryDate >= fromDate.Value);
        if (toDate.HasValue)   periodQuery = periodQuery.Where(l => l.JournalEntry.EntryDate <= toDate.Value);

        var period = await periodQuery
            .GroupBy(l => l.AccountId)
            .Select(g => new { D = g.Sum(x => x.DebitBase), C = g.Sum(x => x.CreditBase) })
            .FirstOrDefaultAsync(cancellationToken);

        var periodDebit  = period?.D ?? 0;
        var periodCredit = period?.C ?? 0;

        return new AccountBalanceResult
        {
            AccountId    = account.Id,
            AccountCode  = account.Code,
            AccountName  = account.Name,
            OpeningDebit  = openingDebit,
            OpeningCredit = openingCredit,
            PeriodDebit   = periodDebit,
            PeriodCredit  = periodCredit,
            ClosingDebit  = openingDebit + periodDebit,
            ClosingCredit = openingCredit + periodCredit
        };
    }

    // ─── Fiscal Period helpers ─────────────────────────────────────────────────

    public async Task<Guid> GetOrCreateFiscalPeriodForDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var period = await _db.FiscalPeriods
            .FirstOrDefaultAsync(p => p.StartDate <= date && p.EndDate >= date && !p.IsDeleted, cancellationToken);

        if (period is null)
            throw new InvalidOperationException(
                $"No fiscal period exists for {date:yyyy-MM-dd}. Create one before posting.");

        return period.Id;
    }

    public async Task<bool> IsFiscalPeriodOpenAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _db.FiscalPeriods
            .AnyAsync(p => p.StartDate <= date
                && p.EndDate >= date
                && p.Status == FiscalPeriodStatus.Open
                && !p.IsDeleted, cancellationToken);
    }

    // ─── Entry number generator ────────────────────────────────────────────────

    private async Task<string> GenerateEntryNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"JE{year}-";

        var last = await _db.JournalEntries
            .Where(e => e.EntryNumber.StartsWith(prefix))
            .OrderByDescending(e => e.EntryNumber)
            .Select(e => e.EntryNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int next = 1;
        if (last is not null && last.Length > prefix.Length
            && int.TryParse(last[prefix.Length..], out var lastNum))
        {
            next = lastNum + 1;
        }

        return $"{prefix}{next:D5}";
    }
}
