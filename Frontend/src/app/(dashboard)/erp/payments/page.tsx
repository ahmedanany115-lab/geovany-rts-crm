"use client";
import { useCustomerPayments, useSupplierPayments } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { DollarSign, RefreshCw } from "lucide-react";
import { useState } from "react";

export default function PaymentsPage() {
  const { t } = useT();
  const [tab, setTab] = useState<"customer" | "supplier">("customer");
  const { data: custPayments, isLoading: custLoading, refetch: custRefetch } = useCustomerPayments({});
  const { data: suppPayments, isLoading: suppLoading, refetch: suppRefetch } = useSupplierPayments({});

  const data    = tab === "customer" ? custPayments : suppPayments;
  const loading = tab === "customer" ? custLoading  : suppLoading;
  const refetch = tab === "customer" ? custRefetch  : suppRefetch;

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><DollarSign className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("payments")}</h1></div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      <div className="flex gap-1 border-b border-border">
        {(["customer", "supplier"] as const).map(k => (
          <button key={k} onClick={() => setTab(k)} className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${tab === k ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}>
            {k === "customer" ? t("customers") : t("suppliers")}
          </button>
        ))}
      </div>
      {loading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Payment #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{tab === "customer" ? t("customers") : t("suppliers")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("date")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Method</th>
              <th className="text-right p-3 font-medium text-muted-foreground">{t("amount")}</th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {(data as any[])?.map((p: any) => (
                <tr key={p.id} className="hover:bg-muted/20">
                  <td className="p-3 font-mono text-xs">{p.paymentNumber}</td>
                  <td className="p-3 font-medium">{p.customerName ?? p.supplierName ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">{p.paymentDate}</td>
                  <td className="p-3 text-muted-foreground">{p.paymentMethodName ?? "—"}</td>
                  <td className="p-3 text-right tabular-nums font-semibold">{p.amount?.toLocaleString()}</td>
                </tr>
              ))}
              {!data?.length && <tr><td colSpan={5} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
