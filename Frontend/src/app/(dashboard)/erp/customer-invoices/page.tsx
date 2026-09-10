"use client";
import { useState } from "react";
import { useCustomerInvoices, usePostCustomerInvoice, useCreateCustomerPayment } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { Receipt, RefreshCw, CheckCircle, DollarSign, X } from "lucide-react";

const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Draft",    cls: "bg-muted text-muted-foreground" },
  2: { label: "Posted",   cls: "bg-blue-100 text-blue-700" },
  3: { label: "Partial",  cls: "bg-amber-100 text-amber-700" },
  4: { label: "Paid",     cls: "bg-emerald-100 text-emerald-700" },
  5: { label: "Cancelled",cls: "bg-red-100 text-red-700" },
};

export default function CustomerInvoicesPage() {
  const { t } = useT();
  const { toast } = useToast();
  const [filterStatus, setFilterStatus] = useState<number | undefined>();
  const { data, isLoading, refetch } = useCustomerInvoices({ status: filterStatus });
  const post     = usePostCustomerInvoice();
  const record   = useCreateCustomerPayment();
  const [payingId, setPayingId] = useState<string | null>(null);
  const [payAmt, setPayAmt] = useState("");

  const handlePost = async (id: string, num: string) => {
    try { await post.mutateAsync(id); toast(`Invoice ${num} posted.`, "success"); }
    catch (err: any) { toast(err?.message ?? "Post failed.", "error"); }
  };

  const handlePay = async (inv: typeof data extends (infer T)[] | undefined ? T : never) => {
    try {
      await record.mutateAsync({
        customerId: (inv as any).customerId,
        paymentDate: new Date().toISOString().split("T")[0],
        amount: Number(payAmt),
        paymentMethod: 1,
        currencyId: (inv as any).currencyId,
        exchangeRate: 1,
        invoiceIds: [(inv as any).id],
      });
      toast("Payment recorded.", "success");
      setPayingId(null); setPayAmt("");
    } catch (err: any) { toast(err?.message ?? "Payment failed.", "error"); }
  };

  const totals = {
    invoiced: data?.reduce((s, i) => s + i.totalAmount, 0) ?? 0,
    paid:     data?.reduce((s, i) => s + i.paidAmount, 0) ?? 0,
    balance:  data?.reduce((s, i) => s + i.balanceDue, 0) ?? 0,
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Receipt className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("customer_invoices")}</h1></div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      {/* KPI strip */}
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Invoiced",   value: totals.invoiced, cls: "text-blue-600" },
          { label: "Collected",  value: totals.paid,     cls: "text-emerald-600" },
          { label: "Outstanding",value: totals.balance,  cls: "text-amber-600" },
        ].map(k => (
          <div key={k.label} className="card p-3 text-center">
            <p className="text-xs text-muted-foreground mb-1">{k.label}</p>
            <p className={`text-lg font-bold tabular-nums ${k.cls}`}>{k.value.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 })}</p>
          </div>
        ))}
      </div>
      {/* Filter */}
      <div className="flex gap-2">
        {[undefined, 1, 2, 3, 4].map(s => (
          <button key={String(s)} onClick={() => setFilterStatus(s)}
            className={`text-xs px-3 py-1.5 rounded-full font-medium transition-colors ${filterStatus === s ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/80"}`}>
            {s === undefined ? "All" : STATUS[s]?.label ?? s}
          </button>
        ))}
      </div>
      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Invoice #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("customers")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("date")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">{t("total")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Paid</th>
              <th className="text-right p-3 font-medium text-muted-foreground">{t("balance")}</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(inv => {
                const st = STATUS[inv.status] ?? { label: "?", cls: "bg-muted" };
                return (
                  <tr key={inv.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{inv.invoiceNumber}</td>
                    <td className="p-3 font-medium">{inv.customerName}</td>
                    <td className="p-3 text-muted-foreground">{inv.invoiceDate}</td>
                    <td className="p-3 text-right tabular-nums">{inv.totalAmount.toLocaleString()}</td>
                    <td className="p-3 text-right tabular-nums text-emerald-600">{inv.paidAmount.toLocaleString()}</td>
                    <td className={`p-3 text-right tabular-nums font-semibold ${inv.balanceDue > 0 ? "text-amber-600" : "text-emerald-600"}`}>{inv.balanceDue.toLocaleString()}</td>
                    <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${st.cls}`}>{st.label}</span></td>
                    <td className="p-3">
                      <div className="flex items-center gap-1 justify-end">
                        {inv.status === 1 && (
                          <button onClick={() => handlePost(inv.id, inv.invoiceNumber)}
                            className="flex items-center gap-1 text-xs px-2 py-1 rounded hover:bg-accent text-blue-600">
                            <CheckCircle className="h-3.5 w-3.5" />Post
                          </button>
                        )}
                        {inv.balanceDue > 0 && inv.status >= 2 && (
                          payingId === inv.id ? (
                            <div className="flex items-center gap-1">
                              <input type="number" value={payAmt} onChange={e => setPayAmt(e.target.value)}
                                placeholder={String(inv.balanceDue)} className="input w-24 py-1 text-xs" />
                              <button onClick={() => handlePay(inv)} disabled={record.isPending}
                                className="text-xs px-2 py-1 rounded bg-emerald-600 text-white disabled:opacity-50">
                                {record.isPending ? "..." : t("save")}
                              </button>
                              <button onClick={() => setPayingId(null)} className="p-1 hover:bg-accent rounded">
                                <X className="h-3.5 w-3.5" />
                              </button>
                            </div>
                          ) : (
                            <button onClick={() => { setPayingId(inv.id); setPayAmt(String(inv.balanceDue)); }}
                              className="flex items-center gap-1 text-xs px-2 py-1 rounded hover:bg-accent text-emerald-600">
                              <DollarSign className="h-3.5 w-3.5" />Pay
                            </button>
                          )
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
              {!data?.length && <tr><td colSpan={8} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
