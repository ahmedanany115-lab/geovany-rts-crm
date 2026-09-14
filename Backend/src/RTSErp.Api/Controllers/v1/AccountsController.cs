using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Accounting.Accounts.Commands.CreateAccount;
using RTSErp.Application.Accounting.Accounts.Commands.ToggleAccountStatus;
using RTSErp.Application.Accounting.Accounts.Commands.UpdateAccount;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Accounting.Accounts.Queries.GetAccount;
using RTSErp.Application.Accounting.Accounts.Queries.GetAccounts;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
public class AccountsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] AccountType? accountType,
        [FromQuery] bool? isGroup,
        [FromQuery] bool? isActive,
        [FromQuery] Guid? parentId,
        [FromQuery] bool topLevelOnly = false)
    {
        var result = await Mediator.Send(new GetAccountsQuery
        {
            AccountType = accountType,
            IsGroup = isGroup,
            IsActive = isActive,
            ParentId = parentId,
            TopLevelOnly = topLevelOnly
        });
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await Mediator.Send(new GetAccountQuery { Id = id });
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountCommand command)
    {
        var id = await Mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateAccountCommand command)
    {
        command.Id = id;
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        await Mediator.Send(new ToggleAccountStatusCommand { Id = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var account = await db.Accounts.FindAsync([id], ct);
        if (account is null || account.IsDeleted) return NotFound();

        // Prevent deletion if account has journal entry lines
        var hasLines = await db.JournalEntryLines
            .AnyAsync(l => l.AccountId == id && !l.IsDeleted, ct);
        if (hasLines)
            return Conflict(new { message = "Cannot delete account with existing journal entries. Deactivate it instead." });

        account.IsDeleted  = true;
        account.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
