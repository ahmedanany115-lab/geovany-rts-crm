using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Operational.Payments;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/customerpayments")]
public class CustomerPaymentsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? customerId, [FromQuery] PaymentStatus? status)
        => Ok(await Mediator.Send(new GetCustomerPaymentsQuery { CustomerId = customerId, Status = status }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerPaymentCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }
}

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/supplierpayments")]
public class SupplierPaymentsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? supplierId)
        => Ok(await Mediator.Send(new GetSupplierPaymentsQuery { SupplierId = supplierId }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplierPaymentCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }
}

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/cheques")]
public class ChequesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? direction,
        [FromQuery] ChequeStatus? status,
        [FromQuery] Guid? customerId,
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var q = db.Cheques
            .Include(c => c.Customer)
            .Include(c => c.Supplier)
            .Include(c => c.Currency)
            .Where(c => !c.IsDeleted);

        if (direction.HasValue) q = q.Where(c => (int)c.Direction == direction.Value);
        if (status.HasValue)    q = q.Where(c => c.Status == status.Value);
        if (customerId.HasValue) q = q.Where(c => c.CustomerId == customerId.Value);

        var items = await q.OrderByDescending(c => c.DueDate)
            .Select(c => new
            {
                c.Id, c.ChequeNumber, c.Direction,
                customerName = c.Customer != null ? c.Customer.Name : null,
                supplierName = c.Supplier != null ? c.Supplier.Name : null,
                c.BankName, c.Amount, c.AmountBase,
                currencyCode = c.Currency.Code,
                c.IssueDate, c.DueDate, c.ReceivedDate, c.Status,
                statusName   = c.Status.ToString(),
                c.Notes, c.CreatedAt,
            }).ToListAsync(ct);

        return Ok(items);
    }

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

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/banktransactions")]
public class BankTransactionsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? bankAccountId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
        => Ok(await Mediator.Send(new GetBankTransactionsQuery { BankAccountId = bankAccountId, FromDate = fromDate, ToDate = toDate }));

    [HttpPost]
    public async Task<IActionResult> Create(CreateBankTransactionCommand cmd)
    { var id = await Mediator.Send(cmd); return Ok(new { id }); }
}

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/bankaccounts")]
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

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/commissions")]
public class CommissionsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? salespersonId, [FromQuery] CommissionStatus? status)
        => Ok(await Mediator.Send(new GetSalesCommissionsQuery { SalespersonId = salespersonId, Status = status }));
}
