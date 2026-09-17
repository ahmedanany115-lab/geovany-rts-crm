"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { BarChart3, RefreshCw, AlertTriangle, CheckCircle } from "lucide-react";

interface BSLine  { accountId?: string; accountCode: string; accountName: string; accountNameAr?: string; balance: number; }
interface BSData  {
  asOfDate: string;
  assetLines: BSLine[]; liabilityLines: BSLine[]; equityLines: BSLine[];
  totalAssets: number; totalLiabilities: number; totalEquity: number;
  totalLiabAndEquity: number; imbalanceAmount: number; isBalanced: boolean;
}

const EGP = (v: number) =>
  new Intl.NumberFormat("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 }).format(Math.abs(v));

export default function BalanceSheetPage() {
  const { t, lang } = useT();
  const [asOf, setAsOf] = useState(new Date().toISOString().split("T")[0]);

  const { data, isLoading, refetch, isFetching } = useQuery<BSData>({
    queryKey: ["balance-sheet", asOf],
    queryFn: () => apiFetch(`/reports/balance-sheet?asOfDate=${asOf}`),
    enabled: !!asOf,
  });

  const Section = ({ title, lines, total, colorClass }: { title: string; lines: BSLine[]; total: number; colorClass: string }) => (
    <div>
      <h3 className={`text-xs font-bold uppercase tracking-widest py-2 px-1 ${colorClass}`}>{title}</h3>
      {lines.map(l => (
        <div key={l.accountCode} className="flex justify-between py-1 text-sm border-b border-border/20 pl-4">
          <span className="text-muted-foreground">{l.accountCode} — {lang === "ar" && l.accountNameAr ? l.accountNameAr : l.accountName}</span>
          <span className="tabular-nums font-medium">{EGP(l.balance)}</span>
        </div>
      ))}
      {lines.length === 0 && <p className="text-xs text-muted-foreground pl-4 py-1 italic">No entries.</p>}
      <div className={`flex justify-between py-2 text-sm font-bold border-t-2 mt-1 ${colorClass.replace("text-", "border-")}`}>
        <span>Total {title}</span>
        <span className="tabular-nums">{EGP(total)}</span>
      </div>
    </div>
  );

  return (
    <div className="p-6 space-y-6 max-w-2xl">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <BarChart3 className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">{lang === "ar" ? "الميزانية العمومية" : "Balance Sheet"}</h1>
            <p className="text-sm text-muted-foreground">Assets = Liabilities + Equity</p>
          </div>
        </div>
        <button onClick={() => refetch()} disabled={isFetching} className="btn-ghost p-2 rounded-lg disabled:opacity-50">
          <RefreshCw className={`h-4 w-4 ${isFetching ? "animate-spin" : ""}`} />
        </button>
      </div>

      <div className="flex gap-3 items-end">
        <div>
          <label className="text-xs text-muted-foreground block mb-1">As of Date</label>
          <input type="date" value={asOf} onChange={e => setAsOf(e.target.value)} className="input" />
        </div>
        <button onClick={() => setAsOf(new Date().toISOString().split("T")[0])} className="btn-ghost text-xs px-3 py-2 rounded-lg">Today</button>
        <button onClick={() => { const d = new Date(); d.setFullYear(d.getFullYear()-1); d.setMonth(11); d.setDate(31); setAsOf(d.toISOString().split("T")[0]); }} className="btn-ghost text-xs px-3 py-2 rounded-lg">Year End</button>
      </div>

      {isLoading && <div className="card p-12 text-center text-muted-foreground animate-pulse">Building balance sheet from accounting data…</div>}

      {data && (
        <>
          {/* Balance check banner */}
          <div className={`flex items-center gap-3 px-4 py-3 rounded-xl ${data.isBalanced ? "bg-emerald-50 border border-emerald-200 text-emerald-700" : "bg-red-50 border border-red-200 text-red-700"}`}>
            {data.isBalanced
              ? <><CheckCircle className="h-5 w-5 shrink-0" /> Balance sheet is balanced — Assets = Liabilities + Equity</>
              : <><AlertTriangle className="h-5 w-5 shrink-0" /> <strong>Imbalance detected!</strong> Difference: {EGP(data.imbalanceAmount)}. Check your Chart of Accounts.</>}
          </div>

          {/* KPI cards */}
          <div className="grid grid-cols-3 gap-4">
            {[
              { label: "Total Assets",      value: data.totalAssets,      color: "text-blue-700",    bg: "bg-blue-50 border-l-blue-500" },
              { label: "Total Liabilities", value: data.totalLiabilities, color: "text-red-700",     bg: "bg-red-50 border-l-red-500" },
              { label: "Total Equity",      value: data.totalEquity,      color: "text-emerald-700", bg: "bg-emerald-50 border-l-emerald-500" },
            ].map(k => (
              <div key={k.label} className={`card p-4 border-l-4 ${k.bg}`}>
                <p className="text-xs text-muted-foreground">{k.label}</p>
                <p className={`text-xl font-bold tabular-nums ${k.color}`}>{EGP(k.value)}</p>
              </div>
            ))}
          </div>

          {/* Balance Sheet statement */}
          <div className="card p-6 space-y-4">
            <div className="text-center pb-4 border-b">
              <p className="font-bold text-lg">Balance Sheet</p>
              <p className="text-sm text-muted-foreground">As of {data.asOfDate}</p>
              <p className="text-xs text-muted-foreground">From posted journal entries only</p>
            </div>

            <Section title="Assets" lines={data.assetLines} total={data.totalAssets} colorClass="text-blue-700" />
            <div className="pt-2" />
            <Section title="Liabilities" lines={data.liabilityLines} total={data.totalLiabilities} colorClass="text-red-700" />
            <div className="pt-2" />
            <Section title="Equity" lines={data.equityLines} total={data.totalEquity} colorClass="text-emerald-700" />

            <div className="border-t-4 border-border pt-3">
              <div className="flex justify-between text-base font-bold">
                <span>Total Liabilities + Equity</span>
                <span className="tabular-nums">{EGP(data.totalLiabAndEquity)}</span>
              </div>
              <div className={`flex justify-between text-base font-bold mt-1 ${data.isBalanced ? "text-emerald-700" : "text-red-700"}`}>
                <span>Total Assets</span>
                <span className="tabular-nums">{EGP(data.totalAssets)}</span>
              </div>
              {!data.isBalanced && (
                <div className="flex justify-between text-sm font-semibold mt-1 text-red-600">
                  <span>Imbalance</span>
                  <span className="tabular-nums">{EGP(data.imbalanceAmount)}</span>
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
