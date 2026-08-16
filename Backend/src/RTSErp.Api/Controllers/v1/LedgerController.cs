using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Accounting.Ledger.Queries;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class LedgerController : BaseApiController
{
    [HttpGet("account/{accountId:guid}")]
    public async Task<IActionResult> AccountLedger(
        Guid accountId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var result = await Mediator.Send(new GetAccountLedgerQuery
        {
            AccountId = accountId,
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }

    [HttpGet("account/{accountId:guid}/balance")]
    public async Task<IActionResult> AccountBalance(
        Guid accountId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var result = await Mediator.Send(new GetAccountBalanceQuery
        {
            AccountId = accountId,
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }
}
