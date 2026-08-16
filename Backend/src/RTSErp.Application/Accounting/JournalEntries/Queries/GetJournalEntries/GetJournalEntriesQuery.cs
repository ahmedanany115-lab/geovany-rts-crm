using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.JournalEntries.Queries.GetJournalEntries;

public class JournalEntryListDto
{
    public Guid Id { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public JournalEntryStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public string FiscalPeriodName { get; set; } = string.Empty;
    public ReferenceType ReferenceType { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetJournalEntriesQuery : IRequest<List<JournalEntryListDto>>
{
    public JournalEntryStatus? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? FiscalPeriodId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetJournalEntriesQueryHandler : IRequestHandler<GetJournalEntriesQuery, List<JournalEntryListDto>>
{
    private readonly IApplicationDbContext _db;

    public GetJournalEntriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<JournalEntryListDto>> Handle(GetJournalEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.JournalEntries
            .Include(e => e.Currency)
            .Include(e => e.FiscalPeriod)
            .Include(e => e.Lines)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        if (request.FromDate.HasValue)
            query = query.Where(e => e.EntryDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(e => e.EntryDate <= request.ToDate.Value);

        if (request.FiscalPeriodId.HasValue)
            query = query.Where(e => e.FiscalPeriodId == request.FiscalPeriodId.Value);

        return await query
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.EntryNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new JournalEntryListDto
            {
                Id = e.Id,
                EntryNumber = e.EntryNumber,
                EntryDate = e.EntryDate,
                Description = e.Description,
                Status = e.Status,
                CurrencyCode = e.Currency.Code,
                ExchangeRate = e.ExchangeRate,
                TotalDebit = e.Lines.Sum(l => l.Debit),
                TotalCredit = e.Lines.Sum(l => l.Credit),
                FiscalPeriodName = e.FiscalPeriod.Name,
                ReferenceType = e.ReferenceType,
                ReferenceNumber = e.ReferenceNumber,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
