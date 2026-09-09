"use client";
import { useState } from "react";
import { useTrialBalance } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { BarChart3, RefreshCw } from "lucide-react";

export default function TrialBalancePage() {
  const { t } = useT();
  const [params, setParams] = useState<{ fromDate?: string; toDate?: string }>({});
  const { data, isLoading, refetch } = useTrialBalance(params);

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><BarChart3 className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("trial_balance")}</h1></div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      <div className="flex gap-3">
        <input type="date" value={params.fromDate ?? ""} onChange={e => setParams(p => ({ ...p, fromDate: e.target.value || undefined }))} className="input text-sm" />
        <input type="date" value={params.toDate ?? ""} onChange={e => setParams(p => ({ ...p, toDate: e.target.value || undefined }))} className="input text-sm" />
      </div>
      {isLoading ? <div className="p-8 text-center text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("code")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Opening {t("debit")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Opening {t("credit")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Period {t("debit")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Period {t("credit")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Closing {t("debit")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Closing {t("credit")}</th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.lines.map(r => (
                <tr key={r.accountId} className="hover:bg-muted/20">
                  <td className="p-3 font-mono text-xs">{r.accountCode}</td>
                  <td className="p-3">{r.accountName}</td>
                  <td className="p-3 text-right tabular-nums">{r.openingDebit > 0 ? r.openingDebit.toLocaleString() : "—"}</td>
                  <td className="p-3 text-right tabular-nums">{r.openingCredit > 0 ? r.openingCredit.toLocaleString() : "—"}</td>
                  <td className="p-3 text-right tabular-nums">{r.periodDebit > 0 ? r.periodDebit.toLocaleString() : "—"}</td>
                  <td className="p-3 text-right tabular-nums">{r.periodCredit > 0 ? r.periodCredit.toLocaleString() : "—"}</td>
                  <td className="p-3 text-right tabular-nums">{r.closingDebit > 0 ? r.closingDebit.toLocaleString() : "—"}</td>
                  <td className="p-3 text-right tabular-nums">{r.closingCredit > 0 ? r.closingCredit.toLocaleString() : "—"}</td>
                </tr>
              ))}
              {!data?.lines.length && <tr><td colSpan={8} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
            {data && <tfoot className="bg-muted/50 font-semibold text-xs">
              <tr>
                <td colSpan={2} className="p-3">{t("total")} — {data.isBalanced ? "✓ Balanced" : "⚠ Imbalanced"}</td>
                <td className="p-3 text-right tabular-nums">{data.totalOpeningDebit.toLocaleString()}</td>
                <td className="p-3 text-right tabular-nums">{data.totalOpeningCredit.toLocaleString()}</td>
                <td className="p-3 text-right tabular-nums">{data.totalPeriodDebit.toLocaleString()}</td>
                <td className="p-3 text-right tabular-nums">{data.totalPeriodCredit.toLocaleString()}</td>
                <td className="p-3 text-right tabular-nums">{data.totalClosingDebit.toLocaleString()}</td>
                <td className="p-3 text-right tabular-nums">{data.totalClosingCredit.toLocaleString()}</td>
              </tr>
            </tfoot>}
          </table>
        </div>
      )}
    </div>
  );
}
