using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.Accounts.Queries.GetAccounts;

public class AccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public AccountType AccountType { get; set; }
    public string AccountTypeName => AccountType.ToString();
    public bool IsGroup { get; set; }
    public bool IsActive { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentCode { get; set; }
    public string? ParentName { get; set; }
    public Guid? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public int ChildCount { get; set; }
}

public class GetAccountsQuery : IRequest<List<AccountDto>>
{
    public AccountType? AccountType { get; set; }
    public bool? IsGroup { get; set; }
    public bool? IsActive { get; set; }
    public Guid? ParentId { get; set; }
    public bool TopLevelOnly { get; set; }
}

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, List<AccountDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAccountsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<AccountDto>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Accounts
            .Include(a => a.Parent)
            .Include(a => a.Currency)
            .Where(a => !a.IsDeleted)
            .AsQueryable();

        if (request.AccountType.HasValue)
            query = query.Where(a => a.AccountType == request.AccountType.Value);

        if (request.IsGroup.HasValue)
            query = query.Where(a => a.IsGroup == request.IsGroup.Value);

        if (request.IsActive.HasValue)
            query = query.Where(a => a.IsActive == request.IsActive.Value);

        if (request.ParentId.HasValue)
            query = query.Where(a => a.ParentId == request.ParentId.Value);

        if (request.TopLevelOnly)
            query = query.Where(a => a.ParentId == null);

        var accounts = await query.OrderBy(a => a.Code).ToListAsync(cancellationToken);

        // Count children without loading them all
        var ids = accounts.Select(a => a.Id).ToList();
        var childCounts = await _db.Accounts
            .Where(a => a.ParentId.HasValue && ids.Contains(a.ParentId!.Value) && !a.IsDeleted)
            .GroupBy(a => a.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var countMap = childCounts.ToDictionary(x => x.ParentId, x => x.Count);

        return accounts.Select(a => new AccountDto
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
            ChildCount = countMap.TryGetValue(a.Id, out var c) ? c : 0
        }).ToList();
    }
}
