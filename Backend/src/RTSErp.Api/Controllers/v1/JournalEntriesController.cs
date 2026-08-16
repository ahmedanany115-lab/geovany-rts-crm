using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Accounting.JournalEntries.Commands.CreateJournalEntry;
using RTSErp.Application.Accounting.JournalEntries.Commands.PostJournalEntry;
using RTSErp.Application.Accounting.JournalEntries.Commands.ReverseJournalEntry;
using RTSErp.Application.Accounting.JournalEntries.Queries.GetJournalEntries;
using RTSErp.Application.Accounting.JournalEntries.Queries.GetJournalEntry;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class JournalEntriesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] JournalEntryStatus? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? fiscalPeriodId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetJournalEntriesQuery
        {
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            FiscalPeriodId = fiscalPeriodId,
            Page = page,
            PageSize = pageSize
        });
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await Mediator.Send(new GetJournalEntryQuery { Id = id });
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateJournalEntryCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors });
        return CreatedAtAction(nameof(Get), new { id = result.EntryId }, result);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id)
    {
        var result = await Mediator.Send(new PostJournalEntryCommand { Id = id });
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors });
        return Ok(result);
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<IActionResult> Reverse(Guid id, ReverseJournalEntryCommand command)
    {
        command.Id = id;
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors });
        return Ok(result);
    }
}
