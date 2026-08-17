using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Operational.Products;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
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
}
