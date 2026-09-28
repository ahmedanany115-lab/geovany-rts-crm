using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Application.Sales.WarrantyCertificates;
using RTSErp.Domain.Entities.Audit;
using RTSErp.Domain.Enums;

namespace RTSErp.Api.Controllers.v1;

[Authorize(Roles = "Admin,Accountant,SalesManager")]
[Microsoft.AspNetCore.Mvc.Route("api/v1/warrantycertificates")]
public class WarrantyCertificatesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? customerId,
        [FromQuery] WarrantyCertificateStatus? status,
        [FromQuery] string? serialNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
        => Ok(await Mediator.Send(new GetWarrantyCertificatesQuery
        {
            CustomerId = customerId, Status = status,
            SerialNumber = serialNumber, Page = page, PageSize = pageSize
        }));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
        => Ok(await Mediator.Send(new GetWarrantyCertificateQuery { Id = id }));

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateWarrantyCertificateCommand cmd,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        var result = await Mediator.Send(cmd, ct);
        _ = audit.LogAsync(AuditActions.Created, AuditModules.Sales,
            entityName: result.CertificateNumber, entityId: result.CertificateId,
            entityType: "WarrantyCertificate", reference: result.CertificateNumber, ct: ct);
        return CreatedAtAction(nameof(Get), new { id = result.CertificateId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateWarrantyCertificateCommand cmd,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        cmd.Id = id;
        await Mediator.Send(cmd, ct);
        _ = audit.LogAsync(AuditActions.Updated, AuditModules.Sales,
            entityId: id, entityType: "WarrantyCertificate", ct: ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        await Mediator.Send(new DeleteWarrantyCertificateCommand { Id = id }, ct);
        _ = audit.LogAsync(AuditActions.Deleted, AuditModules.Sales,
            entityId: id, entityType: "WarrantyCertificate", ct: ct);
        return NoContent();
    }
}
