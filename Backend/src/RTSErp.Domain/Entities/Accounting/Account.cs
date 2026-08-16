using RTSErp.Domain.Common;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Accounting;

public class Account : BaseEntity
{
    public string Code { get; set; } = string.Empty;       // e.g. 1100
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }                    // Arabic name
    public AccountType AccountType { get; set; }
    public bool IsGroup { get; set; }                       // true = parent/group, false = posting leaf
    public bool IsActive { get; set; } = true;

    // Hierarchy
    public Guid? ParentId { get; set; }
    public Account? Parent { get; set; }
    public ICollection<Account> Children { get; set; } = [];

    // Currency (optional: if null, uses base currency)
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    // Navigation
    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = [];
}
