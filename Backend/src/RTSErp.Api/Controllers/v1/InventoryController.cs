using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Operational.Inventory;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class InventoryController : BaseApiController
{
    [HttpGet("movements")]
    public async Task<IActionResult> Movements(
        [FromQuery] Guid? productId, [FromQuery] Guid? warehouseId,
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
        => Ok(await Mediator.Send(new GetInventoryMovementsQuery
            { ProductId = productId, WarehouseId = warehouseId, FromDate = fromDate, ToDate = toDate }));

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust(AdjustInventoryCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(TransferInventoryCommand cmd)
    { await Mediator.Send(cmd); return NoContent(); }
}
