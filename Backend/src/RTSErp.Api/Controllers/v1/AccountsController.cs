using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Accounting.Accounts.Commands.CreateAccount;
using RTSErp.Application.Accounting.Accounts.Commands.ToggleAccountStatus;
using RTSErp.Application.Accounting.Accounts.Commands.UpdateAccount;
using RTSErp.Application.Accounting.Accounts.Queries.GetAccount;
using RTSErp.Application.Accounting.Accounts.Queries.GetAccounts;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
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
}
