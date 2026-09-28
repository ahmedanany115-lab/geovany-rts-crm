"use client";

import { useState } from "react";
import { useCashJournal, useAccounts } from "@/features/finance/hooks";
import { usePrint } from "@/hooks/usePrint";
import { useToast } from "@/components/ui/toast";
import type { AccountDto } from "@/features/finance/types";
import type { CashJournalDto } from "@/features/finance/api/financeApi";
import { BookOpen, Search, Printer, X } from "lucide-react";

function fmt(n: number) {
  return n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function fmtDate(s: string) {
  return new Date(s).toLocaleDateString("en-GB", { day: "2-digit", month: "short" });
}

/* ── Print content ─────────────────────────────────────────────────────────── */
function JournalPrintView({ journal }: { journal: CashJournalDto }) {
  return (
    <div className="p-8 font-mono text-xs space-y-4 text-black">
      <div className="text-center text-lg font-bold tracking-widest border-b-2 border-black pb-2">
        CASH JOURNAL
      </div>
      <div className="flex justify-between text-xs mt-1">
        <span>Cash Account: <strong>{journal.cashAccountName}</strong></span>
        <span>Period: {new Date(journal.fromDate).toLocaleDateString("en-GB")} — {new Date(journal.toDate).toLocaleDateString("en-GB")}</span>
      </div>

      <div className="flex justify-end border-t border-dashed border-black pt-1 mt-2">
        <span>Opening Balance: <strong>{fmt(journal.openingBalance)}</strong></span>
      </div>

      <table className="w-full border-collapse mt-2">
        <thead>
          <tr className="border-b border-black">
            <th className="text-left py-1 pr-2">Date</th>
            <th className="text-left py-1 pr-2">Document #</th>
            <th className="text-left py-1 pr-2">Description</th>
            <th className="text-left py-1 pr-2">Contra Account</th>
            <th className="text-right py-1 pr-2">Debit</th>
            <th className="text-right py-1 pr-2">Credit</th>
            <th className="text-right py-1">Balance</th>
          </tr>
        </thead>
        <tbody>
          {journal.entries.map((e, i) => (
            <tr key={i} className="border-b border-dashed border-gray-300">
              <td className="py-0.5 pr-2">{fmtDate(e.date)}</td>
              <td className="py-0.5 pr-2">{e.documentNumber}</td>
              <td className="py-0.5 pr-2">{e.description}</td>
              <td className="py-0.5 pr-2">{e.contraAccount}</td>
              <td className="py-0.5 pr-2 text-right">{e.debit > 0 ? fmt(e.debit) : "—"}</td>
              <td className="py-0.5 pr-2 text-right">{e.credit > 0 ? fmt(e.credit) : "—"}</td>
              <td className="py-0.5 text-right">{fmt(e.runningBalance)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <div className="border-t-2 border-black pt-2 space-y-0.5 text-right">
        <div>Total Debit: <strong>{fmt(journal.totalDebit)}</strong></div>
        <div>Total Credit: <strong>{fmt(journal.totalCredit)}</strong></div>
        <div className="text-base font-bold">Closing Balance: {fmt(journal.closingBalance)}</div>
      </div>
    </div>
  );
}

/* ── Print modal ───────────────────────────────────────────────────────────── */
function PrintModal({ journal, onClose }: { journal: CashJournalDto; onClose: () => void }) {
  const { printRef, handlePrint } = usePrint(`Cash Journal — ${journal.cashAccountName}`);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="bg-background rounded-xl border shadow-xl w-full max-w-4xl">
        <div className="flex items-center justify-between p-4 border-b">
          <span className="font-semibold text-sm">Print Preview — Cash Journal</span>
          <div className="flex gap-2">
            <button onClick={handlePrint}
              className="flex items-center gap-2 btn-primary px-4 py-1.5 rounded-lg text-sm">
              <Printer className="h-4 w-4" /> Print
            </button>
            <button onClick={onClose} className="p-1 rounded hover:bg-accent text-muted-foreground">
              <X className="h-4 w-4" />
            </button>
          </div>
        </div>
        <div ref={printRef} className="overflow-y-auto max-h-[75vh]">
          <JournalPrintView journal={journal} />
        </div>
      </div>
    </div>
  );
}

/* ── Main page ─────────────────────────────────────────────────────────────── */
export default function CashJournalPage() {
  const today = new Date().toISOString().split("T")[0];
  const firstOfMonth = new Date(new Date().getFullYear(), new Date().getMonth(), 1)
    .toISOString().split("T")[0];

  const [cashAccountId, setCashAccountId] = useState("");
  const [fromDate, setFromDate] = useState(firstOfMonth);
  const [toDate, setToDate]     = useState(today);
  const [searchParams, setSearchParams] = useState<
    { cashAccountId: string; fromDate: string; toDate: string } | undefined
  >(undefined);
  const [showPrint, setShowPrint] = useState(false);

  const { data: accounts } = useAccounts();
  const postingAccounts = (accounts ?? []).filter((a: AccountDto) => !a.isGroup && a.isActive);

  const { data: journal, isLoading, error } = useCashJournal(searchParams);

  const handleSearch = () => {
    if (!cashAccountId) return;
    if (!fromDate || !toDate) return;
    setSearchParams({ cashAccountId, fromDate, toDate });
  };

  return (
    <div className="p-6 space-y-6">
      {/* Print modal */}
      {showPrint && journal && (
        <PrintModal journal={journal} onClose={() => setShowPrint(false)} />
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <BookOpen className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">Cash Journal</h1>
        </div>
        {journal && (
          <button onClick={() => setShowPrint(true)}
            className="btn-ghost flex items-center gap-2 px-4 py-2 rounded-lg text-sm border">
            <Printer className="h-4 w-4" /> Print
          </button>
        )}
      </div>

      {/* Filter bar */}
      <div className="card p-4">
        <div className="flex flex-wrap gap-3 items-end">
          <div className="flex-1 min-w-[200px]">
            <label className="text-xs text-muted-foreground block mb-1">Cash Account *</label>
            <select value={cashAccountId} onChange={e => setCashAccountId(e.target.value)} className="input w-full">
              <option value="">Select account…</option>
              {postingAccounts.map((a: AccountDto) => (
                <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-xs text-muted-foreground block mb-1">From Date</label>
            <input type="date" value={fromDate} onChange={e => setFromDate(e.target.value)} className="input" />
          </div>
          <div>
            <label className="text-xs text-muted-foreground block mb-1">To Date</label>
            <input type="date" value={toDate} onChange={e => setToDate(e.target.value)} className="input" />
          </div>
          <button
            onClick={handleSearch}
            disabled={!cashAccountId || !fromDate || !toDate}
            className="btn-primary flex items-center gap-2 px-5 py-2 rounded-lg text-sm disabled:opacity-50">
            <Search className="h-4 w-4" /> Search
          </button>
        </div>
      </div>

      {/* Results */}
      {isLoading && (
        <div className="text-center py-10 text-muted-foreground">Loading journal…</div>
      )}

      {error && (
        <div className="card p-4 text-red-600 text-sm">
          Failed to load cash journal. Please try again.
        </div>
      )}

      {!isLoading && !error && !journal && (
        <div className="card p-12 text-center text-muted-foreground">
          Select a cash account and date range, then click Search.
        </div>
      )}

      {journal && (
        <div className="space-y-4">
          {/* Period summary */}
          <div className="card p-4 flex flex-wrap gap-6 text-sm">
            <div>
              <span className="text-xs text-muted-foreground block">Cash Account</span>
              <span className="font-semibold">{journal.cashAccountName}</span>
            </div>
            <div>
              <span className="text-xs text-muted-foreground block">Period</span>
              <span className="font-medium">
                {new Date(journal.fromDate).toLocaleDateString("en-GB")} — {new Date(journal.toDate).toLocaleDateString("en-GB")}
              </span>
            </div>
            <div className="ml-auto text-right">
              <span className="text-xs text-muted-foreground block">Opening Balance</span>
              <span className="text-lg font-bold tabular-nums">{fmt(journal.openingBalance)}</span>
            </div>
          </div>

          {/* Entries table */}
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="border-b bg-muted/30">
                <tr>
                  <th className="text-left px-4 py-3 font-medium text-muted-foreground">Date</th>
                  <th className="text-left px-4 py-3 font-medium text-muted-foreground">Document #</th>
                  <th className="text-left px-4 py-3 font-medium text-muted-foreground">Description</th>
                  <th className="text-left px-4 py-3 font-medium text-muted-foreground">Contra Account</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Debit</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Credit</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Balance</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {journal.entries.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-4 py-10 text-center text-muted-foreground">
                      No transactions in this period.
                    </td>
                  </tr>
                ) : journal.entries.map((entry, i) => (
                  <tr key={i} className="hover:bg-muted/20">
                    <td className="px-4 py-2.5 text-muted-foreground whitespace-nowrap">
                      {fmtDate(entry.date)}
                    </td>
                    <td className="px-4 py-2.5 font-mono text-xs text-primary">{entry.documentNumber}</td>
                    <td className="px-4 py-2.5">{entry.description}</td>
                    <td className="px-4 py-2.5 text-muted-foreground">{entry.contraAccount}</td>
                    <td className="px-4 py-2.5 text-right tabular-nums text-emerald-700">
                      {entry.debit > 0 ? fmt(entry.debit) : "—"}
                    </td>
                    <td className="px-4 py-2.5 text-right tabular-nums text-red-600">
                      {entry.credit > 0 ? fmt(entry.credit) : "—"}
                    </td>
                    <td className="px-4 py-2.5 text-right tabular-nums font-medium">
                      {fmt(entry.runningBalance)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Totals footer */}
          <div className="card p-4">
            <div className="grid grid-cols-3 gap-4 text-sm">
              <div className="text-center">
                <span className="text-xs text-muted-foreground block mb-0.5">Total Debit</span>
                <span className="text-lg font-semibold tabular-nums text-emerald-700">{fmt(journal.totalDebit)}</span>
              </div>
              <div className="text-center">
                <span className="text-xs text-muted-foreground block mb-0.5">Total Credit</span>
                <span className="text-lg font-semibold tabular-nums text-red-600">{fmt(journal.totalCredit)}</span>
              </div>
              <div className="text-center border-l pl-4">
                <span className="text-xs text-muted-foreground block mb-0.5">Closing Balance</span>
                <span className="text-xl font-bold tabular-nums">{fmt(journal.closingBalance)}</span>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
