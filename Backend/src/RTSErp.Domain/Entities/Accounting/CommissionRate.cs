using RTSErp.Domain.Common;

namespace RTSErp.Domain.Entities.Accounting;

/// <summary>
/// Configurable sales commission rate.
/// Default: 1.5% of total sale.
/// Future Sales module reads this to post Commission Expense / Commission Payable.
/// </summary>
public class CommissionRate : BaseEntity
{
    public string Name { get; set; } = string.Empty;       // e.g. "Default Commission"
    public decimal Rate { get; set; }                       // e.g. 0.015 (1.5%)
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    // The accounting accounts for commission posting
    public Guid? CommissionExpenseAccountId { get; set; }
    public Account? CommissionExpenseAccount { get; set; }

    public Guid? CommissionPayableAccountId { get; set; }
    public Account? CommissionPayableAccount { get; set; }
}
