"use client";
import { useBankAccounts } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { Landmark, RefreshCw } from "lucide-react";

export default function BankAccountsPage() {
  const { t } = useT();
  const { data, isLoading, refetch } = useBankAccounts({});

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Landmark className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("bank_accounts")}</h1></div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("code")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Bank</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("currency")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Current {t("balance")}</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(b => (
                <tr key={b.id} className="hover:bg-muted/20">
                  <td className="p-3 font-mono text-xs">{b.code}</td>
                  <td className="p-3 font-medium">{b.name}</td>
                  <td className="p-3 text-muted-foreground">{b.bankName ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">{b.currencyCode}</td>
                  <td className="p-3 text-right tabular-nums font-semibold">{b.currentBalance?.toLocaleString()}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${b.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>{b.isActive ? t("active") : t("inactive")}</span></td>
                </tr>
              ))}
              {!data?.length && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
