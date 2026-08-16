using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Accounting.FiscalPeriods.Commands.CloseFiscalPeriod;
using RTSErp.Application.Accounting.FiscalPeriods.Commands.CreateFiscalPeriod;
using RTSErp.Application.Accounting.FiscalPeriods.Queries;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class FiscalPeriodsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var result = await Mediator.Send(new GetFiscalPeriodsQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFiscalPeriodCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(List), new { id }, new { id });
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id)
    {
        await Mediator.Send(new CloseFiscalPeriodCommand { Id = id });
        return NoContent();
    }

    [HttpPost("{id:guid}/open")]
    public async Task<IActionResult> Open(Guid id)
    {
        await Mediator.Send(new OpenFiscalPeriodCommand { Id = id });
        return NoContent();
    }
}
