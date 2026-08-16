using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;

namespace RTSErp.Application.Accounting.Accounts.Commands.ToggleAccountStatus;

public class ToggleAccountStatusCommand : IRequest
{
    public Guid Id { get; set; }
}

public class ToggleAccountStatusCommandHandler : IRequestHandler<ToggleAccountStatusCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleAccountStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ToggleAccountStatusCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.Id && !a.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Accounting.Account), request.Id);

        // Cannot deactivate if it has posted journal lines
        if (account.IsActive)
        {
            var hasLines = await _db.JournalEntryLines
                .AnyAsync(l => l.AccountId == request.Id, cancellationToken);
            if (hasLines)
                throw new InvalidOperationException(
                    "Cannot deactivate an account that has accounting transactions.");
        }

        account.IsActive = !account.IsActive;
        account.ModifiedAt = DateTime.UtcNow;
        account.ModifiedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
