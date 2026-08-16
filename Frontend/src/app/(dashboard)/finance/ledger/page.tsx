"use client";

import { useState } from "react";
import { AlertCircle, Search } from "lucide-react";
import { useAccounts, useAccountLedger } from "@/features/finance/hooks";

function fmt(n: number) {
  return n.toLocaleString("en-EG", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export default function LedgerPage() {
  const [accountId, setAccountId] = useState("");
  const [fromDate,  setFromDate]  = useState("");
  const [toDate,    setToDate]    = useState("");
  const [query,     setQuery]     = useState({ accountId: "", fromDate: "", toDate: "" });

  const { data: accounts } = useAccounts({ isGroup: false, isActive: true });
  const { data: ledger, isLoading, isError } = useAccountLedger(
    query.accountId,
    { fromDate: query.fromDate || undefined, toDate: query.toDate || undefined }
  );

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    setQuery({ accountId, fromDate, toDate });
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">General Ledger</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          View the full transaction history and running balance for any account.
        </p>
      </div>

      <form onSubmit={handleSearch} className="rounded-lg border bg-background p-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-4">
          <div className="sm:col-span-2 space-y-1">
            <label className="text-sm font-medium">Account *</label>
            <select
              required value={accountId} onChange={e => setAccountId(e.target.value)}
              className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            >
              <option value="">Select an account…</option>
              {accounts?.map(a => (
                <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
              ))}
            </select>
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">From Date</label>
            <input type="date" value={fromDate} onChange={e => setFromDate(e.target.value)}
              className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium">To Date</label>
            <input type="date" value={toDate} onChange={e => setToDate(e.target.value)}
              className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>
        </div>
        <div className="mt-4 flex justify-end">
          <button type="submit"
            className="flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90">
            <Search className="h-4 w-4" /> Run Ledger
          </button>
        </div>
      </form>

      {isLoading && query.accountId && (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-10 animate-pulse rounded bg-accent/40" />
          ))}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 rounded-lg border border-danger/30 bg-danger/5 p-4 text-sm">
          <AlertCircle className="h-4 w-4 shrink-0 text-danger" /> Failed to load ledger.
        </div>
      )}

      {ledger && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold">
              {ledger.accountCode} — {ledger.accountName}
            </h2>
            <div className="flex gap-6 text-sm text-muted-foreground">
              <span>Opening: <strong className="text-foreground">{fmt(ledger.openingBalance)}</strong></span>
              <span>Closing: <strong className="text-foreground">{fmt(ledger.closingBalance)}</strong></span>
            </div>
          </div>

          <div className="overflow-x-auto rounded-lg border">
            <table className="w-full text-sm">
              <thead className="bg-accent/40 text-left text-xs uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="px-4 py-3">Entry #</th>
                  <th className="px-4 py-3">Date</th>
                  <th className="px-4 py-3">Description</th>
                  <th className="px-4 py-3 text-right">Debit</th>
                  <th className="px-4 py-3 text-right">Credit</th>
                  <th className="px-4 py-3 text-right">Balance</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {/* Opening balance row */}
                <tr className="bg-accent/10 font-medium">
                  <td className="px-4 py-2" colSpan={3}>Opening Balance</td>
                  <td className="px-4 py-2 text-right" />
                  <td className="px-4 py-2 text-right" />
                  <td className="px-4 py-2 text-right font-mono">{fmt(ledger.openingBalance)}</td>
                </tr>
                {ledger.lines.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                      No transactions in this period.
                    </td>
                  </tr>
                )}
                {ledger.lines.map((line, i) => (
                  <tr key={i} className="hover:bg-accent/20">
                    <td className="px-4 py-2 font-mono text-xs">{line.entryNumber}</td>
                    <td className="px-4 py-2 text-muted-foreground">{line.entryDate}</td>
                    <td className="px-4 py-2 max-w-xs">
                      <div className="truncate">{line.description}</div>
                      {line.lineDescription && (
                        <div className="truncate text-xs text-muted-foreground">{line.lineDescription}</div>
                      )}
                    </td>
                    <td className="px-4 py-2 text-right font-mono">
                      {line.debit > 0 ? fmt(line.debit) : "—"}
                    </td>
                    <td className="px-4 py-2 text-right font-mono">
                      {line.credit > 0 ? fmt(line.credit) : "—"}
                    </td>
                    <td className={`px-4 py-2 text-right font-mono font-medium ${line.runningBalance < 0 ? "text-danger" : ""}`}>
                      {fmt(line.runningBalance)}
                    </td>
                  </tr>
                ))}
                {/* Totals row */}
                <tr className="bg-accent/10 font-semibold">
                  <td className="px-4 py-2" colSpan={3}>Totals</td>
                  <td className="px-4 py-2 text-right font-mono">{fmt(ledger.totalDebit)}</td>
                  <td className="px-4 py-2 text-right font-mono">{fmt(ledger.totalCredit)}</td>
                  <td className="px-4 py-2 text-right font-mono">{fmt(ledger.closingBalance)}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
