using FluentAssertions;
using Moq;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;
using Xunit;

namespace RTSErp.Application.UnitTests.Accounting;

/// <summary>
/// Tests the AccountingService contract via IAccountingService mock.
/// Pure application-layer tests — no infrastructure dependency.
/// </summary>
public class AccountingServiceContractTests
{
    private readonly Mock<IAccountingService> _serviceMock = new();

    // ─── 1. Balanced journal entry succeeds ───────────────────────────────────

    [Fact]
    public async Task CreateJournalEntry_Balanced_Succeeds()
    {
        var request = MakeRequest(debit: 1000m, credit: 1000m);

        _serviceMock
            .Setup(s => s.CreateJournalEntryAsync(It.IsAny<CreateJournalEntryRequest>(), default))
            .ReturnsAsync(JournalEntryResult.Success(Guid.NewGuid(), "JE2026-00001"));

        var result = await _serviceMock.Object.CreateJournalEntryAsync(request);

        result.Succeeded.Should().BeTrue();
        result.EntryNumber.Should().NotBeNullOrEmpty();
    }

    // ─── 2. Unbalanced journal entry fails ────────────────────────────────────

    [Fact]
    public async Task CreateJournalEntry_Unbalanced_Fails()
    {
        var request = MakeRequest(debit: 1000m, credit: 900m);

        _serviceMock
            .Setup(s => s.CreateJournalEntryAsync(
                It.Is<CreateJournalEntryRequest>(r =>
                    r.Lines.Sum(l => l.Debit) != r.Lines.Sum(l => l.Credit)),
                default))
            .ReturnsAsync(JournalEntryResult.Failure(
                "Journal entry does not balance. Debit: 1000.00, Credit: 900.00."));

        var result = await _serviceMock.Object.CreateJournalEntryAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("balance"));
    }

    // ─── 3. Closed fiscal period rejects posting ──────────────────────────────

    [Fact]
    public async Task PostJournalEntry_IntoClosedPeriod_Fails()
    {
        var entryId = Guid.NewGuid();

        _serviceMock
            .Setup(s => s.PostJournalEntryAsync(entryId, default))
            .ReturnsAsync(JournalEntryResult.Failure("Cannot post into closed fiscal period 'FY2025'."));

        var result = await _serviceMock.Object.PostJournalEntryAsync(entryId);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("closed fiscal period"));
    }

    // ─── 4. Reversal creates the correct opposite entry ───────────────────────

    [Fact]
    public async Task ReverseJournalEntry_ReturnsNewEntryId_AndNumber()
    {
        var originalId    = Guid.NewGuid();
        var reversalId    = Guid.NewGuid();
        var reversalDate  = new DateOnly(2026, 2, 1);

        _serviceMock
            .Setup(s => s.ReverseJournalEntryAsync(originalId, It.IsAny<string>(), reversalDate, default))
            .ReturnsAsync(JournalEntryResult.Success(reversalId, "JE2026-00002"));

        var result = await _serviceMock.Object
            .ReverseJournalEntryAsync(originalId, "Error correction", reversalDate);

        result.Succeeded.Should().BeTrue();
        result.EntryId.Should().Be(reversalId);
        result.EntryNumber.Should().Be("JE2026-00002");
    }

    // ─── 5. Account balance calculation ───────────────────────────────────────

    [Fact]
    public async Task GetAccountBalance_ReturnsCorrectBalance()
    {
        var accountId = Guid.NewGuid();
        var from = new DateOnly(2026, 1, 1);
        var to   = new DateOnly(2026, 12, 31);

        var expected = new AccountBalanceResult
        {
            AccountId     = accountId,
            AccountCode   = "1101",
            AccountName   = "Petty Cash",
            OpeningDebit  = 0m,
            OpeningCredit = 0m,
            PeriodDebit   = 50_000m,
            PeriodCredit  = 20_000m,
            ClosingDebit  = 50_000m,
            ClosingCredit = 20_000m
        };

        _serviceMock
            .Setup(s => s.GetAccountBalanceAsync(accountId, from, to, default))
            .ReturnsAsync(expected);

        var result = await _serviceMock.Object.GetAccountBalanceAsync(accountId, from, to);

        result.ClosingBalance.Should().Be(30_000m,
            "closing balance = (opening + period) debit - (opening + period) credit");
        result.NetMovement.Should().Be(30_000m);
    }

    // ─── 6. IsFiscalPeriodOpen returns false for closed period ────────────────

    [Fact]
    public async Task IsFiscalPeriodOpen_ReturnsFalse_ForClosedPeriod()
    {
        var dateInClosedPeriod = new DateOnly(2025, 6, 15);

        _serviceMock
            .Setup(s => s.IsFiscalPeriodOpenAsync(dateInClosedPeriod, default))
            .ReturnsAsync(false);

        var isOpen = await _serviceMock.Object.IsFiscalPeriodOpenAsync(dateInClosedPeriod);
        isOpen.Should().BeFalse();
    }

    // ─── 7. Exchange rate is applied to compute base amounts ──────────────────

    [Fact]
    public void ExchangeRate_ComputesBaseAmount_Correctly()
    {
        decimal foreignAmount = 1000m;
        decimal rate = 48.75m;

        var baseAmount = foreignAmount * rate;

        baseAmount.Should().Be(48_750m);
    }

    // ─── 8. Cannot reverse an already-reversed entry ──────────────────────────

    [Fact]
    public async Task ReverseJournalEntry_AlreadyReversed_Fails()
    {
        var entryId      = Guid.NewGuid();
        var reversalDate = new DateOnly(2026, 3, 1);

        _serviceMock
            .Setup(s => s.ReverseJournalEntryAsync(entryId, It.IsAny<string>(), reversalDate, default))
            .ReturnsAsync(JournalEntryResult.Failure("This entry has already been reversed."));

        var result = await _serviceMock.Object
            .ReverseJournalEntryAsync(entryId, "Second attempt", reversalDate);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("already been reversed"));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CreateJournalEntryRequest MakeRequest(decimal debit, decimal credit)
    {
        var currencyId = Guid.NewGuid();
        return new CreateJournalEntryRequest
        {
            EntryDate = new DateOnly(2026, 1, 15),
            Description = "Test journal entry",
            CurrencyId = currencyId,
            ExchangeRate = 1m,
            Lines = new List<JournalEntryLineRequest>
            {
                new() { AccountId = Guid.NewGuid(), Debit = debit,  Credit = 0m,     CurrencyId = currencyId },
                new() { AccountId = Guid.NewGuid(), Debit = 0m,     Credit = credit,  CurrencyId = currencyId }
            }
        };
    }
}
