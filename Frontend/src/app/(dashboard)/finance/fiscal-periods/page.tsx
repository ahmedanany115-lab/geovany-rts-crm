"use client";

import { useState } from "react";
import { AlertCircle, LockKeyhole, UnlockKeyhole } from "lucide-react";
import { useCloseFiscalPeriod, useCreateFiscalPeriod, useFiscalPeriods, useOpenFiscalPeriod } from "@/features/finance/hooks";
import { FiscalPeriodStatus } from "@/features/finance/types";

export default function FiscalPeriodsPage() {
  const { data: periods, isLoading, isError } = useFiscalPeriods();
  const createPeriod = useCreateFiscalPeriod();
  const closePeriod  = useCloseFiscalPeriod();
  const openPeriod   = useOpenFiscalPeriod();

  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: "", startDate: "", endDate: "" });
  const [actionError, setActionError] = useState<string | null>(null);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setActionError(null);
    try {
      await createPeriod.mutateAsync(form);
      setShowForm(false);
      setForm({ name: "", startDate: "", endDate: "" });
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to create period.");
    }
  }

  async function handleClose(id: string) {
    setActionError(null);
    try { await closePeriod.mutateAsync(id); }
    catch (err) { setActionError(err instanceof Error ? err.message : "Failed to close period."); }
  }

  async function handleOpen(id: string) {
    setActionError(null);
    try { await openPeriod.mutateAsync(id); }
    catch (err) { setActionError(err instanceof Error ? err.message : "Failed to open period."); }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Fiscal Periods</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Manage accounting periods. Closed periods prevent new entries from being posted.
          </p>
        </div>
        <button onClick={() => setShowForm(!showForm)}
          className="flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90">
          + New Period
        </button>
      </div>

      {actionError && (
        <div className="flex items-center gap-3 rounded-lg border border-danger/30 bg-danger/5 p-3 text-sm">
          <AlertCircle className="h-4 w-4 shrink-0 text-danger" /> {actionError}
        </div>
      )}

      {showForm && (
        <form onSubmit={handleCreate} className="rounded-lg border bg-background p-4 space-y-4">
          <h2 className="font-medium">New Fiscal Period</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div className="space-y-1">
              <label className="text-sm font-medium">Name *</label>
              <input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                placeholder="e.g. FY2026"
                className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium">Start Date *</label>
              <input required type="date" value={form.startDate}
                onChange={e => setForm(f => ({ ...f, startDate: e.target.value }))}
                className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium">End Date *</label>
              <input required type="date" value={form.endDate}
                onChange={e => setForm(f => ({ ...f, endDate: e.target.value }))}
                className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>
          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => setShowForm(false)}
              className="rounded-md border px-4 py-2 text-sm hover:bg-accent">Cancel</button>
            <button type="submit" disabled={createPeriod.isPending}
              className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50">
              {createPeriod.isPending ? "Creating…" : "Create Period"}
            </button>
          </div>
        </form>
      )}

      {isLoading && <div className="h-40 animate-pulse rounded-lg bg-accent/40" />}
      {isError && (
        <div className="flex items-center gap-3 rounded-lg border border-danger/30 bg-danger/5 p-4 text-sm">
          <AlertCircle className="h-4 w-4 shrink-0 text-danger" /> Failed to load fiscal periods.
        </div>
      )}

      {periods && (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-full text-sm">
            <thead className="bg-accent/40 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Start</th>
                <th className="px-4 py-3">End</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {periods.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-10 text-center text-muted-foreground">
                    No fiscal periods yet.
                  </td>
                </tr>
              )}
              {periods.map(p => (
                <tr key={p.id} className="hover:bg-accent/20">
                  <td className="px-4 py-3 font-medium">{p.name}</td>
                  <td className="px-4 py-3 text-muted-foreground">{p.startDate}</td>
                  <td className="px-4 py-3 text-muted-foreground">{p.endDate}</td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${
                      p.isClosed ? "bg-accent text-muted-foreground" : "bg-emerald/10 text-emerald"
                    }`}>
                      {p.statusName}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    {!p.isClosed ? (
                      <button onClick={() => handleClose(p.id)} disabled={closePeriod.isPending}
                        title="Close period"
                        className="flex items-center gap-1 ml-auto text-xs text-warning hover:underline disabled:opacity-50">
                        <LockKeyhole className="h-4 w-4" /> Close
                      </button>
                    ) : (
                      <button onClick={() => handleOpen(p.id)} disabled={openPeriod.isPending}
                        title="Re-open period"
                        className="flex items-center gap-1 ml-auto text-xs text-muted-foreground hover:underline disabled:opacity-50">
                        <UnlockKeyhole className="h-4 w-4" /> Re-open
                      </button>
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
