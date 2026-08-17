using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Operational.Warehouses;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class WarehousesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive)
        => Ok(await Mediator.Send(new GetWarehousesQuery { IsActive = isActive }));

    [HttpPost]
    public async Task<IActionResult> Create(UpsertWarehouseCommand cmd)
    { var id = await Mediator.Send(cmd); return CreatedAtAction(nameof(List), new { id }, new { id }); }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertWarehouseCommand cmd)
    { cmd.Id = id; await Mediator.Send(cmd); return NoContent(); }

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<IActionResult> Toggle(Guid id)
    { await Mediator.Send(new ToggleWarehouseStatusCommand { Id = id }); return NoContent(); }
}
