using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Finance.CashJournal;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant")]
[Route("api/v1/cashjournal")]
public class CashJournalController : BaseApiController
{
    /// <summary>
    /// Returns the cash journal for a given account and date range.
    /// Shows all posted journal entry lines affecting the cash account,
    /// with opening balance, running balances, and totals.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid     cashAccountId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate)
    {
        if (cashAccountId == Guid.Empty)
            return BadRequest("cashAccountId is required.");

        if (fromDate > toDate)
            return BadRequest("fromDate must not be later than toDate.");

        var result = await Mediator.Send(new GetCashJournalQuery
        {
            CashAccountId = cashAccountId,
            FromDate      = fromDate,
            ToDate        = toDate
        });

        return Ok(result);
    }
}
