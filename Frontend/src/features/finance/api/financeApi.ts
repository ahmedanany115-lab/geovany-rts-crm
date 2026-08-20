import { apiFetch } from "@/lib/api-client";
import type {
  AccountDto,
  AccountLedgerDto,
  CreateAccountRequest,
  CreateJournalEntryRequest,
  CurrencyDto,
  FiscalPeriodDto,
  JournalEntryDetailDto,
  JournalEntryListDto,
  TrialBalanceDto,
} from "../types";

// ── Accounts ──────────────────────────────────────────────────────────────────

export const accountsApi = {
  list: (params?: { accountType?: number; isGroup?: boolean; isActive?: boolean; parentId?: string; topLevelOnly?: boolean }) => {
    const qs = new URLSearchParams();
    if (params?.accountType !== undefined) qs.set("accountType", String(params.accountType));
    if (params?.isGroup !== undefined)     qs.set("isGroup",     String(params.isGroup));
    if (params?.isActive !== undefined)    qs.set("isActive",    String(params.isActive));
    if (params?.parentId)                  qs.set("parentId",    params.parentId);
    if (params?.topLevelOnly)              qs.set("topLevelOnly", "true");
    const query = qs.toString() ? `?${qs}` : "";
    return apiFetch<AccountDto[]>(`/accounts${query}`);
  },

  get: (id: string) => apiFetch<AccountDto>(`/accounts/${id}`),

  create: (data: CreateAccountRequest) =>
    apiFetch<{ id: string }>("/accounts", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  update: (id: string, data: Omit<CreateAccountRequest, "code" | "accountType">) =>
    apiFetch<void>(`/accounts/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),

  toggleStatus: (id: string) =>
    apiFetch<void>(`/accounts/${id}/toggle-status`, { method: "PATCH" }),
};

// ── Journal Entries ───────────────────────────────────────────────────────────

export const journalEntriesApi = {
  list: (params?: { status?: number; fromDate?: string; toDate?: string; fiscalPeriodId?: string; page?: number; pageSize?: number }) => {
    const qs = new URLSearchParams();
    if (params?.status !== undefined)  qs.set("status", String(params.status));
    if (params?.fromDate)              qs.set("fromDate", params.fromDate);
    if (params?.toDate)                qs.set("toDate",   params.toDate);
    if (params?.fiscalPeriodId)        qs.set("fiscalPeriodId", params.fiscalPeriodId);
    if (params?.page)                  qs.set("page", String(params.page));
    if (params?.pageSize)              qs.set("pageSize", String(params.pageSize));
    const query = qs.toString() ? `?${qs}` : "";
    return apiFetch<JournalEntryListDto[]>(`/journalentries${query}`);
  },

  get: (id: string) => apiFetch<JournalEntryDetailDto>(`/journalentries/${id}`),

  create: (data: CreateJournalEntryRequest) =>
    apiFetch<{ entryId: string; entryNumber: string }>("/journalentries", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  post: (id: string) =>
    apiFetch<{ entryId: string; entryNumber: string }>(`/journalentries/${id}/post`, {
      method: "POST",
    }),

  reverse: (id: string, data: { reason: string; reversalDate: string }) =>
    apiFetch<{ entryId: string; entryNumber: string }>(`/journalentries/${id}/reverse`, {
      method: "POST",
      body: JSON.stringify(data),
    }),
};

// ── Ledger ────────────────────────────────────────────────────────────────────

export const ledgerApi = {
  accountLedger: (accountId: string, params?: { fromDate?: string; toDate?: string }) => {
    const qs = new URLSearchParams();
    if (params?.fromDate) qs.set("fromDate", params.fromDate);
    if (params?.toDate)   qs.set("toDate",   params.toDate);
    const query = qs.toString() ? `?${qs}` : "";
    return apiFetch<AccountLedgerDto>(`/ledger/account/${accountId}${query}`);
  },
};

// ── Trial Balance ─────────────────────────────────────────────────────────────

export const trialBalanceApi = {
  get: (params?: { fromDate?: string; toDate?: string }) => {
    const qs = new URLSearchParams();
    if (params?.fromDate) qs.set("fromDate", params.fromDate);
    if (params?.toDate)   qs.set("toDate",   params.toDate);
    const query = qs.toString() ? `?${qs}` : "";
    return apiFetch<TrialBalanceDto>(`/trialbalance${query}`);
  },
};

// ── Fiscal Periods ────────────────────────────────────────────────────────────

export const fiscalPeriodsApi = {
  list: () => apiFetch<FiscalPeriodDto[]>("/fiscalperiods"),

  create: (data: { name: string; startDate: string; endDate: string }) =>
    apiFetch<{ id: string }>("/fiscalperiods", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  close: (id: string) =>
    apiFetch<void>(`/fiscalperiods/${id}/close`, { method: "POST" }),

  open: (id: string) =>
    apiFetch<void>(`/fiscalperiods/${id}/open`, { method: "POST" }),
};

// ── Currencies ────────────────────────────────────────────────────────────────

export const currenciesApi = {
  list: () => apiFetch<CurrencyDto[]>("/currencies"),

  create: (data: { code: string; name: string; symbol: string; exchangeRate: number }) =>
    apiFetch<{ id: string }>("/currencies", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  update: (id: string, data: { name: string; symbol: string; exchangeRate: number; isActive: boolean }) =>
    apiFetch<void>(`/currencies/${id}`, {
      method: "PUT",
      body: JSON.stringify({ id, ...data }),
    }),
};

export const taxRatesApi = {
  list: () => apiFetch<{ id: string; name: string; code: string; rate: number; isActive: boolean }[]>("/taxrates"),
};
