using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Accounting;

public class TaxRate : BaseEntity
{
    public string Code { get; set; } = string.Empty;       // e.g. VAT14
    public string Name { get; set; } = string.Empty;       // e.g. VAT 14%
    public decimal Rate { get; set; }                       // e.g. 0.14 (14%)
    public bool IsActive { get; set; } = true;

    // The accounting accounts where VAT is posted
    public Guid? InputTaxAccountId { get; set; }
    public Account? InputTaxAccount { get; set; }

    public Guid? OutputTaxAccountId { get; set; }
    public Account? OutputTaxAccount { get; set; }
}
