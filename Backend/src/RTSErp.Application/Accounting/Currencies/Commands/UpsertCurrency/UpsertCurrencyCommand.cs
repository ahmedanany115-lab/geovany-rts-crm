using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;

namespace RTSErp.Application.Accounting.Currencies.Commands.UpsertCurrency;

public class UpsertCurrencyCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; } = 1m;
    public bool IsActive { get; set; } = true;
}

public class UpsertCurrencyCommandValidator : AbstractValidator<UpsertCurrencyCommand>
{
    public UpsertCurrencyCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10)
            .Matches("^[A-Z]{3}$").WithMessage("Currency code must be 3 uppercase letters (e.g. EGP, USD).");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(10);
        RuleFor(x => x.ExchangeRate).GreaterThan(0)
            .WithMessage("Exchange rate must be greater than zero.");
    }
}

public class UpsertCurrencyCommandHandler : IRequestHandler<UpsertCurrencyCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpsertCurrencyCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(UpsertCurrencyCommand request, CancellationToken cancellationToken)
    {
        Currency? currency = null;

        if (request.Id.HasValue)
        {
            currency = await _db.Currencies
                .FirstOrDefaultAsync(c => c.Id == request.Id.Value && !c.IsDeleted, cancellationToken);
        }

        if (currency is null)
        {
            // Check code uniqueness on create
            var exists = await _db.Currencies
                .AnyAsync(c => c.Code == request.Code.ToUpperInvariant() && !c.IsDeleted, cancellationToken);
            if (exists)
                throw new InvalidOperationException($"Currency with code '{request.Code}' already exists.");

            currency = new Currency
            {
                Code = request.Code.ToUpperInvariant(),
                IsBaseCurrency = false,
                CreatedBy = _currentUser.UserId
            };
            _db.Currencies.Add(currency);
        }
        else
        {
            currency.ModifiedAt = DateTime.UtcNow;
            currency.ModifiedBy = _currentUser.UserId;
        }

        currency.Name = request.Name.Trim();
        currency.Symbol = request.Symbol.Trim();
        currency.ExchangeRate = request.ExchangeRate;
        currency.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return currency.Id;
    }
}
