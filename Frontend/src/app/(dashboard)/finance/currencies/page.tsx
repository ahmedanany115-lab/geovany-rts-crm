"use client";

import { useState } from "react";
import { AlertCircle } from "lucide-react";
import { useCreateCurrency, useCurrencies, useUpdateCurrency } from "@/features/finance/hooks";
import type { CurrencyDto } from "@/features/finance/types";

export default function CurrenciesPage() {
  const { data: currencies, isLoading, isError } = useCurrencies();
  const createCurrency = useCreateCurrency();
  const updateCurrency = useUpdateCurrency();

  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState({ code: "", name: "", symbol: "", exchangeRate: 1, isActive: true });
  const [actionError, setActionError] = useState<string | null>(null);

  function startEdit(c: CurrencyDto) {
    setEditingId(c.id);
    setForm({ code: c.code, name: c.name, symbol: c.symbol, exchangeRate: c.exchangeRate, isActive: c.isActive });
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setActionError(null);
    try {
      if (editingId) {
        await updateCurrency.mutateAsync({ id: editingId, ...form });
      } else {
        await createCurrency.mutateAsync(form);
      }
      setShowForm(false);
      setEditingId(null);
      setForm({ code: "", name: "", symbol: "", exchangeRate: 1, isActive: true });
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Operation failed.");
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Currencies</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Manage supported currencies and their exchange rates relative to the base currency (EGP).
          </p>
        </div>
        <button onClick={() => { setShowForm(!showForm); setEditingId(null); setForm({ code: "", name: "", symbol: "", exchangeRate: 1, isActive: true }); }}
          className="flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90">
          + New Currency
        </button>
      </div>

      {actionError && (
        <div className="flex items-center gap-3 rounded-lg border border-danger/30 bg-danger/5 p-3 text-sm">
          <AlertCircle className="h-4 w-4 shrink-0 text-danger" /> {actionError}
        </div>
      )}

      {showForm && (
        <form onSubmit={handleSubmit} className="rounded-lg border bg-background p-4 space-y-4">
          <h2 className="font-medium">{editingId ? "Edit Currency" : "New Currency"}</h2>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            <div className="space-y-1">
              <label className="text-sm font-medium">Code *</label>
              <input required value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value.toUpperCase() }))}
                disabled={!!editingId} placeholder="USD" maxLength={3}
                className="w-full rounded-md border bg-background px-3 py-2 text-sm uppercase focus:outline-none focus:ring-2 focus:ring-primary disabled:opacity-50"
              />
            </div>
            <div className="space-y-1 sm:col-span-2">
              <label className="text-sm font-medium">Name *</label>
              <input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                placeholder="US Dollar"
                className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium">Symbol *</label>
              <input required value={form.symbol} onChange={e => setForm(f => ({ ...f, symbol: e.target.value }))}
                placeholder="$" maxLength={10}
                className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium">Exchange Rate (vs EGP) *</label>
              <input required type="number" step="0.0001" min="0.0001"
                value={form.exchangeRate}
                onChange={e => setForm(f => ({ ...f, exchangeRate: parseFloat(e.target.value) }))}
                className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            {editingId && (
              <div className="flex items-end pb-2">
                <label className="flex items-center gap-2 text-sm">
                  <input type="checkbox" checked={form.isActive}
                    onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))}
                    className="h-4 w-4 rounded border"
                  />
                  Active
                </label>
              </div>
            )}
          </div>
          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => setShowForm(false)}
              className="rounded-md border px-4 py-2 text-sm hover:bg-accent">Cancel</button>
            <button type="submit" disabled={createCurrency.isPending || updateCurrency.isPending}
              className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50">
              {createCurrency.isPending || updateCurrency.isPending ? "Saving…" : editingId ? "Update" : "Create"}
            </button>
          </div>
        </form>
      )}

      {isLoading && <div className="h-40 animate-pulse rounded-lg bg-accent/40" />}
      {isError && (
        <div className="flex items-center gap-3 rounded-lg border border-danger/30 bg-danger/5 p-4 text-sm">
          <AlertCircle className="h-4 w-4 shrink-0 text-danger" /> Failed to load currencies.
        </div>
      )}

      {currencies && (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-full text-sm">
            <thead className="bg-accent/40 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-3">Code</th>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Symbol</th>
                <th className="px-4 py-3 text-right">Exchange Rate</th>
                <th className="px-4 py-3">Base</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {currencies.map(c => (
                <tr key={c.id} className="hover:bg-accent/20">
                  <td className="px-4 py-3 font-mono font-bold">{c.code}</td>
                  <td className="px-4 py-3">{c.name}</td>
                  <td className="px-4 py-3 font-mono">{c.symbol}</td>
                  <td className="px-4 py-3 text-right font-mono">
                    {c.isBaseCurrency ? "1.0000" : c.exchangeRate.toFixed(4)}
                  </td>
                  <td className="px-4 py-3">
                    {c.isBaseCurrency && (
                      <span className="inline-flex rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
                        Base
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${
                      c.isActive ? "bg-emerald/10 text-emerald" : "bg-accent text-muted-foreground"
                    }`}>
                      {c.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    {!c.isBaseCurrency && (
                      <button onClick={() => startEdit(c)}
                        className="text-xs text-primary hover:underline">Edit</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
