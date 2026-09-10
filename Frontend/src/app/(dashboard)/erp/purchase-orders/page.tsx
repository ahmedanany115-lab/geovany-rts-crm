"use client";
import { usePurchaseOrders, useApprovePurchaseOrder } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { ShoppingBag, RefreshCw, CheckCircle } from "lucide-react";

const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Draft",     cls: "bg-muted text-muted-foreground" },
  2: { label: "Confirmed", cls: "bg-blue-100 text-blue-700" },
  3: { label: "Received",  cls: "bg-emerald-100 text-emerald-700" },
  4: { label: "Invoiced",  cls: "bg-purple-100 text-purple-700" },
  5: { label: "Cancelled", cls: "bg-red-100 text-red-700" },
};

export default function PurchaseOrdersPage() {
  const { t } = useT();
  const { toast } = useToast();
  const { data, isLoading, refetch } = usePurchaseOrders({});
  const approve = useApprovePurchaseOrder();

  const handleApprove = async (id: string, num: string) => {
    try { await approve.mutateAsync(id); toast(`PO ${num} confirmed.`, "success"); }
    catch (err: any) { toast(err?.message ?? "Action failed.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><ShoppingBag className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("purchase_orders")}</h1></div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">PO#</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("suppliers")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("date")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">{t("total")}</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(o => {
                const st = STATUS[o.status] ?? { label: "?", cls: "bg-muted" };
                return (
                  <tr key={o.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{o.poNumber}</td>
                    <td className="p-3 font-medium">{o.supplierName}</td>
                    <td className="p-3 text-muted-foreground">{o.orderDate}</td>
                    <td className="p-3 text-right tabular-nums">{o.totalAmount.toLocaleString()}</td>
                    <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${st.cls}`}>{st.label}</span></td>
                    <td className="p-3 text-right">
                      {o.status === 1 && (
                        <button onClick={() => handleApprove(o.id, o.poNumber)}
                          disabled={approve.isPending}
                          className="flex items-center gap-1 text-xs px-2 py-1 rounded hover:bg-accent text-emerald-600 disabled:opacity-50">
                          <CheckCircle className="h-3.5 w-3.5" />{t("approve")}
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
              {!data?.length && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
