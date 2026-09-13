"use client";
import { useState } from "react";
import { useAccountLedger, useAccounts } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { Receipt, RefreshCw, User } from "lucide-react";

export default function LedgerPage() {
  const { t } = useT();
  const [accountId, setAccountId] = useState("");
  const [fromDate, setFromDate]   = useState("");
  const [toDate, setToDate]       = useState("");

  const { data: accounts } = useAccounts();
  const { data: ledger, isLoading, refetch } = useAccountLedger(
    accountId,
    accountId ? { fromDate: fromDate || undefined, toDate: toDate || undefined } : undefined
  );

  const fmt = (v: number) => v.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 2 });

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Receipt className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("ledger")}</h1></div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>

      {/* Filters */}
      <div className="flex gap-3 flex-wrap">
        <div className="flex-1 min-w-[220px]">
          <label className="text-xs text-muted-foreground block mb-1">Account *</label>
          <select value={accountId} onChange={e => setAccountId(e.target.value)} className="input w-full">
            <option value="">Select account…</option>
            {accounts?.filter(a => !a.isGroup).map(a => (
              <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="text-xs text-muted-foreground block mb-1">From</label>
          <input type="date" value={fromDate} onChange={e => setFromDate(e.target.value)} className="input" />
        </div>
        <div>
          <label className="text-xs text-muted-foreground block mb-1">To</label>
          <input type="date" value={toDate} min={fromDate} onChange={e => setToDate(e.target.value)} className="input" />
        </div>
      </div>

      {!accountId && (
        <div className="card p-8 text-center text-muted-foreground">Select an account to view its ledger.</div>
      )}

      {accountId && isLoading && (
        <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>
      )}

      {ledger && (
        <>
          {/* Summary */}
          <div className="grid grid-cols-4 gap-4">
            {[
              { label: "Opening Balance", value: ledger.openingBalance, cls: "" },
              { label: `Total ${t("debit")}`,  value: ledger.totalDebit,    cls: "text-red-600" },
              { label: `Total ${t("credit")}`, value: ledger.totalCredit,   cls: "text-emerald-600" },
              { label: "Closing Balance", value: ledger.closingBalance, cls: ledger.closingBalance >= 0 ? "text-emerald-600" : "text-red-600" },
            ].map(k => (
              <div key={k.label} className="card p-3 text-center">
                <p className="text-xs text-muted-foreground">{k.label}</p>
                <p className={`font-bold tabular-nums ${k.cls}`}>{fmt(k.value)}</p>
              </div>
            ))}
          </div>

          {/* Lines */}
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30"><tr>
                <th className="text-left p-3 font-medium text-muted-foreground">Entry #</th>
                <th className="text-left p-3 font-medium text-muted-foreground">{t("date")}</th>
                <th className="text-left p-3 font-medium text-muted-foreground">{t("description")}</th>
                <th className="text-left p-3 font-medium text-muted-foreground">
                  <div className="flex items-center gap-1"><User className="h-3.5 w-3.5" /> Customer / Partner</div>
                </th>
                <th className="text-left p-3 font-medium text-muted-foreground">Ref #</th>
                <th className="text-right p-3 font-medium text-muted-foreground">{t("debit")}</th>
                <th className="text-right p-3 font-medium text-muted-foreground">{t("credit")}</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Running {t("balance")}</th>
              </tr></thead>
              <tbody className="divide-y divide-border">
                {ledger.lines.map((line: any) => (
                  <tr key={`${line.journalEntryId}-${line.entryNumber}`} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs text-muted-foreground">{line.entryNumber}</td>
                    <td className="p-3 text-muted-foreground">{line.entryDate}</td>
                    <td className="p-3 max-w-[180px] truncate" title={line.description}>{line.description}</td>
                    <td className="p-3">
                      {line.partnerName
                        ? <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full bg-blue-50 text-blue-700 font-medium">
                            <User className="h-3 w-3" />{line.partnerName}
                          </span>
                        : <span className="text-muted-foreground">—</span>}
                    </td>
                    <td className="p-3 font-mono text-xs text-muted-foreground">{line.referenceNumber ?? "—"}</td>
                    <td className="p-3 text-right tabular-nums">{line.debit > 0 ? fmt(line.debit) : "—"}</td>
                    <td className="p-3 text-right tabular-nums">{line.credit > 0 ? fmt(line.credit) : "—"}</td>
                    <td className={`p-3 text-right tabular-nums font-semibold ${line.runningBalance >= 0 ? "text-emerald-600" : "text-red-600"}`}>
                      {fmt(line.runningBalance)}
                    </td>
                  </tr>
                ))}
                {ledger.lines.length === 0 && (
                  <tr><td colSpan={8} className="p-8 text-center text-muted-foreground">No entries in this period.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}
