using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Operational.BusinessPartners;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class BusinessPartnerDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public BusinessPartnerType PartnerType { get; set; }
    public string PartnerTypeName => PartnerType.ToString();
    public bool IsActive { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public decimal? CreditLimit { get; set; }
    public string? CurrencyCode { get; set; }
    public string? ReceivableAccountCode { get; set; }
    public string? PayableAccountCode { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Queries ───────────────────────────────────────────────────────────────────

public class GetBusinessPartnersQuery : IRequest<List<BusinessPartnerDto>>
{
    public BusinessPartnerType? PartnerType { get; set; }
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
}

public class GetBusinessPartnersQueryHandler : IRequestHandler<GetBusinessPartnersQuery, List<BusinessPartnerDto>>
{
    private readonly IApplicationDbContext _db;
    public GetBusinessPartnersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<BusinessPartnerDto>> Handle(GetBusinessPartnersQuery request, CancellationToken ct)
    {
        var query = _db.BusinessPartners
            .Include(bp => bp.Currency)
            .Include(bp => bp.ReceivableAccount)
            .Include(bp => bp.PayableAccount)
            .Where(bp => !bp.IsDeleted);

        if (request.PartnerType.HasValue)
            query = query.Where(bp => bp.PartnerType == request.PartnerType.Value
                || bp.PartnerType == BusinessPartnerType.Both);

        if (request.IsActive.HasValue)
            query = query.Where(bp => bp.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(bp => bp.Name.ToLower().Contains(s)
                || bp.Code.ToLower().Contains(s)
                || (bp.Email != null && bp.Email.ToLower().Contains(s))
                || (bp.Phone != null && bp.Phone.Contains(s)));
        }

        return await query
            .OrderBy(bp => bp.Name)
            .Select(bp => new BusinessPartnerDto
            {
                Id = bp.Id,
                Code = bp.Code,
                Name = bp.Name,
                NameAr = bp.NameAr,
                PartnerType = bp.PartnerType,
                IsActive = bp.IsActive,
                TaxNumber = bp.TaxNumber,
                Phone = bp.Phone,
                Email = bp.Email,
                Address = bp.Address,
                Notes = bp.Notes,
                CreditLimit = bp.CreditLimit,
                CurrencyCode = bp.Currency != null ? bp.Currency.Code : null,
                ReceivableAccountCode = bp.ReceivableAccount != null ? bp.ReceivableAccount.Code : null,
                PayableAccountCode = bp.PayableAccount != null ? bp.PayableAccount.Code : null,
                CreatedAt = bp.CreatedAt
            })
            .ToListAsync(ct);
    }
}

public class GetBusinessPartnerQuery : IRequest<BusinessPartnerDto>
{
    public Guid Id { get; set; }
}

public class GetBusinessPartnerQueryHandler : IRequestHandler<GetBusinessPartnerQuery, BusinessPartnerDto>
{
    private readonly IApplicationDbContext _db;
    public GetBusinessPartnerQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<BusinessPartnerDto> Handle(GetBusinessPartnerQuery request, CancellationToken ct)
    {
        var bp = await _db.BusinessPartners
            .Include(b => b.Currency)
            .Include(b => b.ReceivableAccount)
            .Include(b => b.PayableAccount)
            .Where(b => b.Id == request.Id && !b.IsDeleted)
            .Select(b => new BusinessPartnerDto
            {
                Id = b.Id, Code = b.Code, Name = b.Name, NameAr = b.NameAr,
                PartnerType = b.PartnerType, IsActive = b.IsActive,
                TaxNumber = b.TaxNumber, Phone = b.Phone, Email = b.Email,
                Address = b.Address, Notes = b.Notes, CreditLimit = b.CreditLimit,
                CurrencyCode = b.Currency != null ? b.Currency.Code : null,
                ReceivableAccountCode = b.ReceivableAccount != null ? b.ReceivableAccount.Code : null,
                PayableAccountCode = b.PayableAccount != null ? b.PayableAccount.Code : null,
                CreatedAt = b.CreatedAt
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(BusinessPartner), request.Id);

        return bp;
    }
}

// ── Commands ──────────────────────────────────────────────────────────────────

public class UpsertBusinessPartnerCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public BusinessPartnerType PartnerType { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public decimal? CreditLimit { get; set; }
    public Guid? CurrencyId { get; set; }
    public Guid? ReceivableAccountId { get; set; }
    public Guid? PayableAccountId { get; set; }
}

public class UpsertBusinessPartnerCommandValidator : AbstractValidator<UpsertBusinessPartnerCommand>
{
    public UpsertBusinessPartnerCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0).When(x => x.CreditLimit.HasValue);
    }
}

public class UpsertBusinessPartnerCommandHandler : IRequestHandler<UpsertBusinessPartnerCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public UpsertBusinessPartnerCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<Guid> Handle(UpsertBusinessPartnerCommand request, CancellationToken ct)
    {
        BusinessPartner? bp = null;

        if (request.Id.HasValue)
            bp = await _db.BusinessPartners
                .FirstOrDefaultAsync(b => b.Id == request.Id.Value && !b.IsDeleted, ct);

        if (bp is null)
        {
            var exists = await _db.BusinessPartners
                .AnyAsync(b => b.Code == request.Code && !b.IsDeleted, ct);
            if (exists) throw new InvalidOperationException($"Business partner with code '{request.Code}' already exists.");

            bp = new BusinessPartner { Code = request.Code, CreatedBy = _user.UserId };
            _db.BusinessPartners.Add(bp);
        }
        else
        {
            bp.ModifiedAt = DateTime.UtcNow;
            bp.ModifiedBy = _user.UserId;
        }

        bp.Name = request.Name.Trim();
        bp.NameAr = request.NameAr?.Trim();
        bp.PartnerType = request.PartnerType;
        bp.TaxNumber = request.TaxNumber?.Trim();
        bp.Phone = request.Phone?.Trim();
        bp.Email = request.Email?.Trim();
        bp.Address = request.Address?.Trim();
        bp.Notes = request.Notes?.Trim();
        bp.CreditLimit = request.CreditLimit;
        bp.CurrencyId = request.CurrencyId;
        bp.ReceivableAccountId = request.ReceivableAccountId;
        bp.PayableAccountId = request.PayableAccountId;

        await _db.SaveChangesAsync(ct);
        return bp.Id;
    }
}

public class ToggleBusinessPartnerStatusCommand : IRequest
{
    public Guid Id { get; set; }
}

public class ToggleBusinessPartnerStatusCommandHandler : IRequestHandler<ToggleBusinessPartnerStatusCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    public ToggleBusinessPartnerStatusCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task Handle(ToggleBusinessPartnerStatusCommand request, CancellationToken ct)
    {
        var bp = await _db.BusinessPartners
            .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(BusinessPartner), request.Id);

        bp.IsActive = !bp.IsActive;
        bp.ModifiedAt = DateTime.UtcNow;
        bp.ModifiedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
    }
}
