using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.JournalEntries.Commands.CreateJournalEntry;

public class CreateJournalEntryLineDto
{
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class CreateJournalEntryCommand : IRequest<JournalEntryResult>
{
    public DateOnly EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public ReferenceType ReferenceType { get; set; } = ReferenceType.Manual;
    public Guid? ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public bool PostImmediately { get; set; }
    public List<CreateJournalEntryLineDto> Lines { get; set; } = [];
}

public class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Journal entry must have at least two lines.");
        RuleFor(x => x.Lines).Must(l => l.Count >= 2)
            .WithMessage("Journal entry must have at least two lines.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.Debit).GreaterThanOrEqualTo(0)
                .WithMessage("Debit amount cannot be negative.");
            line.RuleFor(l => l.Credit).GreaterThanOrEqualTo(0)
                .WithMessage("Credit amount cannot be negative.");
            line.RuleFor(l => l).Must(l => !(l.Debit > 0 && l.Credit > 0))
                .WithMessage("A line cannot have both debit and credit amounts.");
            line.RuleFor(l => l).Must(l => l.Debit > 0 || l.Credit > 0)
                .WithMessage("A line must have either a debit or credit amount.");
        });
    }
}

public class CreateJournalEntryCommandHandler : IRequestHandler<CreateJournalEntryCommand, JournalEntryResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IAccountingService _accounting;
    private readonly ICurrentUserService _currentUser;

    public CreateJournalEntryCommandHandler(
        IApplicationDbContext db,
        IAccountingService accounting,
        ICurrentUserService currentUser)
    {
        _db = db;
        _accounting = accounting;
        _currentUser = currentUser;
    }

    public async Task<JournalEntryResult> Handle(CreateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var req = new CreateJournalEntryRequest
        {
            EntryDate = request.EntryDate,
            Description = request.Description,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            ReferenceNumber = request.ReferenceNumber,
            CurrencyId = request.CurrencyId,
            ExchangeRate = request.ExchangeRate,
            PostImmediately = request.PostImmediately,
            CreatedBy = _currentUser.UserId,
            Lines = request.Lines.Select(l => new JournalEntryLineRequest
            {
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                Description = l.Description,
                SortOrder = l.SortOrder
            }).ToList()
        };

        return await _accounting.CreateJournalEntryAsync(req, cancellationToken);
    }
}
