using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Operational.Products;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Manager,Sales,Accountant")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/products")]
public class ProductsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? category, [FromQuery] bool? isActive)
        => Ok(await Mediator.Send(new GetProductsQuery { Search = search, Category = category, IsActive = isActive }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await Mediator.Send(new GetProductQuery { Id = id }));

    [HttpGet("{id:guid}/stock")]
    public async Task<IActionResult> Stock(Guid id) => Ok(await Mediator.Send(new GetProductStockQuery { ProductId = id }));

    [HttpGet("stock")]
    public async Task<IActionResult> AllStock([FromQuery] Guid? warehouseId)
        => Ok(await Mediator.Send(new GetProductStockQuery { WarehouseId = warehouseId }));

    [HttpPost]
    public async Task<IActionResult> Create(UpsertProductCommand cmd)
    {
        var id = await Mediator.Send(cmd);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertProductCommand cmd)
    { cmd.Id = id; await Mediator.Send(cmd); return NoContent(); }

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<IActionResult> Toggle(Guid id)
    { await Mediator.Send(new ToggleProductStatusCommand { Id = id }); return NoContent(); }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] RTSErp.Application.Common.Interfaces.IApplicationDbContext db,
        CancellationToken ct)
    {
        var hasMovements = await db.InventoryMovements.AnyAsync(m => m.ProductId == id && !m.IsDeleted, ct);
        if (hasMovements)
            return Conflict(new { message = "Cannot delete product with inventory history. Deactivate it instead." });

        var product = await db.Products.FindAsync([id], ct);
        if (product is null || product.IsDeleted) return NotFound();
        product.IsDeleted  = true;
        product.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
