// ── Enums (must match backend) ────────────────────────────────────────────────

export enum AccountType {
  Asset = 1,
  Liability = 2,
  Equity = 3,
  Revenue = 4,
  CostOfSales = 5,
  Expense = 6,
}

export const AccountTypeLabels: Record<AccountType, string> = {
  [AccountType.Asset]:       "Asset",
  [AccountType.Liability]:   "Liability",
  [AccountType.Equity]:      "Equity",
  [AccountType.Revenue]:     "Revenue",
  [AccountType.CostOfSales]: "Cost of Sales",
  [AccountType.Expense]:     "Expense",
};

export enum JournalEntryStatus {
  Draft    = 1,
  Posted   = 2,
  Reversed = 3,
}

export const JournalEntryStatusLabels: Record<JournalEntryStatus, string> = {
  [JournalEntryStatus.Draft]:    "Draft",
  [JournalEntryStatus.Posted]:   "Posted",
  [JournalEntryStatus.Reversed]: "Reversed",
};

export enum FiscalPeriodStatus {
  Open   = 1,
  Closed = 2,
}

// ── Account ───────────────────────────────────────────────────────────────────

export interface AccountDto {
  id: string;
  code: string;
  name: string;
  nameAr?: string;
  accountType: AccountType;
  accountTypeName: string;
  isGroup: boolean;
  isActive: boolean;
  parentId?: string;
  parentCode?: string;
  parentName?: string;
  currencyId?: string;
  currencyCode?: string;
  childCount: number;
}

export interface CreateAccountRequest {
  code: string;
  name: string;
  nameAr?: string;
  accountType: AccountType;
  isGroup: boolean;
  parentId?: string;
  currencyId?: string;
}

// ── Journal Entry ─────────────────────────────────────────────────────────────

export interface JournalEntryListDto {
  id: string;
  entryNumber: string;
  entryDate: string;
  description: string;
  status: JournalEntryStatus;
  statusName: string;
  currencyCode: string;
  exchangeRate: number;
  totalDebit: number;
  totalCredit: number;
  fiscalPeriodName: string;
  referenceNumber?: string;
  createdAt: string;
}

export interface JournalEntryLineDetailDto {
  id: string;
  accountId: string;
  accountCode: string;
  accountName: string;
  debit: number;
  credit: number;
  debitBase: number;
  creditBase: number;
  currencyCode: string;
  exchangeRate: number;
  description?: string;
  sortOrder: number;
}

export interface JournalEntryDetailDto extends JournalEntryListDto {
  fiscalPeriodName: string;
  referenceType: number;
  reversedByEntryId?: string;
  reversesEntryId?: string;
  lines: JournalEntryLineDetailDto[];
  isBalanced: boolean;
}

export interface CreateJournalEntryLineDto {
  accountId: string;
  debit: number;
  credit: number;
  description?: string;
  sortOrder: number;
}

export interface CreateJournalEntryRequest {
  entryDate: string;
  description: string;
  currencyId: string;
  exchangeRate: number;
  postImmediately: boolean;
  lines: CreateJournalEntryLineDto[];
}

// ── Ledger ────────────────────────────────────────────────────────────────────

export interface LedgerLineDto {
  journalEntryId: string;
  entryNumber: string;
  entryDate: string;
  description: string;
  lineDescription?: string;
  debit: number;
  credit: number;
  runningBalance: number;
}

export interface AccountLedgerDto {
  accountId: string;
  accountCode: string;
  accountName: string;
  fromDate?: string;
  toDate?: string;
  openingBalance: number;
  lines: LedgerLineDto[];
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}

// ── Trial Balance ─────────────────────────────────────────────────────────────

export interface TrialBalanceLineDto {
  accountId: string;
  accountCode: string;
  accountName: string;
  accountTypeName: string;
  openingDebit: number;
  openingCredit: number;
  periodDebit: number;
  periodCredit: number;
  closingDebit: number;
  closingCredit: number;
}

export interface TrialBalanceDto {
  fromDate?: string;
  toDate?: string;
  lines: TrialBalanceLineDto[];
  totalOpeningDebit: number;
  totalOpeningCredit: number;
  totalPeriodDebit: number;
  totalPeriodCredit: number;
  totalClosingDebit: number;
  totalClosingCredit: number;
  isBalanced: boolean;
}

// ── Fiscal Period ─────────────────────────────────────────────────────────────

export interface FiscalPeriodDto {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  status: FiscalPeriodStatus;
  statusName: string;
  isClosed: boolean;
  createdAt: string;
}

// ── Currency ──────────────────────────────────────────────────────────────────

export interface CurrencyDto {
  id: string;
  code: string;
  name: string;
  symbol: string;
  exchangeRate: number;
  isBaseCurrency: boolean;
  isActive: boolean;
}
