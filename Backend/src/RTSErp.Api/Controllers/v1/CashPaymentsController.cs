using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Finance.Cash;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant")]
[Route("api/v1/cashpayments")]
public class CashPaymentsController : BaseApiController
{
    /// <summary>List cash payments with optional filters.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] CashTransactionStatus? status,
        [FromQuery] Guid?    cashAccountId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 50)
        => Ok(await Mediator.Send(new GetCashPaymentsQuery
        {
            Status        = status,
            CashAccountId = cashAccountId,
            FromDate      = fromDate,
            ToDate        = toDate,
            Page          = page,
            PageSize      = pageSize
        }));

    /// <summary>Get a single cash payment by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await Mediator.Send(new GetCashPaymentQuery { Id = id });
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new cash payment in Draft status.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateCashPaymentCommand cmd)
    {
        var id = await Mediator.Send(cmd);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    /// <summary>Post a draft cash payment — creates and posts the journal entry.</summary>
    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id)
    {
        await Mediator.Send(new PostCashPaymentCommand { Id = id });
        return NoContent();
    }

    /// <summary>Update a draft cash payment.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCashPaymentCommand cmd)
    {
        cmd.Id = id;
        await Mediator.Send(cmd);
        return NoContent();
    }

    /// <summary>Void a posted cash payment — reverses the journal entry.</summary>
    [HttpPost("{id:guid}/void")]
    public async Task<IActionResult> Void(Guid id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.Formatters.EmptyBodyBehavior.Allow)] VoidCashPaymentCommand? cmd)
    {
        var command = cmd ?? new VoidCashPaymentCommand();
        command.Id = id;
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Soft-delete a draft cash payment.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteCashPaymentCommand { Id = id });
        return NoContent();
    }
}
