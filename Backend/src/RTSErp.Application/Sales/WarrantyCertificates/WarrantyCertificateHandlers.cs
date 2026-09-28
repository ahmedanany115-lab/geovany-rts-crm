using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Sales;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Sales.WarrantyCertificates;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public class WarrantyCertificateDto
{
    public Guid Id { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public DateOnly CertificateDate { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerContact { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public DateOnly WarrantyStartDate { get; set; }
    public DateOnly WarrantyEndDate { get; set; }
    public string? WarrantyTerms { get; set; }
    public string? Notes { get; set; }
    public WarrantyCertificateStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime CreatedAt { get; set; }
}

// ── Queries ───────────────────────────────────────────────────────────────────

public class GetWarrantyCertificatesQuery : IRequest<List<WarrantyCertificateDto>>
{
    public Guid? CustomerId { get; set; }
    public WarrantyCertificateStatus? Status { get; set; }
    public string? SerialNumber { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetWarrantyCertificatesQueryHandler : IRequestHandler<GetWarrantyCertificatesQuery, List<WarrantyCertificateDto>>
{
    private readonly IApplicationDbContext _db;
    public GetWarrantyCertificatesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<WarrantyCertificateDto>> Handle(GetWarrantyCertificatesQuery req, CancellationToken ct)
    {
        var q = _db.WarrantyCertificates.Where(c => !c.IsDeleted);
        if (req.CustomerId.HasValue)               q = q.Where(c => c.CustomerId == req.CustomerId.Value);
        if (req.Status.HasValue)                   q = q.Where(c => c.Status == req.Status.Value);
        if (!string.IsNullOrWhiteSpace(req.SerialNumber))
            q = q.Where(c => c.SerialNumber != null && c.SerialNumber.Contains(req.SerialNumber));

        var skip = (req.Page - 1) * req.PageSize;
        return await q
            .OrderByDescending(c => c.CertificateDate)
            .ThenByDescending(c => c.CreatedAt)
            .Skip(skip).Take(req.PageSize)
            .Select(c => new WarrantyCertificateDto
            {
                Id = c.Id, CertificateNumber = c.CertificateNumber, CertificateDate = c.CertificateDate,
                CustomerId = c.CustomerId, CustomerName = c.CustomerName, CustomerContact = c.CustomerContact,
                ProductId = c.ProductId, ProductName = c.ProductName, SerialNumber = c.SerialNumber,
                Quantity = c.Quantity, InvoiceId = c.InvoiceId, SalesOrderId = c.SalesOrderId,
                WarrantyStartDate = c.WarrantyStartDate, WarrantyEndDate = c.WarrantyEndDate,
                WarrantyTerms = c.WarrantyTerms, Notes = c.Notes, Status = c.Status, CreatedAt = c.CreatedAt
            }).ToListAsync(ct);
    }
}

public class GetWarrantyCertificateQuery : IRequest<WarrantyCertificateDto>
{
    public Guid Id { get; set; }
}

public class GetWarrantyCertificateQueryHandler : IRequestHandler<GetWarrantyCertificateQuery, WarrantyCertificateDto>
{
    private readonly IApplicationDbContext _db;
    public GetWarrantyCertificateQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<WarrantyCertificateDto> Handle(GetWarrantyCertificateQuery req, CancellationToken ct)
    {
        var cert = await _db.WarrantyCertificates
            .FirstOrDefaultAsync(c => c.Id == req.Id && !c.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(WarrantyCertificate), req.Id);

        return new WarrantyCertificateDto
        {
            Id = cert.Id, CertificateNumber = cert.CertificateNumber, CertificateDate = cert.CertificateDate,
            CustomerId = cert.CustomerId, CustomerName = cert.CustomerName, CustomerContact = cert.CustomerContact,
            ProductId = cert.ProductId, ProductName = cert.ProductName, SerialNumber = cert.SerialNumber,
            Quantity = cert.Quantity, InvoiceId = cert.InvoiceId, SalesOrderId = cert.SalesOrderId,
            WarrantyStartDate = cert.WarrantyStartDate, WarrantyEndDate = cert.WarrantyEndDate,
            WarrantyTerms = cert.WarrantyTerms, Notes = cert.Notes, Status = cert.Status, CreatedAt = cert.CreatedAt
        };
    }
}

// ── Create ────────────────────────────────────────────────────────────────────

public class CreateWarrantyCertificateCommand : IRequest<CreateWarrantyCertificateResult>
{
    public DateOnly CertificateDate { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerContact { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public Guid? InvoiceId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public DateOnly WarrantyStartDate { get; set; }
    public DateOnly WarrantyEndDate { get; set; }
    public string? WarrantyTerms { get; set; }
    public string? Notes { get; set; }
}

public class CreateWarrantyCertificateResult
{
    public bool Succeeded { get; set; }
    public Guid CertificateId { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
}

public class CreateWarrantyCertificateCommandValidator : AbstractValidator<CreateWarrantyCertificateCommand>
{
    public CreateWarrantyCertificateCommandValidator()
    {
        RuleFor(x => x.WarrantyStartDate).NotEmpty();
        RuleFor(x => x.WarrantyEndDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(x => x.WarrantyStartDate)
            .WithMessage("Warranty end date must be on or after the start date.");
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class CreateWarrantyCertificateCommandHandler : IRequestHandler<CreateWarrantyCertificateCommand, CreateWarrantyCertificateResult>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public CreateWarrantyCertificateCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task<CreateWarrantyCertificateResult> Handle(CreateWarrantyCertificateCommand req, CancellationToken ct)
    {
        // Generate certificate number WC{year}-{seq:D5}
        var year = DateTime.UtcNow.Year;
        var prefix = $"WC{year}-";
        var last = await _db.WarrantyCertificates.IgnoreQueryFilters()
            .Where(c => c.CertificateNumber.StartsWith(prefix))
            .OrderByDescending(c => c.CertificateNumber)
            .Select(c => c.CertificateNumber)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;

        var cert = new WarrantyCertificate
        {
            CertificateNumber = $"{prefix}{seq:D5}",
            CertificateDate = req.CertificateDate,
            CustomerId = req.CustomerId,
            CustomerName = req.CustomerName?.Trim(),
            CustomerContact = req.CustomerContact?.Trim(),
            ProductId = req.ProductId,
            ProductName = req.ProductName?.Trim(),
            SerialNumber = req.SerialNumber?.Trim(),
            Quantity = req.Quantity,
            InvoiceId = req.InvoiceId,
            SalesOrderId = req.SalesOrderId,
            WarrantyStartDate = req.WarrantyStartDate,
            WarrantyEndDate = req.WarrantyEndDate,
            WarrantyTerms = req.WarrantyTerms?.Trim(),
            Notes = req.Notes?.Trim(),
            Status = WarrantyCertificateStatus.Active,
            CreatedBy = _user.UserId
        };

        _db.WarrantyCertificates.Add(cert);
        await _db.SaveChangesAsync(ct);

        return new CreateWarrantyCertificateResult
        {
            Succeeded = true,
            CertificateId = cert.Id,
            CertificateNumber = cert.CertificateNumber
        };
    }
}

// ── Update ────────────────────────────────────────────────────────────────────

public class UpdateWarrantyCertificateCommand : IRequest
{
    public Guid Id { get; set; }
    public DateOnly CertificateDate { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerContact { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public Guid? InvoiceId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public DateOnly WarrantyStartDate { get; set; }
    public DateOnly WarrantyEndDate { get; set; }
    public string? WarrantyTerms { get; set; }
    public string? Notes { get; set; }
    public WarrantyCertificateStatus Status { get; set; }
}

public class UpdateWarrantyCertificateCommandValidator : AbstractValidator<UpdateWarrantyCertificateCommand>
{
    public UpdateWarrantyCertificateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.WarrantyStartDate).NotEmpty();
        RuleFor(x => x.WarrantyEndDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(x => x.WarrantyStartDate)
            .WithMessage("Warranty end date must be on or after the start date.");
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class UpdateWarrantyCertificateCommandHandler : IRequestHandler<UpdateWarrantyCertificateCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public UpdateWarrantyCertificateCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task Handle(UpdateWarrantyCertificateCommand req, CancellationToken ct)
    {
        var cert = await _db.WarrantyCertificates
            .FirstOrDefaultAsync(c => c.Id == req.Id && !c.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(WarrantyCertificate), req.Id);

        if (cert.Status == WarrantyCertificateStatus.Voided)
            throw new InvalidOperationException("Voided certificates cannot be updated.");

        cert.CertificateDate = req.CertificateDate;
        cert.CustomerId = req.CustomerId;
        cert.CustomerName = req.CustomerName?.Trim();
        cert.CustomerContact = req.CustomerContact?.Trim();
        cert.ProductId = req.ProductId;
        cert.ProductName = req.ProductName?.Trim();
        cert.SerialNumber = req.SerialNumber?.Trim();
        cert.Quantity = req.Quantity;
        cert.InvoiceId = req.InvoiceId;
        cert.SalesOrderId = req.SalesOrderId;
        cert.WarrantyStartDate = req.WarrantyStartDate;
        cert.WarrantyEndDate = req.WarrantyEndDate;
        cert.WarrantyTerms = req.WarrantyTerms?.Trim();
        cert.Notes = req.Notes?.Trim();
        cert.Status = req.Status;
        cert.ModifiedAt = DateTime.UtcNow;
        cert.ModifiedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────

public class DeleteWarrantyCertificateCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteWarrantyCertificateCommandHandler : IRequestHandler<DeleteWarrantyCertificateCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public DeleteWarrantyCertificateCommandHandler(IApplicationDbContext db, ICurrentUserService user)
        => (_db, _user) = (db, user);

    public async Task Handle(DeleteWarrantyCertificateCommand req, CancellationToken ct)
    {
        var cert = await _db.WarrantyCertificates
            .FirstOrDefaultAsync(c => c.Id == req.Id && !c.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(WarrantyCertificate), req.Id);

        cert.IsDeleted = true;
        cert.ModifiedAt = DateTime.UtcNow;
        cert.ModifiedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
    }
}
