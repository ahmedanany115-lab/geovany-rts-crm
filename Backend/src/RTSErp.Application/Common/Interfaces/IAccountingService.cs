using RTSErp.Application.Accounting.Common;

namespace RTSErp.Application.Common.Interfaces;

/// <summary>
/// Core double-entry accounting engine.
/// Future Sales, Purchase, Banking modules call this to post transactions.
/// </summary>
public interface IAccountingService
{
    Task<JournalEntryResult> CreateJournalEntryAsync(
        CreateJournalEntryRequest request,
        CancellationToken cancellationToken = default);

    Task<JournalEntryResult> PostJournalEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken = default);

    Task<JournalEntryResult> ReverseJournalEntryAsync(
        Guid entryId,
        string reason,
        DateOnly reversalDate,
        CancellationToken cancellationToken = default);

    Task<AccountBalanceResult> GetAccountBalanceAsync(
        Guid accountId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<Guid> GetOrCreateFiscalPeriodForDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<bool> IsFiscalPeriodOpenAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);
}
