using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Audit;

namespace RTSErp.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService   _user;
    private readonly IHttpContextAccessor  _http;
    private readonly ILogger<AuditService> _log;

    public AuditService(
        IApplicationDbContext db,
        ICurrentUserService user,
        IHttpContextAccessor http,
        ILogger<AuditService> log)
    { _db = db; _user = user; _http = http; _log = log; }

    public async Task LogAsync(
        string  action,
        string  module,
        string? entityName = null,
        Guid?   entityId   = null,
        string? entityType = null,
        string? reference  = null,
        string  status     = "Success",
        string? details    = null,
        CancellationToken ct = default)
    {
        try
        {
            var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var entry = new AuditLog
            {
                UserId     = _user.UserId,
                UserName   = _user.UserName ?? string.Empty,
                UserEmail  = _user.UserEmail ?? string.Empty,
                Action     = action,
                Module     = module,
                EntityType = entityType,
                EntityId   = entityId,
                EntityName = entityName,
                Reference  = reference,
                Status     = status,
                Details    = details,
                IpAddress  = ip,
                OccurredAt = DateTime.UtcNow,
            };
            _db.AuditLogs.Add(entry);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[Audit] Failed to write audit log entry.");
        }
    }
}
