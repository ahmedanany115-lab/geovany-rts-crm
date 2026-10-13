using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Accounting.Currencies.Commands.UpsertCurrency;
using RTSErp.Application.Accounting.Currencies.Queries;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/currencies")]
public class CurrenciesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var result = await Mediator.Send(new GetCurrenciesQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(UpsertCurrencyCommand command)
    {
        command.Id = null;
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(List), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertCurrencyCommand command)
    {
        command.Id = id;
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var currency = await db.Currencies.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
        if (currency is null) return NotFound();
        if (currency.IsBaseCurrency)
            return BadRequest(new { message = "The base currency cannot be deleted." });

        var inUse =
            await db.JournalEntries.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.JournalEntryLines.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.BankAccounts.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.BusinessPartners.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.PurchaseOrders.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.SupplierInvoices.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.SalesOrders.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.CustomerInvoices.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.CustomerPayments.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.SupplierPayments.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.BankTransactions.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.Cheques.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.CashReceipts.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct) ||
            await db.CashPayments.AnyAsync(x => !x.IsDeleted && x.CurrencyId == id, ct);

        if (inUse)
            return Conflict(new { message = "Currency has financial history and cannot be deleted. Deactivate it instead." });

        currency.IsActive = false;
        currency.IsDeleted = true;
        currency.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
