"use client";
import { useCustomerInvoices, useSalesOrders } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { BarChart3, DollarSign, TrendingUp, FileText, Users, RefreshCw } from "lucide-react";
import Link from "next/link";

export default function SalesReportPage() {
  const { t } = useT();
  const { data: invoices, refetch } = useCustomerInvoices({ status: undefined });
  const { data: orders } = useSalesOrders({ status: undefined });

  const totalInvoiced  = invoices?.reduce((s, i) => s + i.totalAmount, 0) ?? 0;
  const totalCollected = invoices?.reduce((s, i) => s + i.paidAmount,  0) ?? 0;
  const outstanding    = totalInvoiced - totalCollected;

  const kpis = [
    { label: t("total_revenue"),   value: totalInvoiced.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 }),  icon: TrendingUp, color: "text-emerald-600" },
    { label: t("payments"),        value: totalCollected.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 }), icon: DollarSign, color: "text-blue-600" },
    { label: t("outstanding_ar"),  value: outstanding.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 }),    icon: FileText,   color: "text-amber-600" },
    { label: t("sales_orders"),    value: String(orders?.length ?? 0),                                                                              icon: Users,      color: "text-purple-600" },
  ];

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><BarChart3 className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("sales_report")}</h1></div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {kpis.map(k => (
          <div key={k.label} className="card p-4">
            <div className="flex items-center gap-2 mb-2"><k.icon className={`h-5 w-5 ${k.color}`} /><p className="text-sm text-muted-foreground">{k.label}</p></div>
            <p className={`text-xl font-bold tabular-nums ${k.color}`}>{k.value}</p>
          </div>
        ))}
      </div>
      <div className="card overflow-hidden">
        <div className="p-4 border-b border-border font-medium">{t("customer_invoices")}</div>
        <table className="w-full text-sm">
          <thead className="bg-muted/30"><tr>
            <th className="text-left p-3 text-muted-foreground">Invoice #</th>
            <th className="text-left p-3 text-muted-foreground">{t("customers")}</th>
            <th className="text-left p-3 text-muted-foreground">{t("date")}</th>
            <th className="text-right p-3 text-muted-foreground">{t("total")}</th>
            <th className="text-right p-3 text-muted-foreground">{t("balance")}</th>
            <th className="text-center p-3 text-muted-foreground">{t("status")}</th>
          </tr></thead>
          <tbody className="divide-y divide-border">
            {invoices?.slice(0, 20).map(i => (
              <tr key={i.id} className="hover:bg-muted/20">
                <td className="p-3 font-mono text-xs">{i.invoiceNumber}</td>
                <td className="p-3">{i.customerName}</td>
                <td className="p-3 text-muted-foreground">{i.invoiceDate}</td>
                <td className="p-3 text-right tabular-nums">{i.totalAmount.toLocaleString()}</td>
                <td className={`p-3 text-right tabular-nums ${i.balanceDue > 0 ? "text-amber-600" : "text-emerald-600"}`}>{i.balanceDue.toLocaleString()}</td>
                <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${i.status >= 3 ? "bg-emerald-100 text-emerald-700" : i.status === 2 ? "bg-blue-100 text-blue-700" : "bg-muted text-muted-foreground"}`}>{i.statusName}</span></td>
              </tr>
            ))}
            {!invoices?.length && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
          </tbody>
        </table>
      </div>
      <div className="flex gap-3 text-sm">
        <Link href="/erp/customer-invoices" className="btn-ghost px-4 py-2 rounded-lg">→ {t("customer_invoices")}</Link>
        <Link href="/erp/sales-orders" className="btn-ghost px-4 py-2 rounded-lg">→ {t("sales_orders")}</Link>
        <Link href="/erp/payments" className="btn-ghost px-4 py-2 rounded-lg">→ {t("payments")}</Link>
      </div>
    </div>
  );
}
