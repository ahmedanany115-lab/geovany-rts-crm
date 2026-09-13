using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Common;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Maintenance;
using RTSErp.Domain.Enums;

namespace RTSErp.Application.Maintenance;

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════════════════════

public class MaintenanceContractDto
{
    public Guid    Id              { get; set; }
    public string  ContractNumber  { get; set; } = string.Empty;
    public Guid    CustomerId      { get; set; }
    public string  CustomerName    { get; set; } = string.Empty;
    public DateOnly StartDate      { get; set; }
    public DateOnly EndDate        { get; set; }
    public int     TotalVisitsPerQuarter { get; set; }
    public decimal ContractValue   { get; set; }
    public string  CurrencyCode    { get; set; } = string.Empty;
    public ContractStatus Status   { get; set; }
    public string  StatusName      => Status.ToString();
    public string? Notes           { get; set; }
    public int     TotalQuarters   { get; set; }
    public int     ActiveQuarter   { get; set; }
    public int     RemainingVisitsThisQuarter { get; set; }
    public DateTime CreatedAt      { get; set; }
}

public class ContractQuarterDto
{
    public Guid    Id              { get; set; }
    public Guid    ContractId      { get; set; }
    public int     QuarterNumber   { get; set; }
    public DateOnly StartDate      { get; set; }
    public DateOnly EndDate        { get; set; }
    public int     AllocatedVisits { get; set; }
    public int     UsedVisits      { get; set; }
    public int     RemainingVisits { get; set; }
    public QuarterStatus Status    { get; set; }
    public string  StatusName      => Status.ToString();
    public decimal QuarterValue    { get; set; }
    public List<MaintenanceVisitDto> Visits { get; set; } = [];
}

public class MaintenanceVisitDto
{
    public Guid    Id              { get; set; }
    public Guid    QuarterId       { get; set; }
    public int     QuarterNumber   { get; set; }
    public Guid    ContractId      { get; set; }
    public string  ContractNumber  { get; set; } = string.Empty;
    public string  CustomerName    { get; set; } = string.Empty;
    public DateOnly ScheduledDate  { get; set; }
    public DateOnly? ActualDate    { get; set; }
    public string? TechnicianName  { get; set; }
    public VisitStatus Status      { get; set; }
    public string  StatusName      => Status.ToString();
    public string? WorkDescription { get; set; }
    public decimal ExtraChargesAmount { get; set; }
    public string? ExtraChargesNotes  { get; set; }
    public string? Notes           { get; set; }
    public DateTime CreatedAt      { get; set; }
}

