namespace RTSErp.Domain.Enums;

// ── Inventory ─────────────────────────────────────────────────────────────────

public enum InventoryMovementType
{
    OpeningBalance     = 1,
    PurchaseReceipt    = 2,
    SalesIssue         = 3,
    TransferOut        = 4,
    TransferIn         = 5,
    AdjustmentIn       = 6,
    AdjustmentOut      = 7,
    ReturnFromCustomer = 8,
    ReturnToSupplier   = 9,
}

// ── Purchase Orders ───────────────────────────────────────────────────────────

public enum PurchaseOrderStatus
{
    Draft              = 1,
    Approved           = 2,
    PartiallyReceived  = 3,
    Received           = 4,
    Cancelled          = 5,
}

// ── Sales Orders ──────────────────────────────────────────────────────────────

public enum SalesOrderStatus
{
    Draft              = 1,
    Approved           = 2,
    PartiallyDelivered = 3,
    Delivered          = 4,
    Cancelled          = 5,
}

// ── Invoices ──────────────────────────────────────────────────────────────────

public enum InvoiceStatus
{
    Draft            = 1,
    Posted           = 2,
    PartiallyPaid    = 3,
    Paid             = 4,
    Cancelled        = 5,
}

public enum InvoiceType
{
    Standard = 1,   // Egyptian ETA invoice type B
    Credit   = 2,   // Credit note
    Debit    = 3,   // Debit note
}

// ── Payments ──────────────────────────────────────────────────────────────────

public enum PaymentMethod
{
    Bank   = 1,
    Cheque = 2,
    Cash   = 3,
}

public enum PaymentStatus
{
    Draft     = 1,
    Posted    = 2,
    Cancelled = 3,
}

// ── Bank Transactions ─────────────────────────────────────────────────────────

public enum BankTransactionType
{
    Deposit          = 1,
    Withdrawal       = 2,
    Transfer         = 3,
    CustomerReceipt  = 4,
    SupplierPayment  = 5,
}

// ── Cheques ───────────────────────────────────────────────────────────────────

public enum ChequeStatus
{
    Received   = 1,
    Deposited  = 2,
    Cleared    = 3,
    Bounced    = 4,
    Cancelled  = 5,
}

// ── Commission ────────────────────────────────────────────────────────────────

public enum CommissionStatus
{
    Pending   = 1,
    Approved  = 2,
    Paid      = 3,
    Cancelled = 4,
}

// ── E-Invoice ─────────────────────────────────────────────────────────────────

public enum EInvoiceSubmissionStatus
{
    NotSubmitted = 0,
    Pending      = 1,
    Submitted    = 2,
    Accepted     = 3,
    Rejected     = 4,
    Cancelled    = 5,
}
