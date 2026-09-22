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
        string? ipAddress  = null,
        CancellationToken ct = default)
    {
        try
        {
            var ip        = ipAddress ?? _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var userId    = _user.UserId;
            var userName  = _user.UserName  ?? string.Empty;
            var userEmail = _user.UserEmail ?? string.Empty;

            // For Login/LoginFailed events the user is not yet authenticated —
            // fall back to entityName (which is the email that was submitted)
            if (string.IsNullOrEmpty(userEmail) && action is AuditActions.Login or AuditActions.LoginFailed)
                userEmail = entityName ?? string.Empty;

            var entry = new AuditLog
            {
                UserId     = userId,
                UserName   = userName,
                UserEmail  = userEmail,
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
            _log.LogWarning(ex, "[Audit] Failed to write audit log entry for action={Action} module={Module}.", action, module);
        }
    }
}
