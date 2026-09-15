"use client";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { BarChart3, RefreshCw, TrendingUp, TrendingDown } from "lucide-react";

interface IncomeStatementLine {
  accountId: string;
  accountCode: string;
  accountName: string;
  accountNameAr?: string;
  amount: number;
}

interface IncomeStatementDto {
  fromDate: string;
  toDate: string;
  revenueLines: IncomeStatementLine[];
  costOfSalesLines: IncomeStatementLine[];
  expenseLines: IncomeStatementLine[];
  totalRevenue: number;
  totalCOGS: number;
  grossProfit: number;
  totalExpenses: number;
  operatingProfit: number;
  netProfit: number;
  grossMarginPct: number;
  netMarginPct: number;
}

const fmt = (v: number, sign = false) => {
  const abs = Math.abs(v).toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 });
  if (sign && v < 0) return `(${abs})`;
  return abs;
};

export default function IncomeStatementPage() {
  const { t, lang } = useT();
  const year = new Date().getFullYear();
  const [fromDate, setFromDate] = useState(`${year}-01-01`);
  const [toDate,   setToDate]   = useState(new Date().toISOString().split("T")[0]);

  const { data, isLoading, refetch } = useQuery({
    queryKey: ["income-statement", fromDate, toDate],
    queryFn: () => apiFetch<IncomeStatementDto>(`/reports/income-statement?fromDate=${fromDate}&toDate=${toDate}`),
    enabled: !!fromDate && !!toDate,
  });

  const Section = ({ title, lines, totalLabel, total, colorClass }: {
    title: string; lines: IncomeStatementLine[];
    totalLabel: string; total: number; colorClass: string;
  }) => (
    <div className="space-y-1">
      <h2 className="font-bold text-sm uppercase tracking-wide text-muted-foreground pt-2">{title}</h2>
      {lines.map(l => (
        <div key={l.accountId} className="flex items-center justify-between py-1 border-b border-border/30 text-sm">
          <span className="text-muted-foreground">
            <span className="font-mono text-xs mr-2">{l.accountCode}</span>
            {lang === "ar" && l.accountNameAr ? l.accountNameAr : l.accountName}
          </span>
          <span className="tabular-nums font-medium">{fmt(l.amount)}</span>
        </div>
      ))}
      {lines.length === 0 && <p className="text-xs text-muted-foreground py-1">— No entries —</p>}
      <div className={`flex items-center justify-between py-2 font-bold text-sm ${colorClass}`}>
        <span>{totalLabel}</span>
        <span className="tabular-nums">{fmt(total)}</span>
      </div>
    </div>
  );

  return (
    <div className="p-6 space-y-6 max-w-3xl">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <BarChart3 className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">
            {lang === "ar" ? "قائمة الدخل" : "Income Statement"}
          </h1>
        </div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>

      <div className="flex gap-3">
        <div><label className="text-xs text-muted-foreground block mb-1">From</label>
          <input type="date" value={fromDate} onChange={e => setFromDate(e.target.value)} className="input" /></div>
        <div><label className="text-xs text-muted-foreground block mb-1">To</label>
          <input type="date" value={toDate} min={fromDate} onChange={e => setToDate(e.target.value)} className="input" /></div>
      </div>

      {isLoading && <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>}

      {data && (
        <div className="card p-6 space-y-4 print:shadow-none">
          <div className="text-center border-b pb-4">
            <h2 className="text-xl font-bold">Income Statement</h2>
            <p className="text-sm text-muted-foreground">{data.fromDate} — {data.toDate}</p>
          </div>

          {/* KPI strip */}
          <div className="grid grid-cols-3 gap-4">
            {[
              { label: "Gross Profit", value: data.grossProfit, pct: data.grossMarginPct, icon: TrendingUp },
              { label: "Total Expenses", value: data.totalExpenses, pct: null, icon: TrendingDown },
              { label: "Net Profit", value: data.netProfit, pct: data.netMarginPct, icon: data.netProfit >= 0 ? TrendingUp : TrendingDown },
            ].map(k => (
              <div key={k.label} className={`rounded-lg p-3 text-center ${k.value >= 0 ? "bg-emerald-50" : "bg-red-50"}`}>
                <p className="text-xs text-muted-foreground">{k.label}</p>
                <p className={`font-bold tabular-nums ${k.value >= 0 ? "text-emerald-700" : "text-red-700"}`}>{fmt(k.value, true)}</p>
                {k.pct !== null && <p className="text-xs text-muted-foreground">{k.pct}%</p>}
              </div>
            ))}
          </div>

          <Section
            title={lang === "ar" ? "الإيرادات" : "Revenue"}
            lines={data.revenueLines}
            totalLabel="Total Revenue"
            total={data.totalRevenue}
            colorClass="text-emerald-700 border-t border-emerald-200"
          />

          <Section
            title={lang === "ar" ? "تكلفة المبيعات" : "Cost of Sales"}
            lines={data.costOfSalesLines}
            totalLabel="Total COGS"
            total={data.totalCOGS}
            colorClass="text-orange-700 border-t border-orange-200"
          />

          {/* Gross Profit line */}
          <div className={`flex justify-between py-2 font-bold border-t-2 ${data.grossProfit >= 0 ? "text-emerald-700 border-emerald-400" : "text-red-700 border-red-400"}`}>
            <span>Gross Profit</span>
            <span className="tabular-nums">{fmt(data.grossProfit, true)}</span>
          </div>

          <Section
            title={lang === "ar" ? "المصروفات" : "Operating Expenses"}
            lines={data.expenseLines}
            totalLabel="Total Expenses"
            total={data.totalExpenses}
            colorClass="text-amber-700 border-t border-amber-200"
          />

          {/* Net Profit */}
          <div className={`flex justify-between py-3 font-bold text-lg border-t-2 ${data.netProfit >= 0 ? "text-emerald-700 border-emerald-500" : "text-red-700 border-red-500"}`}>
            <span>{lang === "ar" ? "صافي الربح" : "Net Profit"}</span>
            <span className="tabular-nums">{fmt(data.netProfit, true)}</span>
          </div>
        </div>
      )}
    </div>
  );
}
