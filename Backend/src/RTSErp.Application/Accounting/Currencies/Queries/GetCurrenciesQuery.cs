using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;

namespace RTSErp.Application.Accounting.Currencies.Queries;

public class CurrencyDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public bool IsBaseCurrency { get; set; }
    public bool IsActive { get; set; }
}

public class GetCurrenciesQuery : IRequest<List<CurrencyDto>> { }

public class GetCurrenciesQueryHandler : IRequestHandler<GetCurrenciesQuery, List<CurrencyDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCurrenciesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CurrencyDto>> Handle(GetCurrenciesQuery request, CancellationToken cancellationToken)
        => await _db.Currencies
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Symbol = c.Symbol,
                ExchangeRate = c.ExchangeRate,
                IsBaseCurrency = c.IsBaseCurrency,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);
}
