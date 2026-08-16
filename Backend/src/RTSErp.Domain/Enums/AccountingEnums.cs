namespace RTSErp.Domain.Enums;

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    CostOfSales = 5,
    Expense = 6
}

public enum JournalEntryStatus
{
    Draft = 1,
    Posted = 2,
    Reversed = 3
}

public enum FiscalPeriodStatus
{
    Open = 1,
    Closed = 2
}

public enum ReferenceType
{
    Manual = 0,
    SalesInvoice = 1,
    PurchaseInvoice = 2,
    Payment = 3,
    Receipt = 4,
    BankTransfer = 5,
    Reversal = 6,
    OpeningBalance = 7,
    Commission = 8
}
