"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { TrendingUp, TrendingDown, RefreshCw, BarChart3 } from "lucide-react";

interface PLLine { accountCode: string; accountName: string; accountNameAr?: string; amount: number; }
interface PLData {
  fromDate: string; toDate: string;
  revenueLines: PLLine[]; costOfSalesLines: PLLine[]; expenseLines: PLLine[];
  totalRevenue: number; totalCOGS: number; grossProfit: number;
  totalExpenses: number; operatingProfit: number; netProfit: number;
  grossMarginPct: number; netMarginPct: number;
}

const curr = (v: number) =>
  new Intl.NumberFormat("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 }).format(Math.abs(v));

export default function ProfitLossPage() {
  const { t, lang } = useT();
  const year = new Date().getFullYear();
  const [from, setFrom] = useState(`${year}-01-01`);
  const [to,   setTo]   = useState(new Date().toISOString().split("T")[0]);

  const { data, isLoading, refetch, isFetching } = useQuery<PLData>({
    queryKey: ["profit-loss", from, to],
    queryFn:  () => apiFetch(`/reports/profit-loss?fromDate=${from}&toDate=${to}`),
    enabled:  !!from && !!to,
  });

  const pct = (v: number) => (data?.totalRevenue && data.totalRevenue !== 0
    ? `${((v / data.totalRevenue) * 100).toFixed(1)}%` : "—");

  const Row = ({ label, value, bold, colorPos, indent }: {
    label: string; value: number; bold?: boolean; colorPos?: boolean; indent?: boolean;
  }) => (
    <div className={`flex justify-between py-1.5 text-sm border-b border-border/20 ${bold ? "font-bold" : ""} ${indent ? "pl-4" : ""}`}>
      <span className={colorPos ? (value >= 0 ? "text-emerald-700" : "text-red-700") : ""}>{label}</span>
      <div className="flex items-center gap-4">
        <span className={`tabular-nums w-28 text-right ${colorPos ? (value >= 0 ? "text-emerald-700" : "text-red-700") : ""}`}>
          {value < 0 ? `(${curr(-value)})` : curr(value)}
        </span>
        <span className="text-xs text-muted-foreground w-12 text-right">{pct(value)}</span>
      </div>
    </div>
  );

  return (
    <div className="p-6 space-y-6 max-w-2xl">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <BarChart3 className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">
              {lang === "ar" ? "قائمة الأرباح والخسائر" : "Profit & Loss"}
            </h1>
            <p className="text-sm text-muted-foreground">
              From posted journal entries · {lang === "ar" ? "من القيود المحاسبية المرحّلة" : ""}
            </p>
          </div>
        </div>
        <button onClick={() => refetch()} disabled={isFetching}
          className="btn-ghost p-2 rounded-lg disabled:opacity-50">
          <RefreshCw className={`h-4 w-4 ${isFetching ? "animate-spin" : ""}`} />
        </button>
      </div>

      {/* Date filters */}
      <div className="flex gap-3 items-end">
        <div>
          <label className="text-xs text-muted-foreground block mb-1">From</label>
          <input type="date" value={from} onChange={e => setFrom(e.target.value)} className="input" />
        </div>
        <div>
          <label className="text-xs text-muted-foreground block mb-1">To</label>
          <input type="date" value={to} min={from} onChange={e => setTo(e.target.value)} className="input" />
        </div>
        <button onClick={() => { setFrom(`${year}-01-01`); setTo(new Date().toISOString().split("T")[0]); }}
          className="btn-ghost text-xs px-3 py-2 rounded-lg">YTD</button>
        <button onClick={() => { setFrom(`${year}-01-01`); setTo(`${year}-12-31`); }}
          className="btn-ghost text-xs px-3 py-2 rounded-lg">Full Year</button>
      </div>

      {isLoading && (
        <div className="card p-12 text-center text-muted-foreground">
          <div className="animate-pulse">Calculating P&L from accounting data…</div>
        </div>
      )}

      {data && (
        <>
          {/* KPI cards */}
          <div className="grid grid-cols-3 gap-4">
            {[
              { label: "Revenue",     value: data.totalRevenue,  icon: TrendingUp,   color: "emerald" },
              { label: "Gross Profit",value: data.grossProfit,   icon: TrendingUp,   color: data.grossProfit >= 0 ? "emerald" : "red" },
              { label: "Net Profit",  value: data.netProfit,     icon: data.netProfit >= 0 ? TrendingUp : TrendingDown, color: data.netProfit >= 0 ? "emerald" : "red" },
            ].map(k => (
              <div key={k.label} className={`card p-4 border-l-4 ${k.color === "emerald" ? "border-l-emerald-500" : "border-l-red-500"}`}>
                <p className="text-xs text-muted-foreground">{k.label}</p>
                <p className={`text-xl font-bold tabular-nums ${k.color === "emerald" ? "text-emerald-700" : "text-red-700"}`}>
                  {k.value < 0 ? `(${curr(-k.value)})` : curr(k.value)}
                </p>
                {k.label !== "Revenue" && (
                  <p className="text-xs text-muted-foreground mt-0.5">{pct(k.value)} of revenue</p>
                )}
              </div>
            ))}
          </div>

          {/* P&L Statement */}
          <div className="card p-6 space-y-1">
            <div className="text-center pb-4 border-b mb-3">
              <p className="font-bold text-lg">Profit & Loss Statement</p>
              <p className="text-sm text-muted-foreground">{data.fromDate} — {data.toDate}</p>
            </div>

            {/* Revenue */}
            <p className="text-xs font-bold uppercase tracking-widest text-muted-foreground pt-1">Revenue</p>
            {data.revenueLines.map(l => (
              <Row key={l.accountCode} indent label={`${l.accountCode} — ${lang === "ar" && l.accountNameAr ? l.accountNameAr : l.accountName}`} value={l.amount} />
            ))}
            {data.revenueLines.length === 0 && <p className="text-xs text-muted-foreground pl-4 py-1">No revenue entries in this period.</p>}
            <Row label="Total Revenue" value={data.totalRevenue} bold />

            {/* COGS */}
            <p className="text-xs font-bold uppercase tracking-widest text-muted-foreground pt-3">Cost of Sales</p>
            {data.costOfSalesLines.map(l => (
              <Row key={l.accountCode} indent label={`${l.accountCode} — ${lang === "ar" && l.accountNameAr ? l.accountNameAr : l.accountName}`} value={l.amount} />
            ))}
            {data.costOfSalesLines.length === 0 && <p className="text-xs text-muted-foreground pl-4 py-1">No cost of sales entries.</p>}
            <Row label="Total Cost of Sales" value={data.totalCOGS} bold />

            {/* Gross Profit */}
            <div className="border-t-2 border-border mt-1 mb-1" />
            <Row label="Gross Profit" value={data.grossProfit} bold colorPos />
            <div className="text-right text-xs text-muted-foreground pb-2">Gross Margin: {data.grossMarginPct}%</div>

            {/* Expenses */}
            <p className="text-xs font-bold uppercase tracking-widest text-muted-foreground pt-2">Operating Expenses</p>
            {data.expenseLines.map(l => (
              <Row key={l.accountCode} indent label={`${l.accountCode} — ${lang === "ar" && l.accountNameAr ? l.accountNameAr : l.accountName}`} value={l.amount} />
            ))}
            {data.expenseLines.length === 0 && <p className="text-xs text-muted-foreground pl-4 py-1">No expense entries.</p>}
            <Row label="Total Expenses" value={data.totalExpenses} bold />

            {/* Net Profit */}
            <div className="border-t-2 border-border mt-1 mb-1" />
            <div className={`flex justify-between py-2 text-base font-bold border-t-4 pt-3 ${data.netProfit >= 0 ? "border-t-emerald-500 text-emerald-700" : "border-t-red-500 text-red-700"}`}>
              <span>Net Profit {data.netProfit < 0 ? "(Loss)" : ""}</span>
              <div className="flex items-center gap-4">
                <span className="tabular-nums w-28 text-right">
                  {data.netProfit < 0 ? `(${curr(-data.netProfit)})` : curr(data.netProfit)}
                </span>
                <span className="text-sm w-12 text-right">{data.netMarginPct}%</span>
              </div>
            </div>
          </div>

          <p className="text-xs text-muted-foreground text-center">
            Calculated from posted journal entries only. Draft entries are excluded.
          </p>
        </>
      )}

      {!isLoading && !data && (
        <div className="card p-8 text-center text-muted-foreground">
          Select a date range to generate the P&L report.
        </div>
      )}
    </div>
  );
}
