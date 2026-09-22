using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Accounting.TaxRates;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/taxrates")]
public class TaxRatesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List()
        => Ok(await Mediator.Send(new GetTaxRatesQuery()));
}
