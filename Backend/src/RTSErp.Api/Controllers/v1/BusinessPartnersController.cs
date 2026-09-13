using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Operational.BusinessPartners;
using RTSErp.Domain.Entities.Accounting;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Manager,SalesManager,Sales,Accountant")]
public class CustomersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive, [FromQuery] string? search)
        => Ok(await Mediator.Send(new GetBusinessPartnersQuery
        {
            PartnerType = BusinessPartnerType.Customer,
            IsActive = isActive,
            Search = search
        }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
        => Ok(await Mediator.Send(new GetBusinessPartnerQuery { Id = id }));

    [HttpPost]
    public async Task<IActionResult> Create(UpsertBusinessPartnerCommand cmd)
    {
        cmd.PartnerType = BusinessPartnerType.Customer;
        var id = await Mediator.Send(cmd);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertBusinessPartnerCommand cmd)
    {
        cmd.Id = id;
        cmd.PartnerType = BusinessPartnerType.Customer;
        await Mediator.Send(cmd);
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await Mediator.Send(new ToggleBusinessPartnerStatusCommand { Id = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var partner = await db.BusinessPartners.FindAsync([id], ct);
        if (partner is null || partner.IsDeleted) return NotFound();
        partner.IsDeleted  = true;
        partner.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

[Authorize(Roles = "Admin,Manager,SalesManager,Sales,Accountant")]
public class SuppliersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive, [FromQuery] string? search)
        => Ok(await Mediator.Send(new GetBusinessPartnersQuery
        {
            PartnerType = BusinessPartnerType.Supplier,
            IsActive = isActive,
            Search = search
        }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
        => Ok(await Mediator.Send(new GetBusinessPartnerQuery { Id = id }));

    [HttpPost]
    public async Task<IActionResult> Create(UpsertBusinessPartnerCommand cmd)
    {
        cmd.PartnerType = BusinessPartnerType.Supplier;
        var id = await Mediator.Send(cmd);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpsertBusinessPartnerCommand cmd)
    {
        cmd.Id = id;
        cmd.PartnerType = BusinessPartnerType.Supplier;
        await Mediator.Send(cmd);
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await Mediator.Send(new ToggleBusinessPartnerStatusCommand { Id = id });
        return NoContent();
    }
}
