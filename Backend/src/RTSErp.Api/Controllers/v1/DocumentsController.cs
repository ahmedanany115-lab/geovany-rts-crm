using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Audit;
using RTSErp.Domain.Entities.Documents;

namespace RTSErp.Api.Controllers.v1;

/// <summary>
/// Company documents.
///
/// Permission model:
///   GET  /documents              → any authenticated user  ([Authorize])
///   GET  /documents/{id}/download → any authenticated user ([Authorize])
///   POST /documents              → Admin, Manager, Accountant, Marketing (can upload)
///   PUT  /documents/{id}         → Admin, Manager, Accountant, Marketing (can edit metadata)
///   DELETE /documents/{id}       → Admin, Manager only
/// </summary>
[Authorize]
[Microsoft.AspNetCore.Mvc.Route("api/v1/documents")]  // explicit lowercase — Railway/Linux is case-sensitive
public class DocumentsController : BaseApiController
{
    // ── List ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var q = db.CompanyDocuments.Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(d => d.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(d => d.Name.ToLower().Contains(s)
                           || d.Category.ToLower().Contains(s)
                           || (d.Description != null && d.Description.ToLower().Contains(s)));
        }

        var docs = await q
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new
            {
                d.Id, d.Name, d.Category, d.Description, d.DocumentNumber,
                d.OriginalName, d.ContentType, d.FileSizeBytes,
                d.StoragePath, d.UploadedByName, d.ExpiryDate, d.IsPublic,
                d.CreatedAt,
                isExpired = d.ExpiryDate.HasValue
                            && d.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow),
            })
            .ToListAsync(ct);

        return Ok(docs);
    }

    // ── Categories ────────────────────────────────────────────────────────────
    [HttpGet("categories")]
    public IActionResult Categories()
        => Ok(DocumentCategories.All);

    // ── Download / Open ───────────────────────────────────────────────────────
    // Any authenticated user may download. Authentication is enforced by [Authorize]
    // on the controller — unauthenticated requests are rejected with 401.
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        var doc = await db.CompanyDocuments
            .Where(d => d.Id == id && !d.IsDeleted)
            .Select(d => new { d.StoragePath, d.OriginalName, d.ContentType, d.Name })
            .FirstOrDefaultAsync(ct);

        if (doc is null) return NotFound();

        _ = audit.LogAsync(AuditActions.Downloaded, AuditModules.Documents,
            entityName: doc.Name, entityId: id, entityType: "CompanyDocument", ct: ct);
        // If StoragePath is an absolute URL, return a redirect (still auth-gated here)
        if (doc.StoragePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
         || doc.StoragePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(doc.StoragePath);
        }

        // Otherwise treat it as a local file path and stream it
        if (!string.IsNullOrEmpty(doc.StoragePath)
         && System.IO.File.Exists(doc.StoragePath))
        {
            var bytes = await System.IO.File.ReadAllBytesAsync(doc.StoragePath, ct);
            var ct2   = doc.ContentType.Length > 0
                          ? doc.ContentType
                          : "application/octet-stream";
            return File(bytes, ct2, doc.OriginalName);
        }

        // No local file — return the path for the client to open
        return Ok(new { storagePath = doc.StoragePath, originalName = doc.OriginalName });
    }

    // ── Create / Upload ───────────────────────────────────────────────────────
    // Upload permission: Admin, Manager, Accountant, Marketing
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Accountant,Marketing")]
    [Microsoft.AspNetCore.Mvc.DisableRequestSizeLimit]
    public async Task<IActionResult> Create(
        [FromBody] CreateDocumentRequest req,
        [FromServices] IApplicationDbContext db,
        [FromServices] ICurrentUserService user,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        var doc = new CompanyDocument
        {
            Name           = req.Name.Trim(),
            Category       = req.Category.Trim(),
            Description    = req.Description?.Trim(),
            DocumentNumber = req.DocumentNumber?.Trim(),
            FileName       = req.FileName ?? string.Empty,
            OriginalName   = req.OriginalName ?? req.Name,
            ContentType    = req.ContentType ?? "application/octet-stream",
            FileSizeBytes  = req.FileSizeBytes,
            StoragePath    = req.StoragePath ?? string.Empty,
            UploadedByName = req.UploadedByName ?? string.Empty,
            ExpiryDate     = req.ExpiryDate,
            IsPublic       = req.IsPublic,
            CreatedBy      = user.UserId,
        };

        db.CompanyDocuments.Add(doc);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(AuditActions.Uploaded, AuditModules.Documents,
            entityName: req.Name, entityId: doc.Id, entityType: "CompanyDocument", ct: ct);

        return Ok(new { doc.Id });
    }

    // ── Edit metadata ─────────────────────────────────────────────────────────
    // Edit permission: Admin, Manager, Accountant, Marketing
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Accountant,Marketing")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CreateDocumentRequest req,
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var doc = await db.CompanyDocuments.FindAsync([id], ct);
        if (doc is null || doc.IsDeleted) return NotFound();

        doc.Name           = req.Name.Trim();
        doc.Category       = req.Category.Trim();
        doc.Description    = req.Description?.Trim();
        doc.DocumentNumber = req.DocumentNumber?.Trim();
        doc.ExpiryDate     = req.ExpiryDate;
        doc.IsPublic       = req.IsPublic;
        doc.ModifiedAt     = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IApplicationDbContext db,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        var doc = await db.CompanyDocuments.FindAsync([id], ct);
        if (doc is null || doc.IsDeleted) return NotFound();

        doc.IsDeleted  = true;
        doc.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        _ = audit.LogAsync(AuditActions.Deleted, AuditModules.Documents,
            entityName: doc.Name, entityId: id, entityType: "CompanyDocument", ct: ct);
        return NoContent();
    }

    // ── Request DTO ───────────────────────────────────────────────────────────
    public record CreateDocumentRequest(
        string   Name,
        string   Category,
        string?  Description,
        string?  DocumentNumber,
        string?  FileName,
        string?  OriginalName,
        string?  ContentType,
        long     FileSizeBytes,
        string?  StoragePath,
        string?  UploadedByName,
        DateOnly? ExpiryDate,
        bool     IsPublic = true);
}
