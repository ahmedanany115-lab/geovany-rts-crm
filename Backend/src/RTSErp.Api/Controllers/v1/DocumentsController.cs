using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Documents;

namespace RTSErp.Api.Controllers.v1;

/// <summary>
/// Company documents management.
/// Since we don't have cloud storage wired, documents are stored as
/// metadata only (the file bytes are NOT persisted server-side in this
/// version — clients provide a description and the UI stores the file
/// locally or references an external URL).
/// </summary>
[Authorize]
public class DocumentsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? category,
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var q = db.CompanyDocuments.Where(d => !d.IsDeleted);
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(d => d.Category == category);

        var docs = await q
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new
            {
                d.Id, d.Name, d.Category, d.Description, d.DocumentNumber,
                d.OriginalName, d.ContentType, d.FileSizeBytes,
                d.StoragePath, d.UploadedByName, d.ExpiryDate, d.IsPublic,
                d.CreatedAt,
                isExpired = d.ExpiryDate.HasValue && d.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow),
            })
            .ToListAsync(ct);

        return Ok(docs);
    }

    [HttpGet("categories")]
    public IActionResult Categories()
        => Ok(DocumentCategories.All);

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> Create(
        [FromBody] CreateDocumentRequest req,
        [FromServices] IApplicationDbContext db,
        [FromServices] ICurrentUserService user,
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
        return Ok(new { doc.Id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Accountant")]
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

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IApplicationDbContext db,
        CancellationToken ct)
    {
        var doc = await db.CompanyDocuments.FindAsync([id], ct);
        if (doc is null || doc.IsDeleted) return NotFound();

        doc.IsDeleted  = true;
        doc.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    public record CreateDocumentRequest(
        string Name,
        string Category,
        string? Description,
        string? DocumentNumber,
        string? FileName,
        string? OriginalName,
        string? ContentType,
        long FileSizeBytes,
        string? StoragePath,
        string? UploadedByName,
        DateOnly? ExpiryDate,
        bool IsPublic = true);
}
