using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Operational.Dashboard.Queries;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class ErpDashboardController : BaseApiController
{
    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis()
        => Ok(await Mediator.Send(new GetErpDashboardKpisQuery()));
}
