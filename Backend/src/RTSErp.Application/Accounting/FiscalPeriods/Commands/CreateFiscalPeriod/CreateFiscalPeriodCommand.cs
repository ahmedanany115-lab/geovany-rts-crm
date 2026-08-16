using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.FiscalPeriods.Commands.CreateFiscalPeriod;

public class CreateFiscalPeriodCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class CreateFiscalPeriodCommandValidator : AbstractValidator<CreateFiscalPeriodCommand>
{
    public CreateFiscalPeriodCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty()
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");
    }
}

public class CreateFiscalPeriodCommandHandler : IRequestHandler<CreateFiscalPeriodCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateFiscalPeriodCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateFiscalPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = new FiscalPeriod
        {
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = FiscalPeriodStatus.Open,
            CreatedBy = _currentUser.UserId
        };

        _db.FiscalPeriods.Add(period);
        await _db.SaveChangesAsync(cancellationToken);
        return period.Id;
    }
}
