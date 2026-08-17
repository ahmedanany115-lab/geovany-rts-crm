using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace RTSErp.Infrastructure.Services;

/// <summary>
/// Development/mock implementation of IEInvoiceService.
/// Returns a simulated submission UUID without connecting to the Egyptian Tax Authority.
/// Replace with real ETA SDK integration when credentials are available.
/// </summary>
public class MockEInvoiceService : IEInvoiceService
{
    private readonly ILogger<MockEInvoiceService> _logger;
    public MockEInvoiceService(ILogger<MockEInvoiceService> logger) => _logger = logger;

    public Task<EInvoiceSubmissionResult> SubmitInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        _logger.LogWarning("MockEInvoiceService: SubmitInvoiceAsync called for {InvoiceId}. This is a dev mock — no real ETA submission.", invoiceId);
        var uuid = $"MOCK-{Guid.NewGuid():N}".ToUpperInvariant();
        return Task.FromResult(EInvoiceSubmissionResult.Success(uuid, $"EXT-{invoiceId:N}".ToUpperInvariant()));
    }

    public Task<EInvoiceSubmissionResult> CancelInvoiceAsync(Guid invoiceId, string reason, CancellationToken ct = default)
    {
        _logger.LogWarning("MockEInvoiceService: CancelInvoiceAsync called for {InvoiceId}.", invoiceId);
        return Task.FromResult(EInvoiceSubmissionResult.Success("CANCELLED", $"EXT-{invoiceId:N}".ToUpperInvariant()));
    }

    public Task<EInvoiceSubmissionStatus> GetSubmissionStatusAsync(string externalInvoiceId, CancellationToken ct = default)
    {
        _logger.LogWarning("MockEInvoiceService: GetSubmissionStatusAsync called for {ExternalId}.", externalInvoiceId);
        return Task.FromResult(EInvoiceSubmissionStatus.Submitted);
    }
}
