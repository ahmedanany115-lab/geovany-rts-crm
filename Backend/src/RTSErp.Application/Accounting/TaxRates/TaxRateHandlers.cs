using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;

namespace RTSErp.Application.Accounting.TaxRates;

public class TaxRateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsActive { get; set; }
}

public class GetTaxRatesQuery : IRequest<List<TaxRateDto>> { }

public class GetTaxRatesQueryHandler : IRequestHandler<GetTaxRatesQuery, List<TaxRateDto>>
{
    private readonly IApplicationDbContext _db;
    public GetTaxRatesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<TaxRateDto>> Handle(GetTaxRatesQuery request, CancellationToken ct)
        => await _db.TaxRates
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Code)
            .Select(t => new TaxRateDto
            {
                Id = t.Id, Code = t.Code, Name = t.Name,
                Rate = t.Rate, IsActive = t.IsActive
            })
            .ToListAsync(ct);
}
