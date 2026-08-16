using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Accounts.Queries.GetAccounts;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;

namespace RTSErp.Application.Accounting.Accounts.Queries.GetAccount;

public class GetAccountQuery : IRequest<AccountDto>
{
    public Guid Id { get; set; }
}

public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, AccountDto>
{
    private readonly IApplicationDbContext _db;

    public GetAccountQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<AccountDto> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        var a = await _db.Accounts
            .Include(x => x.Parent)
            .Include(x => x.Currency)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Accounting.Account), request.Id);

        var childCount = await _db.Accounts
            .CountAsync(x => x.ParentId == a.Id && !x.IsDeleted, cancellationToken);

        return new AccountDto
        {
            Id = a.Id,
            Code = a.Code,
            Name = a.Name,
            NameAr = a.NameAr,
            AccountType = a.AccountType,
            IsGroup = a.IsGroup,
            IsActive = a.IsActive,
            ParentId = a.ParentId,
            ParentCode = a.Parent?.Code,
            ParentName = a.Parent?.Name,
            CurrencyId = a.CurrencyId,
            CurrencyCode = a.Currency?.Code,
            ChildCount = childCount
        };
    }
}
