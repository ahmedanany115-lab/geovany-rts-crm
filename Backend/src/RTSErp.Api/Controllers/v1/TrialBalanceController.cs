using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Accounting.TrialBalance.Queries;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class TrialBalanceController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var result = await Mediator.Send(new GetTrialBalanceQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }
}
