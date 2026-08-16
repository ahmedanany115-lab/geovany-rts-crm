using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Accounting.Currencies.Commands.UpsertCurrency;
using RTSErp.Application.Accounting.Currencies.Queries;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class CurrenciesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var result = await Mediator.Send(new GetCurrenciesQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(UpsertCurrencyCommand command)
    {
        command.Id = null;
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(List), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertCurrencyCommand command)
    {
        command.Id = id;
        await Mediator.Send(command);
        return NoContent();
    }
}
