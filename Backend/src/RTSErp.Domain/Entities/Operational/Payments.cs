using RTSErp.Domain.Common;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Domain.Entities.Operational;

// ── Customer Payment ──────────────────────────────────────────────────────────

public class CustomerPayment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public BusinessPartner Customer { get; set; } = null!;

    public DateOnly PaymentDate { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public decimal Amount { get; set; }
    public decimal AmountBase { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Draft;

    // Bank account (for Bank payments)
    public Guid? BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }

    // Cheque (for cheque payments)
    public Guid? ChequeId { get; set; }
    public Cheque? Cheque { get; set; }

    public string? Notes { get; set; }
    public Guid? JournalEntryId { get; set; }

    // Payment allocations
    public ICollection<CustomerInvoice> Invoices { get; set; } = [];
}

// ── Supplier Payment ──────────────────────────────────────────────────────────

public class SupplierPayment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }
    public BusinessPartner Supplier { get; set; } = null!;

    public DateOnly PaymentDate { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public decimal Amount { get; set; }
    public decimal AmountBase { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Draft;

    public Guid? BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }

    public string? Notes { get; set; }
    public Guid? JournalEntryId { get; set; }

    public ICollection<SupplierInvoice> Invoices { get; set; } = [];
}

// ── Bank Transaction ──────────────────────────────────────────────────────────

public class BankTransaction : BaseEntity
{
    public string TransactionNumber { get; set; } = string.Empty;

    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    public BankTransactionType TransactionType { get; set; }
    public DateOnly TransactionDate { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public decimal Amount { get; set; }
    public decimal AmountBase { get; set; }

    public string? Description { get; set; }
    public string? Reference { get; set; }

    // For transfers: destination account
    public Guid? DestinationBankAccountId { get; set; }
    public BankAccount? DestinationBankAccount { get; set; }

    public Guid? JournalEntryId { get; set; }
}

// ── Cheque ────────────────────────────────────────────────────────────────────

public class Cheque : BaseEntity
{
    public string ChequeNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public BusinessPartner Customer { get; set; } = null!;

    public string BankName { get; set; } = string.Empty;

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal AmountBase { get; set; }

    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly ReceivedDate { get; set; }

    // Where the cheque was deposited (filled when status = Deposited)
    public Guid? BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }

    public ChequeStatus Status { get; set; } = ChequeStatus.Received;
    public string? Notes { get; set; }

    // Accounting entries
    public Guid? ReceiptJournalEntryId { get; set; }    // on receipt
    public Guid? DepositJournalEntryId { get; set; }    // on deposit
    public Guid? BounceJournalEntryId { get; set; }     // on bounce
}

// ── Sales Commission ──────────────────────────────────────────────────────────

public class SalesCommission : BaseEntity
{
    public Guid? SalespersonId { get; set; }
    public string SalespersonName { get; set; } = string.Empty;

    public Guid CommissionRateId { get; set; }
    public CommissionRate CommissionRate { get; set; } = null!;

    public decimal Rate { get; set; }         // snapshot of rate at time of creation
    public decimal BaseSalesAmount { get; set; }
    public decimal CommissionAmount { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;

    // Source document
    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public Guid? CustomerInvoiceId { get; set; }
    public CustomerInvoice? CustomerInvoice { get; set; }

    // Accounting
    public Guid? JournalEntryId { get; set; }
}
