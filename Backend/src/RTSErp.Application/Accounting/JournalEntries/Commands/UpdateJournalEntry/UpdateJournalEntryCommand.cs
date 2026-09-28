using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Entities.Audit;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.JournalEntries.Commands.UpdateJournalEntry;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class UpdateJournalEntryLineDto
{
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

// ─── Command ─────────────────────────────────────────────────────────────────

public class UpdateJournalEntryCommand : IRequest<UpdateJournalEntryResult>
{
    public Guid Id { get; set; }
    public DateOnly EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public string? ReferenceNumber { get; set; }
    public List<UpdateJournalEntryLineDto> Lines { get; set; } = [];
}

// ─── Result ───────────────────────────────────────────────────────────────────

public class UpdateJournalEntryResult
{
    public bool Succeeded { get; set; }
    public string[] Errors { get; set; } = [];

    public static UpdateJournalEntryResult Success() =>
        new() { Succeeded = true };

    public static UpdateJournalEntryResult Failure(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };
}

// ─── Validator ────────────────────────────────────────────────────────────────

public class UpdateJournalEntryCommandValidator : AbstractValidator<UpdateJournalEntryCommand>
{
    public UpdateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Journal entry must have at least two lines.");
        RuleFor(x => x.Lines).Must(l => l.Count >= 2)
            .WithMessage("Journal entry must have at least two lines.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.Debit).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.Credit).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l).Must(l => !(l.Debit > 0 && l.Credit > 0))
                .WithMessage("A line cannot have both debit and credit amounts.");
            line.RuleFor(l => l).Must(l => l.Debit > 0 || l.Credit > 0)
                .WithMessage("A line must have either a debit or credit amount.");
        });
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

/// <summary>
/// Moataz's exact email in the seeded database.
/// If this user is ever recreated with a different email, update this constant.
/// Identified from DbSeeder.cs — stable email: Moataz@rtegy.com
/// </summary>
file static class PrivilegedUsers
{
    public const string MoatazEmail = "Moataz@rtegy.com";
}

public class UpdateJournalEntryCommandHandler : IRequestHandler<UpdateJournalEntryCommand, UpdateJournalEntryResult>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public UpdateJournalEntryCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    /// <summary>
    /// Returns true when the current user is allowed to edit/delete Posted entries.
    /// Allowed: Admin role OR Moataz@rtegy.com (identified by seeded email).
    /// </summary>
    private bool CanModifyPostedEntry() =>
        _currentUser.IsInRole("Admin") ||
        string.Equals(_currentUser.Email, PrivilegedUsers.MoatazEmail, StringComparison.OrdinalIgnoreCase);

    public async Task<UpdateJournalEntryResult> Handle(UpdateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        // Load entry with existing lines
        var entry = await _db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, cancellationToken);

        if (entry is null)
            return UpdateJournalEntryResult.Failure("Journal entry not found.");

        // Business rule: Draft → anyone authorized may edit.
        //                Posted → only Admin or Moataz may edit.
        //                Reversed → nobody may edit.
        if (entry.Status == JournalEntryStatus.Reversed)
            return UpdateJournalEntryResult.Failure(
                "Reversed journal entries cannot be edited.");

        if (entry.Status == JournalEntryStatus.Posted && !CanModifyPostedEntry())
            return UpdateJournalEntryResult.Failure(
                "Only administrators or privileged users can edit Posted journal entries.");

        // Validate balance
        var totalDebit  = request.Lines.Sum(l => l.Debit);
        var totalCredit = request.Lines.Sum(l => l.Credit);
        if (Math.Abs(totalDebit - totalCredit) > 0.001m)
            return UpdateJournalEntryResult.Failure(
                $"Journal entry does not balance. Debit: {totalDebit:N2}, Credit: {totalCredit:N2}.");

        // Validate currency
        var currency = await _db.Currencies
            .FirstOrDefaultAsync(c => c.Id == request.CurrencyId && !c.IsDeleted, cancellationToken);
        if (currency is null)
            return UpdateJournalEntryResult.Failure("Invalid currency.");

        // Validate accounts
        var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts
            .Where(a => accountIds.Contains(a.Id) && !a.IsDeleted)
            .ToListAsync(cancellationToken);

        if (accounts.Count != accountIds.Count)
            return UpdateJournalEntryResult.Failure("One or more accounts not found.");

        var groupAccounts = accounts.Where(a => a.IsGroup).ToList();
        if (groupAccounts.Any())
            return UpdateJournalEntryResult.Failure(
                $"Cannot post to group accounts: {string.Join(", ", groupAccounts.Select(a => a.Code))}.");

        var inactiveAccounts = accounts.Where(a => !a.IsActive).ToList();
        if (inactiveAccounts.Any())
            return UpdateJournalEntryResult.Failure(
                $"Cannot post to inactive accounts: {string.Join(", ", inactiveAccounts.Select(a => a.Code))}.");

        // ── Update header ──────────────────────────────────────────────────────
        entry.EntryDate       = request.EntryDate;
        entry.Description     = request.Description;
        entry.CurrencyId      = request.CurrencyId;
        entry.ExchangeRate    = request.ExchangeRate;
        entry.ReferenceNumber = request.ReferenceNumber;
        entry.ModifiedAt      = DateTime.UtcNow;
        entry.ModifiedBy      = _currentUser.UserId;

        // ── Replace lines: soft-delete existing, add new ───────────────────────
        foreach (var existingLine in entry.Lines)
        {
            existingLine.IsDeleted  = true;
            existingLine.ModifiedAt = DateTime.UtcNow;
        }

        for (int i = 0; i < request.Lines.Count; i++)
        {
            var lineReq = request.Lines[i];
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId    = lineReq.AccountId,
                Debit        = lineReq.Debit,
                Credit       = lineReq.Credit,
                CurrencyId   = request.CurrencyId,
                ExchangeRate = request.ExchangeRate,
                DebitBase    = lineReq.Debit  * request.ExchangeRate,
                CreditBase   = lineReq.Credit * request.ExchangeRate,
                Description  = lineReq.Description,
                SortOrder    = lineReq.SortOrder > 0 ? lineReq.SortOrder : i + 1,
                CreatedBy    = _currentUser.UserId,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Audit — fire-and-forget; never throws to caller
        await _audit.LogAsync(
            action:     AuditActions.Updated,
            module:     AuditModules.Finance,
            entityName: "JournalEntry",
            entityId:   entry.Id,
            entityType: "JournalEntry",
            reference:  entry.EntryNumber,
            details:    entry.Status == JournalEntryStatus.Posted
                            ? $"Posted entry edited by privileged user ({_currentUser.Email})"
                            : "Draft entry edited",
            ct: cancellationToken);

        return UpdateJournalEntryResult.Success();
    }
}