public class ContractEquipmentDto
{
    public Guid    Id           { get; set; }
    public Guid    ContractId   { get; set; }
    public string  ItemName     { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Brand        { get; set; }
    public string? Model        { get; set; }
    public string? Location     { get; set; }
    public DateOnly? InstallDate { get; set; }
    public string? Notes        { get; set; }
    public string? ProductName  { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
// QUERIES
// ═══════════════════════════════════════════════════════════════════════════════

// ── List contracts ────────────────────────────────────────────────────────────
public class GetMaintenanceContractsQuery : IRequest<List<MaintenanceContractDto>>
{
    public ContractStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Search { get; set; }
}

public class GetMaintenanceContractsHandler
    : IRequestHandler<GetMaintenanceContractsQuery, List<MaintenanceContractDto>>
{
    private readonly IApplicationDbContext _db;
    public GetMaintenanceContractsHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<MaintenanceContractDto>> Handle(
        GetMaintenanceContractsQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var q = _db.MaintenanceContracts
            .Include(c => c.Customer)
            .Include(c => c.Currency)
            .Include(c => c.Quarters)
                .ThenInclude(q => q.Visits)
            .Where(c => !c.IsDeleted);

        if (request.Status.HasValue) q = q.Where(c => c.Status == request.Status.Value);
        if (request.CustomerId.HasValue) q = q.Where(c => c.CustomerId == request.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            q = q.Where(c => c.ContractNumber.ToLower().Contains(s) ||
                              c.Customer.Name.ToLower().Contains(s));
        }

        var contracts = await q.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);

        return contracts.Select(c =>
        {
            var activeQ = c.Quarters.FirstOrDefault(q =>
                q.StartDate <= today && q.EndDate >= today && !q.IsDeleted);
            return new MaintenanceContractDto
            {
                Id = c.Id, ContractNumber = c.ContractNumber,
                CustomerId = c.CustomerId, CustomerName = c.Customer.Name,
                StartDate = c.StartDate, EndDate = c.EndDate,
                TotalVisitsPerQuarter = c.TotalVisitsPerQuarter,
                ContractValue = c.ContractValue,
                CurrencyCode = c.Currency?.Code ?? "EGP",
                Status = c.Status, Notes = c.Notes,
                TotalQuarters = c.Quarters.Count(q => !q.IsDeleted),
                ActiveQuarter = activeQ?.QuarterNumber ?? 0,
                RemainingVisitsThisQuarter = activeQ?.RemainingVisits ?? 0,
                CreatedAt = c.CreatedAt,
            };
        }).ToList();
    }
}

// ── Get single contract with quarters & equipment ─────────────────────────────
public class GetMaintenanceContractQuery : IRequest<object>
{
    public Guid Id { get; set; }
}

public class GetMaintenanceContractHandler
    : IRequestHandler<GetMaintenanceContractQuery, object>
{
    private readonly IApplicationDbContext _db;
    public GetMaintenanceContractHandler(IApplicationDbContext db) => _db = db;

    public async Task<object> Handle(GetMaintenanceContractQuery request, CancellationToken ct)
    {
        var c = await _db.MaintenanceContracts
            .Include(x => x.Customer)
            .Include(x => x.Currency)
            .Include(x => x.Quarters.Where(q => !q.IsDeleted))
                .ThenInclude(q => q.Visits.Where(v => !v.IsDeleted))
            .Include(x => x.Equipment.Where(e => !e.IsDeleted))
                .ThenInclude(e => e.Product)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException($"Contract {request.Id} not found");

        return new
        {
            c.Id, c.ContractNumber, c.CustomerId,
            customerName = c.Customer.Name,
            c.StartDate, c.EndDate, c.TotalVisitsPerQuarter,
            c.ContractValue, currencyCode = c.Currency?.Code ?? "EGP",
            c.Status, statusName = c.Status.ToString(), c.Notes,
            quarters = c.Quarters
                .OrderBy(q => q.QuarterNumber)
                .Select(q => new ContractQuarterDto
                {
                    Id = q.Id, ContractId = q.ContractId,
                    QuarterNumber = q.QuarterNumber,
                    StartDate = q.StartDate, EndDate = q.EndDate,
                    AllocatedVisits = q.AllocatedVisits, UsedVisits = q.UsedVisits,
                    RemainingVisits = q.RemainingVisits,
                    Status = q.Status, QuarterValue = q.QuarterValue,
                    Visits = q.Visits.OrderBy(v => v.ScheduledDate)
                        .Select(v => MapVisit(v, q, c)).ToList()
                }),
            equipment = c.Equipment.Select(e => new ContractEquipmentDto
            {
                Id = e.Id, ContractId = e.ContractId,
                ItemName = e.ItemName, SerialNumber = e.SerialNumber,
                Brand = e.Brand, Model = e.Model,
                Location = e.Location, InstallDate = e.InstallDate,
                Notes = e.Notes, ProductName = e.Product?.Name
            })
        };
    }

    private static MaintenanceVisitDto MapVisit(
        MaintenanceVisit v, ContractQuarter q, MaintenanceContract c) => new()
    {
        Id = v.Id, QuarterId = v.QuarterId,
        QuarterNumber = q.QuarterNumber, ContractId = c.Id,
        ContractNumber = c.ContractNumber, CustomerName = c.Customer.Name,
        ScheduledDate = v.ScheduledDate, ActualDate = v.ActualDate,
        TechnicianName = v.TechnicianName, Status = v.Status,
        WorkDescription = v.WorkDescription,
        ExtraChargesAmount = v.ExtraChargesAmount,
        ExtraChargesNotes = v.ExtraChargesNotes,
        Notes = v.Notes, CreatedAt = v.CreatedAt,
    };
}

// ── List visits ───────────────────────────────────────────────────────────────
public class GetMaintenanceVisitsQuery : IRequest<List<MaintenanceVisitDto>>
{
    public Guid? ContractId { get; set; }
    public VisitStatus? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate   { get; set; }
}

public class GetMaintenanceVisitsHandler
    : IRequestHandler<GetMaintenanceVisitsQuery, List<MaintenanceVisitDto>>
{
    private readonly IApplicationDbContext _db;
    public GetMaintenanceVisitsHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<MaintenanceVisitDto>> Handle(
        GetMaintenanceVisitsQuery request, CancellationToken ct)
    {
        var q = _db.MaintenanceVisits
            .Include(v => v.Quarter)
                .ThenInclude(q => q.Contract)
                    .ThenInclude(c => c.Customer)
            .Where(v => !v.IsDeleted);

        if (request.ContractId.HasValue)
            q = q.Where(v => v.Quarter.ContractId == request.ContractId.Value);
        if (request.Status.HasValue) q = q.Where(v => v.Status == request.Status.Value);
        if (request.FromDate.HasValue) q = q.Where(v => v.ScheduledDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)   q = q.Where(v => v.ScheduledDate <= request.ToDate.Value);

        var visits = await q.OrderByDescending(v => v.ScheduledDate).ToListAsync(ct);

        return visits.Select(v => new MaintenanceVisitDto
        {
            Id = v.Id, QuarterId = v.QuarterId,
            QuarterNumber = v.Quarter.QuarterNumber,
            ContractId = v.Quarter.ContractId,
            ContractNumber = v.Quarter.Contract.ContractNumber,
            CustomerName = v.Quarter.Contract.Customer.Name,
            ScheduledDate = v.ScheduledDate, ActualDate = v.ActualDate,
            TechnicianName = v.TechnicianName, Status = v.Status,
            WorkDescription = v.WorkDescription,
            ExtraChargesAmount = v.ExtraChargesAmount,
            ExtraChargesNotes = v.ExtraChargesNotes,
            Notes = v.Notes, CreatedAt = v.CreatedAt,
        }).ToList();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// COMMANDS
// ═══════════════════════════════════════════════════════════════════════════════

// ── Create contract ───────────────────────────────────────────────────────────
public class CreateMaintenanceContractCommand : IRequest<Guid>
{
    public Guid    CustomerId             { get; set; }
    public DateOnly StartDate             { get; set; }
    public DateOnly EndDate               { get; set; }
    public int     TotalVisitsPerQuarter  { get; set; } = 1;
    public Guid    CurrencyId             { get; set; }
    public decimal ContractValue          { get; set; }
    public Guid?   RevenueAccountId       { get; set; }
    public Guid?   ReceivableAccountId    { get; set; }
    public Guid?   DeferredRevenueAccountId { get; set; }
    public string? Notes                  { get; set; }
    public bool    PostAccountingEntries  { get; set; } = true;
}

public class CreateMaintenanceContractValidator
    : AbstractValidator<CreateMaintenanceContractCommand>
{
    public CreateMaintenanceContractValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
        RuleFor(x => x.TotalVisitsPerQuarter).InclusiveBetween(1, 12);
        RuleFor(x => x.ContractValue).GreaterThanOrEqualTo(0);
    }
}

public class CreateMaintenanceContractHandler
    : IRequestHandler<CreateMaintenanceContractCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IAccountingService    _accounting;
    private readonly ICurrentUserService   _currentUser;

    public CreateMaintenanceContractHandler(
        IApplicationDbContext db,
        IAccountingService accounting,
        ICurrentUserService currentUser)
    {
        _db = db; _accounting = accounting; _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateMaintenanceContractCommand cmd, CancellationToken ct)
    {
        // Generate contract number
        var count = await _db.MaintenanceContracts.CountAsync(ct);
        var number = $"MC-{DateTime.UtcNow.Year}-{(count + 1):D4}";

        var contract = new MaintenanceContract
        {
            ContractNumber            = number,
            CustomerId                = cmd.CustomerId,
            StartDate                 = cmd.StartDate,
            EndDate                   = cmd.EndDate,
            TotalVisitsPerQuarter     = cmd.TotalVisitsPerQuarter,
            CurrencyId                = cmd.CurrencyId,
            ContractValue             = cmd.ContractValue,
            RevenueAccountId          = cmd.RevenueAccountId,
            ReceivableAccountId       = cmd.ReceivableAccountId,
            DeferredRevenueAccountId  = cmd.DeferredRevenueAccountId,
            Status                    = ContractStatus.Draft,
            Notes                     = cmd.Notes,
            CreatedBy                 = _currentUser.UserId,
        };

        // Auto-generate 4 quarters spanning the contract period
        var span = (cmd.EndDate.DayNumber - cmd.StartDate.DayNumber) / 4;
        for (var i = 0; i < 4; i++)
        {
            var qStart = cmd.StartDate.AddDays(i * span);
            var qEnd   = i < 3 ? cmd.StartDate.AddDays((i + 1) * span - 1) : cmd.EndDate;
            contract.Quarters.Add(new ContractQuarter
            {
                QuarterNumber    = i + 1,
                StartDate        = qStart,
                EndDate          = qEnd,
                AllocatedVisits  = cmd.TotalVisitsPerQuarter,
                QuarterValue     = Math.Round(cmd.ContractValue / 4m, 4),
                Status           = QuarterStatus.Upcoming,
                CreatedBy        = _currentUser.UserId,
            });
        }

        _db.MaintenanceContracts.Add(contract);
        await _db.SaveChangesAsync(ct);

        // ── Accounting: Dr AR / Cr Deferred Revenue (contract value up-front) ──
        if (cmd.PostAccountingEntries
            && cmd.ContractValue > 0
            && cmd.ReceivableAccountId.HasValue
            && cmd.DeferredRevenueAccountId.HasValue)
        {
            var je = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
            {
                EntryDate       = cmd.StartDate,
                Description     = $"Maintenance contract {number} — {cmd.ContractValue:N2} recognised",
                ReferenceType   = ReferenceType.MaintenanceContract,
                ReferenceId     = contract.Id,
                ReferenceNumber = number,
                CurrencyId      = cmd.CurrencyId,
                ExchangeRate    = 1m,
                PostImmediately = true,
                CreatedBy       = _currentUser.UserId,
                Lines =
                [
                    new() { AccountId = cmd.ReceivableAccountId.Value,      Debit  = cmd.ContractValue, SortOrder = 1 },
                    new() { AccountId = cmd.DeferredRevenueAccountId.Value, Credit = cmd.ContractValue, SortOrder = 2 },
                ]
            }, ct);

            if (je.Succeeded)
            {
                contract.InitialJournalEntryId = je.EntryId;
                await _db.SaveChangesAsync(ct);
            }
        }

        return contract.Id;
    }
}

// ── Activate / change contract status ────────────────────────────────────────
public class ChangeContractStatusCommand : IRequest<Unit>
{
    public Guid           ContractId { get; set; }
    public ContractStatus NewStatus  { get; set; }
}

public class ChangeContractStatusHandler
    : IRequestHandler<ChangeContractStatusCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IAccountingService    _accounting;
    private readonly ICurrentUserService   _currentUser;

