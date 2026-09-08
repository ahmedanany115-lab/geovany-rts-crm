using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Dashboard.Queries.GetKpis;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Manager,Sales,Accountant,SupportAgent")]
public class DashboardController : BaseApiController
{
    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis()
    {
        var kpis = await Mediator.Send(new GetDashboardKpisQuery());
        return Ok(kpis);
    }
}
