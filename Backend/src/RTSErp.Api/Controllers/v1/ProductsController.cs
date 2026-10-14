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
        var product = await db.Products.FindAsync([id], ct);
        if (product is null || product.IsDeleted) return NotFound();

        var hasMovements = await db.InventoryMovements.AnyAsync(m => m.ProductId == id && !m.IsDeleted, ct);
        var hasStock = await db.InventoryBalances.AnyAsync(b => b.ProductId == id && !b.IsDeleted && b.Quantity != 0, ct);
        var hasPurchaseHistory =
            await db.PurchaseOrderLines.AnyAsync(l => l.ProductId == id && !l.IsDeleted, ct) ||
            await db.PurchaseReceiptLines.AnyAsync(l => l.ProductId == id && !l.IsDeleted, ct) ||
            await db.SupplierInvoiceLines.AnyAsync(l => l.ProductId == id && !l.IsDeleted, ct);
        var hasSalesHistory =
            await db.SalesOrderLines.AnyAsync(l => l.ProductId == id && !l.IsDeleted, ct) ||
            await db.SalesDeliveryLines.AnyAsync(l => l.ProductId == id && !l.IsDeleted, ct) ||
            await db.CustomerInvoiceLines.AnyAsync(l => l.ProductId == id && !l.IsDeleted, ct);

        // Historical products remain in the database so old documents and reports
        // keep their references. DELETE behaves as a safe archive/deactivation.
        if (hasMovements || hasStock || hasPurchaseHistory || hasSalesHistory)
        {
            product.IsActive = false;
            product.ModifiedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Ok(new { archived = true, message = "Product has history and was deactivated instead of being permanently deleted." });
        }

        product.IsActive = false;
        product.IsDeleted = true;
        product.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { archived = false, message = "Product deleted." });
    }
}
