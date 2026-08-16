"use client";

import { useState } from "react";
import { AlertCircle, BookOpen, CheckCircle, RefreshCw, RotateCcw } from "lucide-react";
import { useJournalEntries, usePostJournalEntry, useReverseJournalEntry } from "@/features/finance/hooks";
import { JournalEntryStatus, JournalEntryStatusLabels } from "@/features/finance/types";

const STATUS_STYLES: Record<JournalEntryStatus, string> = {
  [JournalEntryStatus.Draft]:    "bg-warning/10 text-warning",
  [JournalEntryStatus.Posted]:   "bg-emerald/10 text-emerald",
  [JournalEntryStatus.Reversed]: "bg-accent text-muted-foreground",
};

function fmt(n: number) {
  return n.toLocaleString("en-EG", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export default function JournalEntriesPage() {
  const [statusFilter, setStatusFilter] = useState<number | undefined>();
  const { data: entries, isLoading, isError, refetch } =
    useJournalEntries({ status: statusFilter });

  const postEntry    = usePostJournalEntry();
  const reverseEntry = useReverseJournalEntry();

  const [reverseTarget, setReverseTarget] = useState<string | null>(null);
  const [reverseReason, setReverseReason] = useState("");
  const [reverseDate, setReverseDate] = useState(new Date().toISOString().slice(0, 10));
  const [actionError, setActionError] = useState<string | null>(null);

  async function handlePost(id: string) {
    setActionError(null);
    try {
      await postEntry.mutateAsync(id);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to post entry.");
    }
  }

  async function handleReverse() {
    if (!reverseTarget) return;
    setActionError(null);
    try {
      await reverseEntry.mutateAsync({
        id: reverseTarget,
        reason: reverseReason,
        reversalDate: reverseDate,
      });
      setReverseTarget(null);
      setReverseReason("");
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to reverse entry.");
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Journal Entries</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            All double-entry accounting transactions. Draft entries can be posted; posted entries can only be reversed.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <select
            value={statusFilter ?? ""}
            onChange={e => setStatusFilter(e.target.value ? Number(e.target.value) : undefined)}
            className="rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
          >
            <option value="">All statuses</option>
            {Object.entries(JournalEntryStatusLabels).map(([k, v]) => (
              <option key={k} value={k}>{v}</option>
            ))}
          </select>
        </div>
      </div>

      {actionError && (
        <div className="flex items-center gap-3 rounded-lg border border-danger/30 bg-danger/5 p-3 text-sm">
          <AlertCircle className="h-4 w-4 shrink-0 text-danger" /> {actionError}
        </div>
      )}

      {/* Reversal modal */}
      {reverseTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="w-full max-w-md rounded-lg border bg-background p-6 shadow-xl space-y-4">
            <h2 className="text-lg font-semibold">Reverse Journal Entry</h2>
            <div className="space-y-3">
              <div className="space-y-1">
                <label className="text-sm font-medium">Reason *</label>
                <textarea value={reverseReason} onChange={e => setReverseReason(e.target.value)}
                  rows={3} placeholder="Explain why this entry is being reversed..."
                  className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>
              <div className="space-y-1">
                <label className="text-sm font-medium">Reversal Date *</label>
                <input type="date" value={reverseDate} onChange={e => setReverseDate(e.target.value)}
                  className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <button onClick={() => setReverseTarget(null)}
                className="rounded-md border px-4 py-2 text-sm hover:bg-accent">Cancel</button>
              <button
                onClick={handleReverse}
                disabled={!reverseReason.trim() || reverseEntry.isPending}
                className="rounded-md bg-danger px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
              >
                {reverseEntry.isPending ? "Reversing…" : "Confirm Reversal"}
              </button>
            </div>
          </div>
        </div>
      )}

      {isLoading && (
        <div className="space-y-2">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="h-14 animate-pulse rounded-lg bg-accent/40" />
          ))}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 rounded-lg border border-danger/30 bg-danger/5 p-4 text-sm">
          <AlertCircle className="h-4 w-4 shrink-0 text-danger" /> Failed to load journal entries.
          <button onClick={() => refetch()} className="ml-auto">
            <RefreshCw className="h-4 w-4" />
          </button>
        </div>
      )}

      {entries && (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-full text-sm">
            <thead className="bg-accent/40 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-3">Entry #</th>
                <th className="px-4 py-3">Date</th>
                <th className="px-4 py-3">Description</th>
                <th className="px-4 py-3">Period</th>
                <th className="px-4 py-3 text-right">Debit</th>
                <th className="px-4 py-3 text-right">Credit</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {entries.length === 0 && (
                <tr>
                  <td colSpan={8} className="px-4 py-12 text-center text-muted-foreground">
                    No journal entries found.
                  </td>
                </tr>
              )}
              {entries.map(entry => (
                <tr key={entry.id} className="hover:bg-accent/20 transition-colors">
                  <td className="px-4 py-3 font-mono text-sm font-medium">
                    <div className="flex items-center gap-1">
                      <BookOpen className="h-3 w-3 text-muted-foreground" />
                      {entry.entryNumber}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{entry.entryDate}</td>
                  <td className="px-4 py-3 max-w-xs truncate">{entry.description}</td>
                  <td className="px-4 py-3 text-muted-foreground text-xs">{entry.fiscalPeriodName}</td>
                  <td className="px-4 py-3 text-right font-mono">{fmt(entry.totalDebit)}</td>
                  <td className="px-4 py-3 text-right font-mono">{fmt(entry.totalCredit)}</td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_STYLES[entry.status]}`}>
                      {entry.statusName}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-1">
                      {entry.status === JournalEntryStatus.Draft && (
                        <button
                          onClick={() => handlePost(entry.id)}
                          disabled={postEntry.isPending}
                          title="Post entry"
                          className="rounded p-1 hover:bg-emerald/10 text-emerald disabled:opacity-50"
                        >
                          <CheckCircle className="h-4 w-4" />
                        </button>
                      )}
                      {entry.status === JournalEntryStatus.Posted && (
                        <button
                          onClick={() => setReverseTarget(entry.id)}
                          title="Reverse entry"
                          className="rounded p-1 hover:bg-warning/10 text-warning"
                        >
                          <RotateCcw className="h-4 w-4" />
                        </button>
                      )}
                    </div>
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
