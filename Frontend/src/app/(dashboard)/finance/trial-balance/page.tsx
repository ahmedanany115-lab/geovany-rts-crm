"use client";

import { useState } from "react";
import { AlertCircle, CheckCircle, Search, XCircle } from "lucide-react";
import { useTrialBalance } from "@/features/finance/hooks";

function fmt(n: number) {
  return n > 0
    ? n.toLocaleString("en-EG", { minimumFractionDigits: 2, maximumFractionDigits: 2 })
    : "—";
}

export default function TrialBalancePage() {
  const [fromDate, setFromDate] = useState("");
  const [toDate,   setToDate]   = useState("");
  const [query, setQuery] = useState<{ fromDate?: string; toDate?: string }>({});

  const { data: tb, isLoading, isError } = useTrialBalance(query);

  function handleRun(e: React.FormEvent) {
    e.preventDefault();
    setQuery({ fromDate: fromDate || undefined, toDate: toDate || undefined });
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Trial Balance</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Verify that total debits equal total credits across all accounts for a given period.
        </p>
      </div>

      <form onSubmit={handleRun} className="rounded-lg border bg-background p-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
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
          <div className="flex items-end">
            <button type="submit"
              className="flex w-full items-center justify-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90">
              <Search className="h-4 w-4" /> Generate
            </button>
          </div>
        </div>
      </form>

      {isLoading && (
        <div className="space-y-2">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-10 animate-pulse rounded bg-accent/40" />
          ))}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 rounded-lg border border-danger/30 bg-danger/5 p-4 text-sm">
          <AlertCircle className="h-4 w-4 shrink-0 text-danger" /> Failed to generate trial balance.
        </div>
      )}

      {tb && (
        <div className="space-y-4">
          {/* Balance indicator */}
          <div className={`flex items-center gap-3 rounded-lg border p-3 text-sm font-medium ${
            tb.isBalanced
              ? "border-emerald/30 bg-emerald/5 text-emerald"
              : "border-danger/30 bg-danger/5 text-danger"
          }`}>
            {tb.isBalanced
              ? <><CheckCircle className="h-4 w-4" /> Trial balance is balanced.</>
              : <><XCircle className="h-4 w-4" /> Trial balance is NOT balanced — review posted entries.</>
            }
          </div>

          <div className="overflow-x-auto rounded-lg border">
            <table className="w-full text-sm">
              <thead className="bg-accent/40 text-left text-xs uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="px-4 py-3">Code</th>
                  <th className="px-4 py-3">Account Name</th>
                  <th className="px-4 py-3">Type</th>
                  <th className="px-4 py-3 text-right">Opening Dr</th>
                  <th className="px-4 py-3 text-right">Opening Cr</th>
                  <th className="px-4 py-3 text-right">Period Dr</th>
                  <th className="px-4 py-3 text-right">Period Cr</th>
                  <th className="px-4 py-3 text-right">Closing Dr</th>
                  <th className="px-4 py-3 text-right">Closing Cr</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {tb.lines.length === 0 && (
                  <tr>
                    <td colSpan={9} className="px-4 py-10 text-center text-muted-foreground">
                      No posted transactions found for the selected period.
                    </td>
                  </tr>
                )}
                {tb.lines.map(line => (
                  <tr key={line.accountId} className="hover:bg-accent/20">
                    <td className="px-4 py-2 font-mono">{line.accountCode}</td>
                    <td className="px-4 py-2">{line.accountName}</td>
                    <td className="px-4 py-2 text-xs text-muted-foreground">{line.accountTypeName}</td>
                    <td className="px-4 py-2 text-right font-mono text-xs">{fmt(line.openingDebit)}</td>
                    <td className="px-4 py-2 text-right font-mono text-xs">{fmt(line.openingCredit)}</td>
                    <td className="px-4 py-2 text-right font-mono text-xs">{fmt(line.periodDebit)}</td>
                    <td className="px-4 py-2 text-right font-mono text-xs">{fmt(line.periodCredit)}</td>
                    <td className="px-4 py-2 text-right font-mono font-medium">{fmt(line.closingDebit)}</td>
                    <td className="px-4 py-2 text-right font-mono font-medium">{fmt(line.closingCredit)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="bg-accent/40 font-semibold">
                <tr>
                  <td className="px-4 py-3" colSpan={3}>Totals</td>
                  <td className="px-4 py-3 text-right font-mono">{fmt(tb.totalOpeningDebit)}</td>
                  <td className="px-4 py-3 text-right font-mono">{fmt(tb.totalOpeningCredit)}</td>
                  <td className="px-4 py-3 text-right font-mono">{fmt(tb.totalPeriodDebit)}</td>
                  <td className="px-4 py-3 text-right font-mono">{fmt(tb.totalPeriodCredit)}</td>
                  <td className="px-4 py-3 text-right font-mono">{fmt(tb.totalClosingDebit)}</td>
                  <td className="px-4 py-3 text-right font-mono">{fmt(tb.totalClosingCredit)}</td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
