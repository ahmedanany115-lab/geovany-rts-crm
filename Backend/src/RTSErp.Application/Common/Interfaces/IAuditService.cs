using RTSErp.Domain.Entities.Audit;

namespace RTSErp.Application.Common.Interfaces;

/// <summary>
/// Writes audit log entries. Fire-and-forget — never throws to the caller.
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        string  action,
        string  module,
        string? entityName   = null,
        Guid?   entityId     = null,
        string? entityType   = null,
        string? reference    = null,
        string  status       = "Success",
        string? details      = null,
        CancellationToken ct = default);
}
