using MediatR;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Interfaces;

namespace RTSErp.Application.Accounting.JournalEntries.Commands.PostJournalEntry;

public class PostJournalEntryCommand : IRequest<JournalEntryResult>
{
    public Guid Id { get; set; }
}

public class PostJournalEntryCommandHandler : IRequestHandler<PostJournalEntryCommand, JournalEntryResult>
{
    private readonly IAccountingService _accounting;

    public PostJournalEntryCommandHandler(IAccountingService accounting)
        => _accounting = accounting;

    public Task<JournalEntryResult> Handle(PostJournalEntryCommand request, CancellationToken cancellationToken)
        => _accounting.PostJournalEntryAsync(request.Id, cancellationToken);
}
