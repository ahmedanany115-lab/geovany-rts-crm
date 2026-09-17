"use client";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { usePrint } from "@/hooks/usePrint";
import { exportCsv } from "@/lib/export-csv";
import { BarChart3, RefreshCw, Printer, Download } from "lucide-react";

type Tab = "summary" | "by-customer" | "by-item" | "by-rep";
const EGP = (v: number) => v.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 });

export default function SalesReportsPage() {
  const { t } = useT();
  const { printRef, handlePrint } = usePrint("RTS ERP — Sales Report");
  const year = new Date().getFullYear();
  const [tab, setTab]       = useState<Tab>("summary");
  const [from, setFrom]     = useState(`${year}-01-01`);
  const [to, setTo]         = useState(new Date().toISOString().split("T")[0]);

  const qs = `?fromDate=${from}&toDate=${to}`;

  const { data: summary, isLoading: sL }    = useQuery({ queryKey: ["sales-report-summary", from, to],    queryFn: () => apiFetch<any>(`/reports/sales-summary${qs}`) });
  const { data: byCustomer, isLoading: cL }  = useQuery({ queryKey: ["sales-report-customer", from, to],  queryFn: () => apiFetch<any[]>(`/reports/sales-by-customer${qs}`) });
  const { data: byItem, isLoading: iL }      = useQuery({ queryKey: ["sales-report-item", from, to],      queryFn: () => apiFetch<any[]>(`/reports/sales-by-item${qs}`) });
  const { data: byRep, isLoading: rL }       = useQuery({ queryKey: ["sales-report-rep", from, to],       queryFn: () => apiFetch<any[]>(`/reports/sales-by-rep${qs}`) });

  const loading = sL || cL || iL || rL;

  const handleExport = () => {
    if (tab === "summary" && summary) {
      exportCsv([summary], `sales-summary-${from}-to-${to}.csv`);
    } else if (tab === "by-customer" && byCustomer) {
      exportCsv(byCustomer, `sales-by-customer-${from}-to-${to}.csv`, [
        { key: "customerName", header: "Customer" },
        { key: "orderCount", header: "Orders" },
        { key: "totalRevenue", header: "Revenue" },
        { key: "totalVat", header: "VAT" },
        { key: "totalCollected", header: "Collected" },
        { key: "totalOutstanding", header: "Outstanding" },
      ]);
    } else if (tab === "by-item" && byItem) {
      exportCsv(byItem, `sales-by-item-${from}-to-${to}.csv`, [
        { key: "productSku", header: "SKU" },
        { key: "productName", header: "Product" },
        { key: "quantitySold", header: "Qty Sold" },
        { key: "totalRevenue", header: "Revenue" },
        { key: "totalDiscount", header: "Discount" },
        { key: "totalVat", header: "VAT" },
        { key: "netRevenue", header: "Net Revenue" },
      ]);
    } else if (tab === "by-rep" && byRep) {
      exportCsv(byRep, `sales-by-rep-${from}-to-${to}.csv`, [
        { key: "salesRepName", header: "Sales Rep" },
        { key: "orderCount", header: "Orders" },
        { key: "totalRevenue", header: "Revenue" },
        { key: "totalCollected", header: "Collected" },
        { key: "totalOutstanding", header: "Outstanding" },
        { key: "commission", header: "Commission" },
      ]);
    }
  };

  const tabs: { key: Tab; label: string }[] = [
    { key: "by-customer", label: "By Customer" },
    { key: "by-item",     label: "By Product/Item" },
    { key: "by-rep",      label: "By Sales Rep" },
  ];

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><BarChart3 className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Sales Reports</h1></div>
        <button onClick={handleExport} className="btn-ghost flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
          <Download className="h-4 w-4" /> Export CSV
        </button>
        <button onClick={handlePrint} className="btn-ghost flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
          <Printer className="h-4 w-4" /> Print
        </button>
      </div>

      {/* Date filters */}
      <div className="flex gap-3 items-end flex-wrap">
        <div><label className="text-xs text-muted-foreground block mb-1">From</label>
          <input type="date" value={from} onChange={e => setFrom(e.target.value)} className="input" /></div>
        <div><label className="text-xs text-muted-foreground block mb-1">To</label>
          <input type="date" value={to} min={from} onChange={e => setTo(e.target.value)} className="input" /></div>
        <button onClick={() => { setFrom(`${year}-01-01`); setTo(new Date().toISOString().split("T")[0]); }} className="btn-ghost text-xs px-3 py-2 rounded-lg">YTD</button>
        <button onClick={() => { const d=new Date(); setFrom(`${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-01`); setTo(new Date().toISOString().split("T")[0]); }} className="btn-ghost text-xs px-3 py-2 rounded-lg">This Month</button>
      </div>

      {/* Tab bar */}
      <div className="flex gap-1 border-b border-border">
        {tabs.map(tb => (
          <button key={tb.key} onClick={() => setTab(tb.key)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${tab === tb.key ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}>
            {tb.label}
          </button>
        ))}
      </div>

      <div ref={printRef}>
        {/* Print header */}
        <div className="print-header hidden">
          <div className="print-header-text">
            <h1>Royal Technology System</h1>
            <p>Sales Report — {tabs.find(t => t.key === tab)?.label} · {from} to {to}</p>
          </div>
        </div>

        {loading && <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>}

        {/* Summary */}
        {tab === "summary" && summary && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {[
                { label: "Total Revenue",    value: EGP(summary.totalRevenue ?? 0)      },
                { label: "Total VAT",        value: EGP(summary.totalVat ?? 0)          },
                { label: "Total Discounts",  value: EGP(summary.totalDiscounts ?? 0)    },
                { label: "Net Revenue",      value: EGP(summary.netRevenue ?? 0)        },
                { label: "Invoices",         value: String(summary.invoiceCount ?? 0)   },
                { label: "Orders",           value: String(summary.orderCount ?? 0)     },
                { label: "Collected",        value: EGP(summary.totalCollected ?? 0)    },
                { label: "Outstanding",      value: EGP(summary.totalOutstanding ?? 0)  },
              ].map(k => (
                <div key={k.label} className="card p-4"><p className="text-xs text-muted-foreground">{k.label}</p><p className="text-xl font-bold tabular-nums">{k.value}</p></div>
              ))}
            </div>
          </div>
        )}

        {/* By Customer */}
        {tab === "by-customer" && (
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30"><tr>
                <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Orders</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Revenue</th>
                <th className="text-right p-3 font-medium text-muted-foreground">VAT</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Collected</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Outstanding</th>
              </tr></thead>
              <tbody className="divide-y divide-border">
                {(byCustomer ?? []).map((r: any) => (
                  <tr key={r.customerId} className="hover:bg-muted/20">
                    <td className="p-3 font-medium">{r.customerName}</td>
                    <td className="p-3 text-right tabular-nums">{r.orderCount}</td>
                    <td className="p-3 text-right tabular-nums">{EGP(r.totalRevenue)}</td>
                    <td className="p-3 text-right tabular-nums">{EGP(r.totalVat)}</td>
                    <td className="p-3 text-right tabular-nums text-emerald-600">{EGP(r.totalCollected)}</td>
                    <td className="p-3 text-right tabular-nums text-red-600">{r.totalOutstanding > 0 ? EGP(r.totalOutstanding) : "—"}</td>
                  </tr>
                ))}
                {!byCustomer?.length && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">No data for this period.</td></tr>}
              </tbody>
            </table>
          </div>
        )}

        {/* By Item */}
        {tab === "by-item" && (
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30"><tr>
                <th className="text-left p-3 font-medium text-muted-foreground">Product</th>
                <th className="text-left p-3 font-medium text-muted-foreground">SKU</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Qty Sold</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Revenue</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Discount</th>
                <th className="text-right p-3 font-medium text-muted-foreground">VAT</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Net</th>
              </tr></thead>
              <tbody className="divide-y divide-border">
                {(byItem ?? []).map((r: any) => (
                  <tr key={r.productId} className="hover:bg-muted/20">
                    <td className="p-3 font-medium">{r.productName}</td>
                    <td className="p-3 font-mono text-xs">{r.productSku}</td>
                    <td className="p-3 text-right tabular-nums">{r.quantitySold}</td>
                    <td className="p-3 text-right tabular-nums">{EGP(r.totalRevenue)}</td>
                    <td className="p-3 text-right tabular-nums">{r.totalDiscount > 0 ? EGP(r.totalDiscount) : "—"}</td>
                    <td className="p-3 text-right tabular-nums">{EGP(r.totalVat)}</td>
                    <td className="p-3 text-right tabular-nums font-semibold">{EGP(r.netRevenue)}</td>
                  </tr>
                ))}
                {!byItem?.length && <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">No data for this period.</td></tr>}
              </tbody>
            </table>
          </div>
        )}

        {/* By Sales Rep */}
        {tab === "by-rep" && (
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30"><tr>
                <th className="text-left p-3 font-medium text-muted-foreground">Sales Rep</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Orders</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Revenue</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Collected</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Outstanding</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Commission</th>
              </tr></thead>
              <tbody className="divide-y divide-border">
                {(byRep ?? []).map((r: any) => (
                  <tr key={r.salesRepId ?? r.salesRepName} className="hover:bg-muted/20">
                    <td className="p-3 font-medium">{r.salesRepName ?? "Unassigned"}</td>
                    <td className="p-3 text-right tabular-nums">{r.orderCount}</td>
                    <td className="p-3 text-right tabular-nums">{EGP(r.totalRevenue)}</td>
                    <td className="p-3 text-right tabular-nums text-emerald-600">{EGP(r.totalCollected)}</td>
                    <td className="p-3 text-right tabular-nums text-red-600">{r.totalOutstanding > 0 ? EGP(r.totalOutstanding) : "—"}</td>
                    <td className="p-3 text-right tabular-nums">{r.commission > 0 ? EGP(r.commission) : "—"}</td>
                  </tr>
                ))}
                {!byRep?.length && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">No data for this period.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
