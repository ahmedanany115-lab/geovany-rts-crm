using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Warehouse.GoodsReceipts;
using RTSErp.Domain.Entities.Audit;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,Warehouse,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/goodsreceipts")]
public class GoodsReceiptsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] GoodsReceiptStatus? status,
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? supplierId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
        => Ok(await Mediator.Send(new GetGoodsReceiptsQuery
        {
            Status = status, WarehouseId = warehouseId, SupplierId = supplierId,
            FromDate = fromDate, ToDate = toDate, Page = page, PageSize = pageSize
        }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
        => Ok(await Mediator.Send(new GetGoodsReceiptQuery { Id = id }));

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateGoodsReceiptCommand cmd,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        var result = await Mediator.Send(cmd, ct);
        _ = audit.LogAsync(AuditActions.Created, AuditModules.Inventory,
            entityName: result.ReceiptNumber, entityId: result.ReceiptId,
            entityType: "GoodsReceipt", reference: result.ReceiptNumber, ct: ct);
        return CreatedAtAction(nameof(Get), new { id = result.ReceiptId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateGoodsReceiptCommand cmd,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        cmd.Id = id;
        await Mediator.Send(cmd, ct);
        _ = audit.LogAsync(AuditActions.Updated, AuditModules.Inventory,
            entityId: id, entityType: "GoodsReceipt", ct: ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        await Mediator.Send(new DeleteGoodsReceiptCommand { Id = id }, ct);
        _ = audit.LogAsync(AuditActions.Deleted, AuditModules.Inventory,
            entityId: id, entityType: "GoodsReceipt", ct: ct);
        return NoContent();
    }
}
