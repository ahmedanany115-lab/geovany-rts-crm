using MediatR;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;

namespace RTSErp.Application.Dashboard.Queries.GetKpis;

public class GetDashboardKpisQuery : IRequest<DashboardKpisDto>
{
}

public class DashboardKpisDto
{
    public int EmployeeCount { get; set; }

    // Genuinely 0, not faked: these depend on Invoices/Projects/Tickets, which don't exist
    // yet (see RoadmapFinal.md Milestones 5-7). Wiring real aggregation queries here is a
    // one-line change once those entities land — the endpoint contract doesn't need to change.
    public decimal Revenue { get; set; }
    public int ActiveProjects { get; set; }
    public int ActiveTickets { get; set; }
}

public class GetDashboardKpisQueryHandler : IRequestHandler<GetDashboardKpisQuery, DashboardKpisDto>
{
    private readonly IApplicationDbContext _db;

    public GetDashboardKpisQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardKpisDto> Handle(GetDashboardKpisQuery request, CancellationToken cancellationToken)
    {
        var employeeCount = await _db.Employees.CountAsync(cancellationToken);

        return new DashboardKpisDto
        {
            EmployeeCount = employeeCount,
            Revenue = 0,
            ActiveProjects = 0,
            ActiveTickets = 0
        };
    }
}
