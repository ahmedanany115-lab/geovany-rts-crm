using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Operational.PurchaseOrders;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class PurchaseOrdersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PurchaseOrderStatus? status, [FromQuery] Guid? supplierId)
        => Ok(await Mediator.Send(new GetPurchaseOrdersQuery { Status = status, SupplierId = supplierId }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await Mediator.Send(new GetPurchaseOrderQuery { Id = id }));

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseOrderCommand cmd)
    { var id = await Mediator.Send(cmd); return CreatedAtAction(nameof(Get), new { id }, new { id }); }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    { await Mediator.Send(new ApprovePurchaseOrderCommand { Id = id }); return NoContent(); }
}

[Authorize]
public class PurchaseReceiptsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? purchaseOrderId, [FromQuery] Guid? supplierId)
        => Ok(await Mediator.Send(new GetPurchaseReceiptsQuery { PurchaseOrderId = purchaseOrderId, SupplierId = supplierId }));

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseReceiptCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }
}

[Authorize]
public class SupplierInvoicesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? status, [FromQuery] Guid? supplierId)
        => Ok(await Mediator.Send(new GetSupplierInvoicesQuery { Status = status, SupplierId = supplierId }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplierInvoiceCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id)
    { await Mediator.Send(new PostSupplierInvoiceCommand { Id = id }); return NoContent(); }
}
