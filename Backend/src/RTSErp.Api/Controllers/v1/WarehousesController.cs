using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Operational.Warehouses;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Manager,Sales,Accountant")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/warehouses")]
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

    /// <summary>Soft-delete a warehouse (only if no stock movements exist).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var hasMovements = await db.InventoryMovements
            .AnyAsync(m => m.WarehouseId == id && !m.IsDeleted, ct);
        if (hasMovements)
            return Conflict(new { message = "Cannot delete warehouse with existing stock movements. Deactivate it instead." });

        var hasGoodsReceipts = await db.GoodsReceipts
            .AnyAsync(g => g.WarehouseId == id && !g.IsDeleted, ct);
        if (hasGoodsReceipts)
            return Conflict(new { message = "Cannot delete warehouse with existing goods receipts. Deactivate it instead." });

        await Mediator.Send(new DeleteWarehouseCommand { Id = id }, ct);
        return NoContent();
    }
}
