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
    private readonly ICurrentUserService   _currentUser;

    public UpdateAccountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        => (_db, _currentUser) = (db, currentUser);

    public async Task Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        // 1. Load with AsNoTracking to avoid snapshot conflicts
        var account = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id && !a.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Accounting.Account), request.Id);

        // 2. Validate new code if provided
        var newCode = string.IsNullOrWhiteSpace(request.Code) ? account.Code : request.Code.Trim();

        if (newCode != account.Code)
        {
            var duplicate = await _db.Accounts
                .AnyAsync(a => a.Code == newCode && a.Id != request.Id && !a.IsDeleted, cancellationToken);
            if (duplicate)
                throw new InvalidOperationException($"Account code '{newCode}' is already in use.");
        }

        var name   = request.Name.Trim();
        var nameAr = request.NameAr?.Trim();
        var now    = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        // 3. Use ExecuteUpdateAsync — direct SQL UPDATE, no EF change-tracker involved
        var rows = await _db.Accounts
            .Where(a => a.Id == request.Id && !a.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Code,       newCode)
                .SetProperty(a => a.Name,       name)
                .SetProperty(a => a.NameAr,     nameAr)
                .SetProperty(a => a.IsGroup,    request.IsGroup)
                .SetProperty(a => a.ParentId,   request.ParentId)
                .SetProperty(a => a.CurrencyId, request.CurrencyId)
                .SetProperty(a => a.ModifiedAt, now)
                .SetProperty(a => a.ModifiedBy, userId),
            cancellationToken);

        if (rows == 0)
            throw new NotFoundException(nameof(Domain.Entities.Accounting.Account), request.Id);
    }
}
