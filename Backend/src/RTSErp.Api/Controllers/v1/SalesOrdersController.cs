using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Operational.SalesOrders;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class SalesOrdersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] SalesOrderStatus? status, [FromQuery] Guid? customerId)
        => Ok(await Mediator.Send(new GetSalesOrdersQuery { Status = status, CustomerId = customerId }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await Mediator.Send(new GetSalesOrderQuery { Id = id }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateSalesOrderCommand cmd)
    { var id = await Mediator.Send(cmd); return CreatedAtAction(nameof(Get), new { id }, new { id }); }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    { await Mediator.Send(new ApproveSalesOrderCommand { Id = id }); return NoContent(); }
}

[Authorize]
public class SalesDeliveriesController : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateSalesDeliveryCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }
}

[Authorize]
public class CustomerInvoicesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] InvoiceStatus? status, [FromQuery] Guid? customerId)
        => Ok(await Mediator.Send(new GetCustomerInvoicesQuery { Status = status, CustomerId = customerId }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await Mediator.Send(new GetCustomerInvoiceQuery { Id = id }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerInvoiceCommand cmd)
    { var id = await Mediator.Send(cmd); return CreatedAtAction(nameof(Get), new { id }, new { id }); }

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id)
    { await Mediator.Send(new PostCustomerInvoiceCommand { Id = id }); return NoContent(); }
}
