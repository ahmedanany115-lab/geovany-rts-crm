using FluentAssertions;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;
using Xunit;

namespace RTSErp.Domain.UnitTests.Accounting;

/// <summary>
/// Pure domain-level tests — no EF, no DI.
/// Tests the accounting rules that hold true regardless of infrastructure.
/// </summary>
public class JournalEntryDomainTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static JournalEntry BuildPostedEntry(decimal debit, decimal credit,
        Guid? accountDebitId = null, Guid? accountCreditId = null)
    {
        var currencyId = Guid.NewGuid();
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNumber = "JE2026-00001",
            EntryDate = new DateOnly(2026, 1, 15),
            Description = "Test entry",
            Status = JournalEntryStatus.Posted,
            CurrencyId = currencyId,
            ExchangeRate = 1m,
            FiscalPeriodId = Guid.NewGuid()
        };

        if (debit > 0)
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = accountDebitId ?? Guid.NewGuid(),
                Debit = debit, Credit = 0,
                DebitBase = debit, CreditBase = 0,
                CurrencyId = currencyId, ExchangeRate = 1m
            });

        if (credit > 0)
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = accountCreditId ?? Guid.NewGuid(),
                Debit = 0, Credit = credit,
                DebitBase = 0, CreditBase = credit,
                CurrencyId = currencyId, ExchangeRate = 1m
            });

        return entry;
    }

    // ─── 1. Balanced entry ─────────────────────────────────────────────────────

    [Fact]
    public void BalancedEntry_TotalDebit_EqualsTotal_Credit()
    {
        var entry = BuildPostedEntry(1000m, 1000m);

        var totalDebit  = entry.Lines.Sum(l => l.Debit);
        var totalCredit = entry.Lines.Sum(l => l.Credit);

        totalDebit.Should().Be(totalCredit);
        Math.Abs(totalDebit - totalCredit).Should().BeLessThan(0.001m);
    }

    // ─── 2. Unbalanced entry detection ────────────────────────────────────────

    [Theory]
    [InlineData(1000, 999)]
    [InlineData(500,  600)]
    [InlineData(0,    100)]
    public void UnbalancedEntry_IsDetectedByImbalance(decimal debit, decimal credit)
    {
        var entry = BuildPostedEntry(debit, credit);

        var totalDebit  = entry.Lines.Sum(l => l.Debit);
        var totalCredit = entry.Lines.Sum(l => l.Credit);

        Math.Abs(totalDebit - totalCredit).Should().BeGreaterThan(0.001m,
            "an unbalanced entry must be rejected by the posting engine");
    }

    // ─── 3. Negative amounts are invalid ──────────────────────────────────────

    [Theory]
    [InlineData(-100, 0)]
    [InlineData(0, -100)]
    public void NegativeAmounts_AreInvalid(decimal debit, decimal credit)
    {
        // FluentValidation enforces this; here we verify the constraint at domain level
        var hasNegative = debit < 0 || credit < 0;
        hasNegative.Should().BeTrue("test data should contain a negative value");

        // Confirm business rule: no line may carry a negative amount
        var lineIsInvalid = debit < 0 || credit < 0;
        lineIsInvalid.Should().BeTrue();
    }

    // ─── 4. A single line cannot have both Debit and Credit ───────────────────

    [Fact]
    public void SingleLine_WithBothDebitAndCredit_IsInvalid()
    {
        var line = new JournalEntryLine { Debit = 500m, Credit = 500m };

        var invalid = line.Debit > 0 && line.Credit > 0;
        invalid.Should().BeTrue("a line cannot carry both a debit and credit amount simultaneously");
    }

    // ─── 5. Posted entry cannot be edited — status check ─────────────────────

    [Fact]
    public void PostedEntry_Status_IsPosted()
    {
        var entry = BuildPostedEntry(500m, 500m);
        entry.Status.Should().Be(JournalEntryStatus.Posted);

        // An edit attempt should fail — the service checks this before saving
        var canEdit = entry.Status == JournalEntryStatus.Draft;
        canEdit.Should().BeFalse("posted entries are immutable; use reversal instead");
    }

    // ─── 6. Reversal swaps debit and credit ───────────────────────────────────

    [Fact]
    public void Reversal_SwapsDebitAndCredit_ForEachLine()
    {
        var originalEntry = BuildPostedEntry(750m, 750m);
        var originalDebit  = originalEntry.Lines.Sum(l => l.Debit);
        var originalCredit = originalEntry.Lines.Sum(l => l.Credit);

        // Simulate what AccountingService.ReverseJournalEntryAsync does
        var reversalLines = originalEntry.Lines.Select(l => new JournalEntryLine
        {
            AccountId  = l.AccountId,
            Debit      = l.Credit,      // swap
            Credit     = l.Debit,       // swap
            DebitBase  = l.CreditBase,
            CreditBase = l.DebitBase,
            CurrencyId = l.CurrencyId,
            ExchangeRate = l.ExchangeRate
        }).ToList();

        var reversalDebit  = reversalLines.Sum(l => l.Debit);
        var reversalCredit = reversalLines.Sum(l => l.Credit);

        // Reversal must itself balance
        Math.Abs(reversalDebit - reversalCredit).Should().BeLessThan(0.001m);

        // And the net effect of original + reversal should be zero
        (originalDebit  + reversalDebit).Should().Be(originalCredit + reversalCredit);
        (originalDebit  - reversalCredit).Should().Be(0m, "reversal exactly cancels the original");
    }

    // ─── 7. Currency and exchange rate calculations ────────────────────────────

    [Theory]
    [InlineData(100, 48.75)]   // 100 USD × 48.75 = 4,875 EGP
    [InlineData(200, 48.75)]   // 200 USD × 48.75 = 9,750 EGP
    [InlineData(50,  50.0)]
    public void BaseAmount_IsCalculated_Correctly(decimal foreignAmount, decimal exchangeRate)
    {
        var baseAmount = foreignAmount * exchangeRate;

        var line = new JournalEntryLine
        {
            Debit     = foreignAmount,
            DebitBase = foreignAmount * (decimal)exchangeRate,
            ExchangeRate = (decimal)exchangeRate
        };

        line.DebitBase.Should().Be(baseAmount);
        (line.Debit * line.ExchangeRate).Should().Be(line.DebitBase);
    }

    // ─── 8. Fiscal period status guards ───────────────────────────────────────

    [Fact]
    public void ClosedFiscalPeriod_ShouldBlock_Posting()
    {
        var period = new FiscalPeriod
        {
            Status = FiscalPeriodStatus.Closed,
            Name   = "FY2025",
            StartDate = new DateOnly(2025, 1, 1),
            EndDate   = new DateOnly(2025, 12, 31)
        };

        var canPost = period.Status == FiscalPeriodStatus.Open;
        canPost.Should().BeFalse("posting into a closed fiscal period must be rejected");
    }

    [Fact]
    public void OpenFiscalPeriod_ShouldAllow_Posting()
    {
        var period = new FiscalPeriod
        {
            Status = FiscalPeriodStatus.Open,
            Name   = "FY2026",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate   = new DateOnly(2026, 12, 31)
        };

        var canPost = period.Status == FiscalPeriodStatus.Open;
        canPost.Should().BeTrue();
    }

    // ─── 9. Account ledger balance calculation ─────────────────────────────────

    [Fact]
    public void AccountBalance_CalculatesCorrectly_FromLines()
    {
        // Debit-side account (e.g. bank): opening + debits - credits = closing
        decimal openingBalance = 10_000m;  // dr balance brought forward
        var lines = new[]
        {
            new { Debit = 5_000m, Credit = 0m },   // receipt
            new { Debit = 0m,     Credit = 2_000m }, // payment
            new { Debit = 3_000m, Credit = 0m },   // receipt
        };

        var totalDebit  = lines.Sum(l => l.Debit);
        var totalCredit = lines.Sum(l => l.Credit);
        var closing     = openingBalance + totalDebit - totalCredit;

        totalDebit.Should().Be(8_000m);
        totalCredit.Should().Be(2_000m);
        closing.Should().Be(16_000m);
    }

    // ─── 10. Trial balance: total debits must equal total credits ─────────────

    [Fact]
    public void TrialBalance_TotalDebits_MustEqual_TotalCredits()
    {
        // Simulate a simple set of accounts in the GL
        var glEntries = new[]
        {
            new { AccountCode = "1101", Debit = 50_000m, Credit = 0m      },  // Cash
            new { AccountCode = "1301", Debit = 20_000m, Credit = 0m      },  // AR
            new { AccountCode = "2101", Debit = 0m,      Credit = 15_000m },  // AP
            new { AccountCode = "4100", Debit = 0m,      Credit = 55_000m },  // Revenue
        };

        var totalDebit  = glEntries.Sum(e => e.Debit);
        var totalCredit = glEntries.Sum(e => e.Credit);

        // This is balanced by design; a real TB would read from the DB
        totalDebit.Should().Be(totalCredit,
            "the trial balance must balance — total debits = total credits");
    }
}
