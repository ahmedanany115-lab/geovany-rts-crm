"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import {
  accountsApi,
  cashJournalApi,
  cashPaymentsApi,
  cashReceiptsApi,
  currenciesApi,
  fiscalPeriodsApi,
  journalEntriesApi,
  ledgerApi,
  trialBalanceApi,
} from "../api/financeApi";

// ── Accounts ──────────────────────────────────────────────────────────────────

export function useAccounts(params?: Parameters<typeof accountsApi.list>[0]) {
  return useQuery({
    queryKey: ["accounts", params],
    queryFn: () => accountsApi.list(params),
  });
}

export function useCreateAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: accountsApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["accounts"] }),
  });
}

export function useUpdateAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Parameters<typeof accountsApi.update>[1] }) =>
      accountsApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["accounts"] }),
  });
}

export function useDeleteAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => accountsApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["accounts"] }),
  });
}

export function useToggleAccountStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: accountsApi.toggleStatus,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["accounts"] }),
  });
}

// ── Journal Entries ───────────────────────────────────────────────────────────

export function useJournalEntries(params?: Parameters<typeof journalEntriesApi.list>[0]) {
  return useQuery({
    queryKey: ["journal-entries", params],
    queryFn: () => journalEntriesApi.list(params),
  });
}

export function useJournalEntry(id: string) {
  return useQuery({
    queryKey: ["journal-entry", id],
    queryFn: () => journalEntriesApi.get(id),
    enabled: !!id,
  });
}

export function useCreateJournalEntry() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: journalEntriesApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["journal-entries"] }),
  });
}

export function useUpdateJournalEntry() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: { id: string } & Parameters<typeof journalEntriesApi.update>[1]) =>
      journalEntriesApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["journal-entries"] });
      qc.invalidateQueries({ queryKey: ["journal-entry"] });
    },
  });
}

export function usePostJournalEntry() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: journalEntriesApi.post,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["journal-entries"] });
      qc.invalidateQueries({ queryKey: ["journal-entry"] });
    },
  });
}

export function useReverseJournalEntry() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: { id: string; reason: string; reversalDate: string }) =>
      journalEntriesApi.reverse(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["journal-entries"] }),
  });
}

// ── Ledger ────────────────────────────────────────────────────────────────────

export function useAccountLedger(
  accountId: string,
  params?: { fromDate?: string; toDate?: string }
) {
  return useQuery({
    queryKey: ["ledger", accountId, params],
    queryFn: () => ledgerApi.accountLedger(accountId, params),
    enabled: !!accountId,
  });
}

// ── Trial Balance ─────────────────────────────────────────────────────────────

export function useTrialBalance(params?: { fromDate?: string; toDate?: string }) {
  return useQuery({
    queryKey: ["trial-balance", params],
    queryFn: () => trialBalanceApi.get(params),
  });
}

// ── Fiscal Periods ────────────────────────────────────────────────────────────

export function useFiscalPeriods() {
  return useQuery({
    queryKey: ["fiscal-periods"],
    queryFn: fiscalPeriodsApi.list,
  });
}

export function useCreateFiscalPeriod() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: fiscalPeriodsApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["fiscal-periods"] }),
  });
}

export function useCloseFiscalPeriod() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: fiscalPeriodsApi.close,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["fiscal-periods"] }),
  });
}

export function useOpenFiscalPeriod() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: fiscalPeriodsApi.open,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["fiscal-periods"] }),
  });
}

// ── Currencies ────────────────────────────────────────────────────────────────

export function useCurrencies() {
  return useQuery({
    queryKey: ["currencies"],
    queryFn: currenciesApi.list,
  });
}

export function useCreateCurrency() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: currenciesApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["currencies"] }),
  });
}

export function useUpdateCurrency() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: { id: string; name: string; symbol: string; exchangeRate: number; isActive: boolean }) =>
      currenciesApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["currencies"] }),
  });
}

export function useDeleteJournalEntry() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiFetch<void>(`/journalentries/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["journal-entries"] }),
  });
}

// ── Cash Receipts ─────────────────────────────────────────────────────────────

export function useCashReceipts(params?: Parameters<typeof cashReceiptsApi.list>[0]) {
  return useQuery({
    queryKey: ["cash-receipts", params],
    queryFn: () => cashReceiptsApi.list(params),
  });
}

export function useCashReceipt(id: string) {
  return useQuery({
    queryKey: ["cash-receipt", id],
    queryFn: () => cashReceiptsApi.get(id),
    enabled: !!id,
  });
}

export function useCreateCashReceipt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: cashReceiptsApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["cash-receipts"] }),
  });
}

export function useUpdateCashReceipt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: { id: string } & Parameters<typeof cashReceiptsApi.update>[1]) =>
      cashReceiptsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["cash-receipts"] });
      qc.invalidateQueries({ queryKey: ["cash-receipt"] });
    },
  });
}

export function usePostCashReceipt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: cashReceiptsApi.post,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["cash-receipts"] }),
  });
}

export function useVoidCashReceipt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: cashReceiptsApi.void,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["cash-receipts"] }),
  });
}

export function useDeleteCashReceipt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: cashReceiptsApi.delete,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["cash-receipts"] }),
  });
}

// ── Cash Payments ─────────────────────────────────────────────────────────────

export function useCashPayments(params?: Parameters<typeof cashPaymentsApi.list>[0]) {
  return useQuery({
    queryKey: ["cash-payments", params],
    queryFn: () => cashPaymentsApi.list(params),
  });
}

export function useCashPayment(id: string) {
  return useQuery({
    queryKey: ["cash-payment", id],
    queryFn: () => cashPaymentsApi.get(id),
    enabled: !!id,
  });
}

export function useCreateCashPayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: cashPaymentsApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["cash-payments"] }),
  });
}

export function useUpdateCashPayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: { id: string } & Parameters<typeof cashPaymentsApi.update>[1]) =>
      cashPaymentsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["cash-payments"] });
      qc.invalidateQueries({ queryKey: ["cash-payment"] });
    },
  });
}

export function usePostCashPayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: cashPaymentsApi.post,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["cash-payments"] }),
  });
}

export function useVoidCashPayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: cashPaymentsApi.void,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["cash-payments"] }),
  });
}

export function useDeleteCashPayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: cashPaymentsApi.delete,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["cash-payments"] }),
  });
}

// ── Cash Journal ──────────────────────────────────────────────────────────────

export function useCashJournal(params?: Parameters<typeof cashJournalApi.get>[0]) {
  return useQuery({
    queryKey: ["cash-journal", params],
    queryFn: () => cashJournalApi.get(params!),
    enabled: !!params?.cashAccountId && !!params?.fromDate && !!params?.toDate,
  });
}
