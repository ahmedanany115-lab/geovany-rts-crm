using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Exceptions;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Accounting.JournalEntries.Queries.GetJournalEntry;

public class JournalEntryLineDetailDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal DebitBase { get; set; }
    public decimal CreditBase { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class JournalEntryDetailDto
{
    public Guid Id { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public JournalEntryStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public string FiscalPeriodName { get; set; } = string.Empty;
    public ReferenceType ReferenceType { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid? ReversedByEntryId { get; set; }
    public Guid? ReversesEntryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<JournalEntryLineDetailDto> Lines { get; set; } = [];
    public decimal TotalDebit => Lines.Sum(l => l.Debit);
    public decimal TotalCredit => Lines.Sum(l => l.Credit);
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.001m;
}

public class GetJournalEntryQuery : IRequest<JournalEntryDetailDto>
{
    public Guid Id { get; set; }
}

public class GetJournalEntryQueryHandler : IRequestHandler<GetJournalEntryQuery, JournalEntryDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetJournalEntryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<JournalEntryDetailDto> Handle(GetJournalEntryQuery request, CancellationToken cancellationToken)
    {
        var entry = await _db.JournalEntries
            .Include(e => e.Currency)
            .Include(e => e.FiscalPeriod)
            .Include(e => e.Lines)
                .ThenInclude(l => l.Account)
            .Include(e => e.Lines)
                .ThenInclude(l => l.Currency)
            .FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Accounting.JournalEntry), request.Id);

        return new JournalEntryDetailDto
        {
            Id = entry.Id,
            EntryNumber = entry.EntryNumber,
            EntryDate = entry.EntryDate,
            Description = entry.Description,
            Status = entry.Status,
            CurrencyCode = entry.Currency.Code,
            ExchangeRate = entry.ExchangeRate,
            FiscalPeriodName = entry.FiscalPeriod.Name,
            ReferenceType = entry.ReferenceType,
            ReferenceNumber = entry.ReferenceNumber,
            ReversedByEntryId = entry.ReversedByEntryId,
            ReversesEntryId = entry.ReversesEntryId,
            CreatedAt = entry.CreatedAt,
            Lines = entry.Lines.OrderBy(l => l.SortOrder).Select(l => new JournalEntryLineDetailDto
            {
                Id = l.Id,
                AccountId = l.AccountId,
                AccountCode = l.Account.Code,
                AccountName = l.Account.Name,
                Debit = l.Debit,
                Credit = l.Credit,
                DebitBase = l.DebitBase,
                CreditBase = l.CreditBase,
                CurrencyCode = l.Currency.Code,
                ExchangeRate = l.ExchangeRate,
                Description = l.Description,
                SortOrder = l.SortOrder
            }).ToList()
        };
    }
}
