using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Accounting.TaxRates;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class TaxRatesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List()
        => Ok(await Mediator.Send(new GetTaxRatesQuery()));
}
