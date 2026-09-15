using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Documents;

public class CompanyDocument : BaseEntity
{
    public string Name        { get; set; } = string.Empty;
    public string Category    { get; set; } = string.Empty;   // CompanyRegistration, TaxCard, etc.
    public string? Description { get; set; }
    public string? DocumentNumber { get; set; }
    public string FileName    { get; set; } = string.Empty;   // stored filename (GUID-based)
    public string OriginalName { get; set; } = string.Empty;  // original upload name
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;   // relative path in uploads folder
    public string UploadedByName { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public bool IsPublic { get; set; } = true;   // visible to all staff
}

public static class DocumentCategories
{
    public const string CompanyRegistration = "Company Registration";
    public const string TaxCard             = "Tax Card";
    public const string VatCertificate      = "VAT Certificate";
    public const string CommercialReg       = "Commercial Registration";
    public const string Contract            = "Contracts";
    public const string BankDocument        = "Bank Documents";
    public const string Certification       = "Certifications";
    public const string PartnerCertificate  = "Partner Certificates";
    public const string TechnicalDocument   = "Technical Documents";
    public const string Other               = "Other";

    public static readonly string[] All =
    [
        CompanyRegistration, TaxCard, VatCertificate, CommercialReg,
        Contract, BankDocument, Certification, PartnerCertificate,
        TechnicalDocument, Other
    ];
}
