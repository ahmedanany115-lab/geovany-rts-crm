"use client";
import { useState } from "react";
import { useSalesOrders, useApproveSalesOrder } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { FileCheck2, RefreshCw, CheckCircle, FileText } from "lucide-react";
import Link from "next/link";

// Quotations = Sales Orders in Draft status.
// Approving a draft converts it to a confirmed sales order
// which then triggers the AR/Invoice accounting pipeline.

const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Draft / Quote", cls: "bg-amber-100 text-amber-700" },
  2: { label: "Confirmed",     cls: "bg-blue-100 text-blue-700" },
  3: { label: "Part. Delivered", cls: "bg-indigo-100 text-indigo-700" },
  4: { label: "Delivered",    cls: "bg-emerald-100 text-emerald-700" },
  5: { label: "Cancelled",    cls: "bg-red-100 text-red-700" },
};

export default function QuotationsPage() {
  const { t } = useT();
  const { toast } = useToast();
  // Show all orders but highlight drafts as quotations
  const { data: allOrders, isLoading, refetch } = useSalesOrders({ status: undefined });
  const approve = useApproveSalesOrder();

  // Quotations = Draft SOs
  const quotes   = (allOrders ?? []).filter(o => o.status === 1);
  const pipeline = (allOrders ?? []).filter(o => o.status > 1 && o.status < 5);

  const [tab, setTab] = useState<"quotes" | "pipeline">("quotes");
  const rows = tab === "quotes" ? quotes : pipeline;

  const handleApprove = async (id: string, num: string) => {
    try {
      await approve.mutateAsync(id);
      toast(`Quotation ${num} confirmed → Sales Order. Accounting entry will be generated on invoice.`, "success");
    } catch (err: any) { toast(err?.message ?? "Approval failed.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <FileCheck2 className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">{t("quotations")}</h1>
            <p className="text-sm text-muted-foreground">
              {quotes.length} open quote{quotes.length !== 1 ? "s" : ""} · {pipeline.length} in progress
            </p>
          </div>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <Link href="/erp/sales-orders" className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            <FileText className="h-4 w-4" /> All Orders
          </Link>
        </div>
      </div>

      {/* Info banner */}
      <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
        <strong>Quotation workflow:</strong> Draft orders are treated as quotations.
        Clicking <em>Confirm</em> converts the quote to a Sales Order.
        An AR accounting entry is generated automatically when the customer invoice is posted.
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-border">
        {([["quotes", "Open Quotes"], ["pipeline", "Sales Pipeline"]] as const).map(([k, lbl]) => (
          <button key={k} onClick={() => setTab(k)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${tab === k ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}>
            {lbl} ({k === "quotes" ? quotes.length : pipeline.length})
          </button>
        ))}
      </div>

      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">SO #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("date")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">{t("total")}</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {rows.map(o => {
                const st = STATUS[o.status] ?? { label: "?", cls: "bg-muted" };
                return (
                  <tr key={o.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{o.soNumber}</td>
                    <td className="p-3 font-semibold">{o.customerName}</td>
                    <td className="p-3 text-muted-foreground">{o.orderDate}</td>
                    <td className="p-3 text-right tabular-nums">
                      {o.totalAmount.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 })}
                    </td>
                    <td className="p-3 text-center">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${st.cls}`}>{st.label}</span>
                    </td>
                    <td className="p-3 text-right">
                      {o.status === 1 && (
                        <button onClick={() => handleApprove(o.id, o.soNumber)} disabled={approve.isPending}
                          className="flex items-center gap-1 text-xs px-3 py-1.5 rounded bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50">
                          <CheckCircle className="h-3.5 w-3.5" /> Confirm Quote
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
              {rows.length === 0 && (
                <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">
                  {tab === "quotes" ? "No open quotations. Create a Sales Order in Draft status." : "No active pipeline."}
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
