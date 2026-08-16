using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.FiscalPeriods.Queries;

public class FiscalPeriodDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public FiscalPeriodStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public bool IsClosed => Status == FiscalPeriodStatus.Closed;
    public DateTime CreatedAt { get; set; }
}

public class GetFiscalPeriodsQuery : IRequest<List<FiscalPeriodDto>> { }

public class GetFiscalPeriodsQueryHandler : IRequestHandler<GetFiscalPeriodsQuery, List<FiscalPeriodDto>>
{
    private readonly IApplicationDbContext _db;
    public GetFiscalPeriodsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<FiscalPeriodDto>> Handle(GetFiscalPeriodsQuery request, CancellationToken cancellationToken)
        => await _db.FiscalPeriods
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new FiscalPeriodDto
            {
                Id = p.Id,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);
}
