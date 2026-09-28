using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Finance.Cash;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant")]
[Route("api/v1/cashreceipts")]
public class CashReceiptsController : BaseApiController
{
    /// <summary>List cash receipts with optional filters.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] CashTransactionStatus? status,
        [FromQuery] Guid?    cashAccountId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 50)
        => Ok(await Mediator.Send(new GetCashReceiptsQuery
        {
            Status        = status,
            CashAccountId = cashAccountId,
            FromDate      = fromDate,
            ToDate        = toDate,
            Page          = page,
            PageSize      = pageSize
        }));

    /// <summary>Get a single cash receipt by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await Mediator.Send(new GetCashReceiptQuery { Id = id });
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new cash receipt in Draft status.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateCashReceiptCommand cmd)
    {
        var id = await Mediator.Send(cmd);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    /// <summary>Post a draft cash receipt — creates and posts the journal entry.</summary>
    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id)
    {
        await Mediator.Send(new PostCashReceiptCommand { Id = id });
        return NoContent();
    }

    /// <summary>Update a draft cash receipt.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCashReceiptCommand cmd)
    {
        cmd.Id = id;
        await Mediator.Send(cmd);
        return NoContent();
    }

    /// <summary>Void a posted cash receipt — reverses the journal entry.</summary>
    [HttpPost("{id:guid}/void")]
    public async Task<IActionResult> Void(Guid id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.Formatters.EmptyBodyBehavior.Allow)] VoidCashReceiptCommand? cmd)
    {
        var command = cmd ?? new VoidCashReceiptCommand();
        command.Id = id;
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>Soft-delete a draft cash receipt.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteCashReceiptCommand { Id = id });
        return NoContent();
    }
}