    public ChangeContractStatusHandler(
        IApplicationDbContext db, IAccountingService accounting,
        ICurrentUserService currentUser)
    { _db = db; _accounting = accounting; _currentUser = currentUser; }

    public async Task<Unit> Handle(ChangeContractStatusCommand cmd, CancellationToken ct)
    {
        var contract = await _db.MaintenanceContracts
            .Include(c => c.Quarters)
            .FirstOrDefaultAsync(c => c.Id == cmd.ContractId && !c.IsDeleted, ct)
            ?? throw new KeyNotFoundException($"Contract {cmd.ContractId} not found");

        contract.Status     = cmd.NewStatus;
        contract.ModifiedAt = DateTime.UtcNow;
        contract.ModifiedBy = _currentUser.UserId;

        // On activation — activate the first quarter
        if (cmd.NewStatus == ContractStatus.Active)
        {
            var first = contract.Quarters
                .Where(q => !q.IsDeleted)
                .OrderBy(q => q.QuarterNumber)
                .FirstOrDefault();
            if (first is not null && first.Status == QuarterStatus.Upcoming)
            {
                first.Status     = QuarterStatus.Active;
                first.ModifiedAt = DateTime.UtcNow;
                first.ModifiedBy = _currentUser.UserId;

                // Revenue recognition for Q1: Dr DeferredRevenue / Cr Revenue
                await RecogniseQuarterRevenueAsync(contract, first, ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private async Task RecogniseQuarterRevenueAsync(
        MaintenanceContract contract,
        ContractQuarter     quarter,
        CancellationToken   ct)
    {
        if (quarter.QuarterValue <= 0) return;
        if (!contract.DeferredRevenueAccountId.HasValue) return;
        if (!contract.RevenueAccountId.HasValue) return;

        var je = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            EntryDate       = quarter.StartDate,
            Description     = $"{contract.ContractNumber} — Q{quarter.QuarterNumber} revenue recognition",
            ReferenceType   = ReferenceType.QuarterRecognition,
            ReferenceId     = quarter.Id,
            ReferenceNumber = $"{contract.ContractNumber}/Q{quarter.QuarterNumber}",
            CurrencyId      = contract.CurrencyId,
            ExchangeRate    = 1m,
            PostImmediately = true,
            CreatedBy       = _currentUser.UserId,
            Lines =
            [
                new() { AccountId = contract.DeferredRevenueAccountId.Value, Debit  = quarter.QuarterValue, SortOrder = 1 },
                new() { AccountId = contract.RevenueAccountId.Value,         Credit = quarter.QuarterValue, SortOrder = 2 },
            ]
        }, ct);

        if (je.Succeeded)
        {
            quarter.RevenueJournalEntryId = je.EntryId;
        }
    }
}

// ── Schedule visit ────────────────────────────────────────────────────────────
public class ScheduleVisitCommand : IRequest<Guid>
{
    public Guid    QuarterId      { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public string? TechnicianName { get; set; }
    public string? Notes          { get; set; }
}

public class ScheduleVisitHandler : IRequestHandler<ScheduleVisitCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _currentUser;

    public ScheduleVisitHandler(IApplicationDbContext db, ICurrentUserService u)
    { _db = db; _currentUser = u; }

    public async Task<Guid> Handle(ScheduleVisitCommand cmd, CancellationToken ct)
    {
        var quarter = await _db.ContractQuarters
            .Include(q => q.Visits.Where(v => !v.IsDeleted))
            .FirstOrDefaultAsync(q => q.Id == cmd.QuarterId && !q.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Quarter not found");

        if (quarter.RemainingVisits <= 0)
            throw new InvalidOperationException(
                "No remaining visits in this quarter. Cannot schedule additional visits.");

        var visit = new MaintenanceVisit
        {
            QuarterId      = cmd.QuarterId,
            ScheduledDate  = cmd.ScheduledDate,
            TechnicianName = cmd.TechnicianName,
            Notes          = cmd.Notes,
            Status         = VisitStatus.Scheduled,
            CreatedBy      = _currentUser.UserId,
        };

        _db.MaintenanceVisits.Add(visit);
        await _db.SaveChangesAsync(ct);
        return visit.Id;
    }
}

// ── Complete visit ────────────────────────────────────────────────────────────
public class CompleteVisitCommand : IRequest<Unit>
{
    public Guid    VisitId             { get; set; }
    public DateOnly ActualDate         { get; set; }
    public string? WorkDescription     { get; set; }
    public string? CustomerFeedback    { get; set; }
    public decimal ExtraChargesAmount  { get; set; }
    public string? ExtraChargesNotes   { get; set; }
    public Guid?   ExtraChargesAccountId    { get; set; }  // Revenue account for extras
    public Guid?   ExtraChargesReceivableId { get; set; }  // AR account for extras
}

public class CompleteVisitHandler : IRequestHandler<CompleteVisitCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IAccountingService    _accounting;
    private readonly ICurrentUserService   _currentUser;

    public CompleteVisitHandler(
        IApplicationDbContext db, IAccountingService accounting,
        ICurrentUserService currentUser)
    { _db = db; _accounting = accounting; _currentUser = currentUser; }

    public async Task<Unit> Handle(CompleteVisitCommand cmd, CancellationToken ct)
    {
        var visit = await _db.MaintenanceVisits
            .Include(v => v.Quarter)
                .ThenInclude(q => q.Contract)
            .FirstOrDefaultAsync(v => v.Id == cmd.VisitId && !v.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Visit not found");

        if (visit.Status == VisitStatus.Completed)
            throw new InvalidOperationException("Visit is already completed.");

        visit.Status          = VisitStatus.Completed;
        visit.ActualDate      = cmd.ActualDate;
        visit.WorkDescription = cmd.WorkDescription;
        visit.CustomerFeedback = cmd.CustomerFeedback;
        visit.ExtraChargesAmount = cmd.ExtraChargesAmount;
        visit.ExtraChargesNotes  = cmd.ExtraChargesNotes;
        visit.ModifiedAt         = DateTime.UtcNow;
        visit.ModifiedBy         = _currentUser.UserId;

        // Consume one visit from the quarter
        var quarter = visit.Quarter;
        quarter.UsedVisits++;
        quarter.ModifiedAt = DateTime.UtcNow;
        quarter.ModifiedBy = _currentUser.UserId;

        // Check if quarter is now fully used — auto-complete it
        if (quarter.UsedVisits >= quarter.AllocatedVisits)
        {
            quarter.Status = QuarterStatus.Completed;

            // Activate next quarter and recognise its revenue
            var nextQ = await _db.ContractQuarters
                .FirstOrDefaultAsync(q =>
                    q.ContractId == quarter.ContractId &&
                    q.QuarterNumber == quarter.QuarterNumber + 1 &&
                    !q.IsDeleted, ct);

            if (nextQ is not null && nextQ.Status == QuarterStatus.Upcoming)
            {
                nextQ.Status     = QuarterStatus.Active;
                nextQ.ModifiedAt = DateTime.UtcNow;
                nextQ.ModifiedBy = _currentUser.UserId;

                // Revenue recognition for next quarter
                var contract = quarter.Contract;
                if (contract.DeferredRevenueAccountId.HasValue &&
                    contract.RevenueAccountId.HasValue &&
                    nextQ.QuarterValue > 0)
                {
                    var je = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
                    {
                        EntryDate       = nextQ.StartDate,
                        Description     = $"{contract.ContractNumber} — Q{nextQ.QuarterNumber} revenue recognition",
                        ReferenceType   = ReferenceType.QuarterRecognition,
                        ReferenceId     = nextQ.Id,
                        ReferenceNumber = $"{contract.ContractNumber}/Q{nextQ.QuarterNumber}",
                        CurrencyId      = contract.CurrencyId,
                        ExchangeRate    = 1m,
                        PostImmediately = true,
                        CreatedBy       = _currentUser.UserId,
                        Lines =
                        [
                            new() { AccountId = contract.DeferredRevenueAccountId.Value, Debit  = nextQ.QuarterValue, SortOrder = 1 },
                            new() { AccountId = contract.RevenueAccountId.Value,         Credit = nextQ.QuarterValue, SortOrder = 2 },
                        ]
                    }, ct);
                    if (je.Succeeded) nextQ.RevenueJournalEntryId = je.EntryId;
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        // ── Extra charges → JE: Dr AR / Cr Extra Revenue ──────────────────────
        if (cmd.ExtraChargesAmount > 0
            && cmd.ExtraChargesAccountId.HasValue
            && cmd.ExtraChargesReceivableId.HasValue)
        {
            var contract = quarter.Contract;
            var je = await _accounting.CreateJournalEntryAsync(new CreateJournalEntryRequest
            {
                EntryDate       = cmd.ActualDate,
                Description     = $"Visit extra charges — {contract.ContractNumber} / {cmd.ActualDate}",
                ReferenceType   = ReferenceType.MaintenanceVisit,
                ReferenceId     = visit.Id,
                ReferenceNumber = visit.Id.ToString("N")[..8].ToUpper(),
                CurrencyId      = contract.CurrencyId,
                ExchangeRate    = 1m,
                PostImmediately = true,
                CreatedBy       = _currentUser.UserId,
                Lines =
                [
                    new() { AccountId = cmd.ExtraChargesReceivableId.Value, Debit  = cmd.ExtraChargesAmount, SortOrder = 1,
                            Description = cmd.ExtraChargesNotes },
                    new() { AccountId = cmd.ExtraChargesAccountId.Value,    Credit = cmd.ExtraChargesAmount, SortOrder = 2,
                            Description = cmd.ExtraChargesNotes },
                ]
            }, ct);

            if (je.Succeeded)
            {
                visit.ExtraChargesJournalEntryId = je.EntryId;
                await _db.SaveChangesAsync(ct);
            }
        }

        return Unit.Value;
    }
}

// ── Cancel visit ──────────────────────────────────────────────────────────────
public class CancelVisitCommand : IRequest<Unit>
{
    public Guid   VisitId { get; set; }
    public string? Reason { get; set; }
}

public class CancelVisitHandler : IRequestHandler<CancelVisitCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _currentUser;

    public CancelVisitHandler(IApplicationDbContext db, ICurrentUserService u)
    { _db = db; _currentUser = u; }

    public async Task<Unit> Handle(CancelVisitCommand cmd, CancellationToken ct)
    {
        var visit = await _db.MaintenanceVisits
            .Include(v => v.Quarter)
            .FirstOrDefaultAsync(v => v.Id == cmd.VisitId && !v.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Visit not found");

        if (visit.Status == VisitStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed visit.");

        visit.Status     = VisitStatus.Cancelled;
        visit.Notes      = string.IsNullOrWhiteSpace(cmd.Reason) ? visit.Notes
                         : $"{visit.Notes}\nCancelled: {cmd.Reason}".Trim();
        visit.ModifiedAt = DateTime.UtcNow;
        visit.ModifiedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── Manage equipment ──────────────────────────────────────────────────────────
public class UpsertEquipmentCommand : IRequest<Guid>
{
    public Guid?   Id           { get; set; }
    public Guid    ContractId   { get; set; }
    public string  ItemName     { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Brand        { get; set; }
    public string? Model        { get; set; }
    public string? Location     { get; set; }
    public DateOnly? InstallDate { get; set; }
    public Guid?   ProductId    { get; set; }
    public string? Notes        { get; set; }
}

public class UpsertEquipmentHandler : IRequestHandler<UpsertEquipmentCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _currentUser;

    public UpsertEquipmentHandler(IApplicationDbContext db, ICurrentUserService u)
    { _db = db; _currentUser = u; }

    public async Task<Guid> Handle(UpsertEquipmentCommand cmd, CancellationToken ct)
    {
        ContractEquipment eq;
        if (cmd.Id.HasValue)
        {
            eq = await _db.ContractEquipments
                .FirstOrDefaultAsync(e => e.Id == cmd.Id.Value && !e.IsDeleted, ct)
                ?? throw new KeyNotFoundException("Equipment not found");
            eq.ModifiedAt = DateTime.UtcNow;
            eq.ModifiedBy = _currentUser.UserId;
        }
        else
        {
            eq = new ContractEquipment
            {
                ContractId = cmd.ContractId,
                CreatedBy  = _currentUser.UserId,
            };
            _db.ContractEquipments.Add(eq);
        }

        eq.ItemName     = cmd.ItemName;
        eq.SerialNumber = cmd.SerialNumber;
        eq.Brand        = cmd.Brand;
        eq.Model        = cmd.Model;
        eq.Location     = cmd.Location;
        eq.InstallDate  = cmd.InstallDate;
        eq.ProductId    = cmd.ProductId;
        eq.Notes        = cmd.Notes;

        await _db.SaveChangesAsync(ct);
        return eq.Id;
    }
}
