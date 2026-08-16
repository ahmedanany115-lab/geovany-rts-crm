using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.Common;

// ─── Journal Entry creation request ─────────────────────────────────────────

public class CreateJournalEntryRequest
{
    public DateOnly EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public ReferenceType ReferenceType { get; set; } = ReferenceType.Manual;
    public Guid? ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public bool PostImmediately { get; set; }
    public List<JournalEntryLineRequest> Lines { get; set; } = [];
    public Guid? CreatedBy { get; set; }
}

public class JournalEntryLineRequest
{
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public Guid? CurrencyId { get; set; }         // optional override; defaults to entry currency
    public decimal? ExchangeRate { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

// ─── Results ─────────────────────────────────────────────────────────────────

public class JournalEntryResult
{
    public bool Succeeded { get; set; }
    public string[] Errors { get; set; } = [];
    public Guid? EntryId { get; set; }
    public string? EntryNumber { get; set; }

    public static JournalEntryResult Success(Guid id, string number) =>
        new() { Succeeded = true, EntryId = id, EntryNumber = number };

    public static JournalEntryResult Failure(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };
}

public class AccountBalanceResult
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }

    public decimal OpeningBalance => OpeningDebit - OpeningCredit;
    public decimal NetMovement => PeriodDebit - PeriodCredit;
    public decimal ClosingBalance => ClosingDebit - ClosingCredit;
}
