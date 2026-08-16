using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.Accounts.Commands.CreateAccount;

public class CreateAccountCommand : IRequest<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public AccountType AccountType { get; set; }
    public bool IsGroup { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? CurrencyId { get; set; }
}

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    private readonly IApplicationDbContext _db;

    public CreateAccountCommandValidator(IApplicationDbContext db)
    {
        _db = db;

        RuleFor(x => x.Code)
            .NotEmpty().MaximumLength(20)
            .MustAsync(BeUniqueCode).WithMessage("Account code '{PropertyValue}' already exists.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AccountType).IsInEnum();
    }

    private async Task<bool> BeUniqueCode(string code, CancellationToken ct) =>
        !await _db.Accounts.AnyAsync(a => a.Code == code && !a.IsDeleted, ct);
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateAccountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = new Account
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            NameAr = request.NameAr?.Trim(),
            AccountType = request.AccountType,
            IsGroup = request.IsGroup,
            ParentId = request.ParentId,
            CurrencyId = request.CurrencyId,
            IsActive = true,
            CreatedBy = _currentUser.UserId
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account.Id;
    }
}
