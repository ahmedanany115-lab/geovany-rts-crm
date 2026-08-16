"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  accountsApi,
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
