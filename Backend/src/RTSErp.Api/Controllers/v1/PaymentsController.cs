using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Operational.Payments;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class CustomerPaymentsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? customerId, [FromQuery] PaymentStatus? status)
        => Ok(await Mediator.Send(new GetCustomerPaymentsQuery { CustomerId = customerId, Status = status }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerPaymentCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }
}

[Authorize]
public class SupplierPaymentsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? supplierId)
        => Ok(await Mediator.Send(new GetSupplierPaymentsQuery { SupplierId = supplierId }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplierPaymentCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }
}

[Authorize]
public class ChequesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ChequeStatus? status, [FromQuery] Guid? customerId)
        => Ok(await Mediator.Send(new GetChequesQuery { Status = status, CustomerId = customerId }));

    [HttpPost]
    public async Task<IActionResult> Receive(ReceiveChequeCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }

    [HttpPost("{id:guid}/deposit")]
    public async Task<IActionResult> Deposit(Guid id, DepositChequeCommand cmd)
    { cmd.Id = id; await Mediator.Send(cmd); return NoContent(); }

    [HttpPost("{id:guid}/bounce")]
    public async Task<IActionResult> Bounce(Guid id, [FromBody] DateOnly bounceDate)
    { await Mediator.Send(new BounceChequeCommand { Id = id, BounceDate = bounceDate }); return NoContent(); }
}

[Authorize]
public class BankTransactionsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? bankAccountId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
        => Ok(await Mediator.Send(new GetBankTransactionsQuery { BankAccountId = bankAccountId, FromDate = fromDate, ToDate = toDate }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateBankTransactionCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }
}

[Authorize]
public class BankAccountsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive)
        => Ok(await Mediator.Send(new GetBankAccountsQuery { IsActive = isActive }));

    [HttpPost]
    public async Task<IActionResult> Create(UpsertBankAccountCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertBankAccountCommand cmd)
    { cmd.Id = id; await Mediator.Send(cmd); return NoContent(); }
}

[Authorize]
public class CommissionsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? salespersonId, [FromQuery] CommissionStatus? status)
        => Ok(await Mediator.Send(new GetSalesCommissionsQuery { SalespersonId = salespersonId, Status = status }));
}
