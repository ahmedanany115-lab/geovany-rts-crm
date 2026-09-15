using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Accounting.Reports;
using RTSErp.Application.Accounting.TrialBalance.Queries;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
public class ReportsController : BaseApiController
{
    /// <summary>Income Statement / Profit & Loss from posted JE lines.</summary>
    [HttpGet("income-statement")]
    public async Task<IActionResult> IncomeStatement(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await Mediator.Send(new GetIncomeStatementQuery
        {
            FromDate = fromDate ?? new DateOnly(today.Year, 1, 1),
            ToDate   = toDate   ?? today
        }, ct));
    }

    /// <summary>Profit & Loss — same engine as Income Statement, aliased route.</summary>
    [HttpGet("profit-loss")]
    public async Task<IActionResult> ProfitLoss(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken ct)
        => await IncomeStatement(fromDate, toDate, ct);
}
