"use client";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";

interface CurrencyDto { id: string; code: string; name: string; symbol: string; }

/**
 * Returns the ID of the EGP currency from the API.
 * Falls back to empty string if not yet loaded.
 */
export function useEgpCurrencyId(): string {
  const { data } = useQuery({
    queryKey: ["currencies-list"],
    queryFn: () => apiFetch<CurrencyDto[]>("/currencies"),
    staleTime: Infinity, // currencies rarely change
  });
  return data?.find(c => c.code === "EGP")?.id ?? "";
}

export function useCurrencyList(): CurrencyDto[] {
  const { data } = useQuery({
    queryKey: ["currencies-list"],
    queryFn: () => apiFetch<CurrencyDto[]>("/currencies"),
    staleTime: Infinity,
  });
  return data ?? [];
}
