using RTSErp.Domain.Enums;

namespace RTSErp.Application.Common.Interfaces;

/// <summary>
/// Abstraction for Egyptian Tax Authority (ETA) e-invoice submission.
/// Currently returns a development mock. Real integration will be implemented
/// when ETA sandbox credentials are available.
/// </summary>
public interface IEInvoiceService
{
    /// <summary>
    /// Submits an invoice to the ETA and returns the submission UUID.
    /// </summary>
    Task<EInvoiceSubmissionResult> SubmitInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a previously submitted invoice.
    /// </summary>
    Task<EInvoiceSubmissionResult> CancelInvoiceAsync(
        Guid invoiceId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Polls the ETA for the current submission status.
    /// </summary>
    Task<EInvoiceSubmissionStatus> GetSubmissionStatusAsync(
        string externalInvoiceId,
        CancellationToken cancellationToken = default);
}

public class EInvoiceSubmissionResult
{
    public bool Succeeded { get; set; }
    public string? ExternalInvoiceId { get; set; }
    public string? UUID { get; set; }
    public EInvoiceSubmissionStatus Status { get; set; }
    public string[]? Errors { get; set; }

    public static EInvoiceSubmissionResult Success(string uuid, string externalId) => new()
    {
        Succeeded = true, UUID = uuid, ExternalInvoiceId = externalId,
        Status = EInvoiceSubmissionStatus.Submitted
    };

    public static EInvoiceSubmissionResult Failure(params string[] errors) => new()
    {
        Succeeded = false, Errors = errors, Status = EInvoiceSubmissionStatus.Rejected
    };
}
