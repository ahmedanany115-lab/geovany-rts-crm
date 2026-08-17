using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Operational.BusinessPartners;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize]
public class CustomersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive, [FromQuery] string? search)
        => Ok(await Mediator.Send(new GetBusinessPartnersQuery { PartnerType = BusinessPartnerType.Customer, IsActive = isActive, Search = search }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await Mediator.Send(new GetBusinessPartnerQuery { Id = id }));

    [HttpPost]
    public async Task<IActionResult> Create(UpsertBusinessPartnerCommand cmd)
    {
        cmd.PartnerType = BusinessPartnerType.Customer;
        var id = await Mediator.Send(cmd);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertBusinessPartnerCommand cmd)
    { cmd.Id = id; await Mediator.Send(cmd); return NoContent(); }

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<IActionResult> Toggle(Guid id)
    { await Mediator.Send(new ToggleBusinessPartnerStatusCommand { Id = id }); return NoContent(); }
}

[Authorize]
public class SuppliersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive, [FromQuery] string? search)
        => Ok(await Mediator.Send(new GetBusinessPartnersQuery { PartnerType = BusinessPartnerType.Supplier, IsActive = isActive, Search = search }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await Mediator.Send(new GetBusinessPartnerQuery { Id = id }));

    [HttpPost]
    public async Task<IActionResult> Create(UpsertBusinessPartnerCommand cmd)
    {
        cmd.PartnerType = BusinessPartnerType.Supplier;
        var id = await Mediator.Send(cmd);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertBusinessPartnerCommand cmd)
    { cmd.Id = id; await Mediator.Send(cmd); return NoContent(); }

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<IActionResult> Toggle(Guid id)
    { await Mediator.Send(new ToggleBusinessPartnerStatusCommand { Id = id }); return NoContent(); }
}
