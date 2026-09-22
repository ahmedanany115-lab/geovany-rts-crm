using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Operational.BusinessPartners;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Entities.Audit;

namespace RTSErp.Api.Controllers.v1;

// ── Customers ─────────────────────────────────────────────────────────────────
[Authorize(Roles = "Admin,Manager,SalesManager,Sales,Accountant")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/customers")]
public class CustomersController : BaseApiController
{
    private readonly ICurrentUserService _user;
    public CustomersController(ICurrentUserService user) => _user = user;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive, [FromQuery] string? search)
    {
        // Sales users only see customers assigned to them
        Guid? salesFilter = null;
        if (User.IsInRole("Sales") && !User.IsInRole("Admin") && !User.IsInRole("Manager")
            && !User.IsInRole("Accountant") && !User.IsInRole("SalesManager"))
        {
            salesFilter = _user.UserId;
        }

        return Ok(await Mediator.Send(new GetBusinessPartnersQuery
        {
            PartnerType       = BusinessPartnerType.Customer,
            IsActive          = isActive,
            Search            = search,
            AssignedSalesRepId = salesFilter,
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
        => Ok(await Mediator.Send(new GetBusinessPartnerQuery { Id = id }));

    // CREATE — Admin + Accountant only. Sales cannot create customers.
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> Create(
        UpsertBusinessPartnerCommand cmd,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        cmd.PartnerType = BusinessPartnerType.Customer;
        var id = await Mediator.Send(cmd);
        _ = audit.LogAsync(AuditActions.Created, AuditModules.CRM,
            entityName: cmd.Name, entityId: id, entityType: "Customer", ct: ct);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> Update(
        Guid id,
        UpsertBusinessPartnerCommand cmd,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        cmd.Id = id;
        cmd.PartnerType = BusinessPartnerType.Customer;
        await Mediator.Send(cmd);
        _ = audit.LogAsync(AuditActions.Updated, AuditModules.CRM,
            entityName: cmd.Name, entityId: id, entityType: "Customer", ct: ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle-status")]
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await Mediator.Send(new ToggleBusinessPartnerStatusCommand { Id = id });
        return NoContent();
    }

    [HttpPatch("{id:guid}/assign")]
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> AssignSalesRep(
        Guid id,
        [FromBody] AssignSalesRepRequest req,
        [FromServices] IApplicationDbContext db,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        var partner = await db.BusinessPartners.FindAsync([id], ct);
        if (partner is null || partner.IsDeleted) return NotFound();

        partner.AssignedSalesRepId   = req.SalesRepId;
        partner.AssignedSalesRepName = req.SalesRepName?.Trim();
        partner.ModifiedAt           = DateTime.UtcNow;
        partner.ModifiedBy           = _user.UserId;

        await db.SaveChangesAsync(ct);

        _ = audit.LogAsync(AuditActions.Updated, AuditModules.CRM,
            entityName: partner.Name, entityId: id, entityType: "Customer",
            details: $"Assigned to sales rep: {req.SalesRepName ?? "none"}", ct: ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        var partner = await db.BusinessPartners.FindAsync([id], ct);
        if (partner is null || partner.IsDeleted) return NotFound();
        partner.IsDeleted  = true;
        partner.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        _ = audit.LogAsync(AuditActions.Deleted, AuditModules.CRM,
            entityName: partner.Name, entityId: id, entityType: "Customer", ct: ct);
        return NoContent();
    }

    public record AssignSalesRepRequest(Guid? SalesRepId, string? SalesRepName);
}

// ── Suppliers ─────────────────────────────────────────────────────────────────
[Authorize(Roles = "Admin,Manager,SalesManager,Sales,Accountant,Purchasing")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/suppliers")]
public class SuppliersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? isActive, [FromQuery] string? search)
        => Ok(await Mediator.Send(new GetBusinessPartnersQuery
        {
            PartnerType = BusinessPartnerType.Supplier,
            IsActive = isActive,
            Search = search,
        }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
        => Ok(await Mediator.Send(new GetBusinessPartnerQuery { Id = id }));

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Accountant,Purchasing")]
    public async Task<IActionResult> Create(UpsertBusinessPartnerCommand cmd)
    {
        cmd.PartnerType = BusinessPartnerType.Supplier;
        var id = await Mediator.Send(cmd);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Accountant,Purchasing")]
    public async Task<IActionResult> Update(Guid id, UpsertBusinessPartnerCommand cmd)
    {
        cmd.Id = id;
        cmd.PartnerType = BusinessPartnerType.Supplier;
        await Mediator.Send(cmd);
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle-status")]
    [Authorize(Roles = "Admin,Manager,Accountant,Purchasing")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await Mediator.Send(new ToggleBusinessPartnerStatusCommand { Id = id });
        return NoContent();
    }
}
