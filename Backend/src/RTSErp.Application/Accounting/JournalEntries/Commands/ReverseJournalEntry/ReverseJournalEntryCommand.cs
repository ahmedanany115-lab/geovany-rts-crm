using FluentValidation;
using MediatR;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Interfaces;

namespace RTSErp.Application.Accounting.JournalEntries.Commands.ReverseJournalEntry;

public class ReverseJournalEntryCommand : IRequest<JournalEntryResult>
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateOnly ReversalDate { get; set; }
}

public class ReverseJournalEntryCommandValidator : AbstractValidator<ReverseJournalEntryCommand>
{
    public ReverseJournalEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class ReverseJournalEntryCommandHandler : IRequestHandler<ReverseJournalEntryCommand, JournalEntryResult>
{
    private readonly IAccountingService _accounting;

    public ReverseJournalEntryCommandHandler(IAccountingService accounting)
        => _accounting = accounting;

    public Task<JournalEntryResult> Handle(ReverseJournalEntryCommand request, CancellationToken cancellationToken)
        => _accounting.ReverseJournalEntryAsync(request.Id, request.Reason, request.ReversalDate, cancellationToken);
}
