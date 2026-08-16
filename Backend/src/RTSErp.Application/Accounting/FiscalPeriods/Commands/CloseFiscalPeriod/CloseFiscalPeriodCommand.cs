using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.FiscalPeriods.Commands.CloseFiscalPeriod;

public class CloseFiscalPeriodCommand : IRequest
{
    public Guid Id { get; set; }
}

public class CloseFiscalPeriodCommandHandler : IRequestHandler<CloseFiscalPeriodCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CloseFiscalPeriodCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(CloseFiscalPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _db.FiscalPeriods
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Accounting.FiscalPeriod), request.Id);

        if (period.Status == FiscalPeriodStatus.Closed)
            throw new InvalidOperationException("Fiscal period is already closed.");

        period.Status = FiscalPeriodStatus.Closed;
        period.ModifiedAt = DateTime.UtcNow;
        period.ModifiedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class OpenFiscalPeriodCommand : IRequest
{
    public Guid Id { get; set; }
}

public class OpenFiscalPeriodCommandHandler : IRequestHandler<OpenFiscalPeriodCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public OpenFiscalPeriodCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(OpenFiscalPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _db.FiscalPeriods
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Accounting.FiscalPeriod), request.Id);

        if (period.Status == FiscalPeriodStatus.Open)
            throw new InvalidOperationException("Fiscal period is already open.");

        period.Status = FiscalPeriodStatus.Open;
        period.ModifiedAt = DateTime.UtcNow;
        period.ModifiedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
