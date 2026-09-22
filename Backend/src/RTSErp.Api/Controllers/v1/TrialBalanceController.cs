using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Accounting.TrialBalance.Queries;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/trialbalance")]
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
