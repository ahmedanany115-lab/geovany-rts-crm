using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;

namespace RTSErp.Application.Accounting.Accounts.Commands.UpdateAccount;

public class UpdateAccountCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Code { get; set; }   // Optional: if provided, change the account code
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public bool IsGroup { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? CurrencyId { get; set; }
}

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(30).When(x => !string.IsNullOrWhiteSpace(x.Code));
    }
}

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateAccountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.Id && !a.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Accounting.Account), request.Id);

        // If code is being changed, validate no duplicate
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var newCode = request.Code.Trim();
            if (newCode != account.Code)
            {
                var duplicate = await _db.Accounts
                    .AnyAsync(a => a.Code == newCode && a.Id != request.Id && !a.IsDeleted, cancellationToken);
                if (duplicate)
                    throw new InvalidOperationException($"Account code '{newCode}' is already used by another account.");
            }
            account.Code = newCode;  // persist even if same (idempotent)
        }

        account.Name       = request.Name.Trim();
        account.NameAr     = request.NameAr?.Trim();
        account.IsGroup    = request.IsGroup;
        account.ParentId   = request.ParentId;
        account.CurrencyId = request.CurrencyId;
        account.ModifiedAt = DateTime.UtcNow;
        account.ModifiedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
