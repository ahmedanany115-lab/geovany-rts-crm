"use client";

import { useState } from "react";
import { FileText, RefreshCw, Send, CheckCircle2 } from "lucide-react";
import { useCustomerInvoices, usePostCustomerInvoice } from "@/features/erp/hooks";
import { InvoiceStatusLabels } from "@/features/erp/types";

const STATUS_COLORS: Record<number, string> = {
  1: "bg-muted text-muted-foreground",
  2: "bg-blue-100 text-blue-700",
  3: "bg-amber-100 text-amber-700",
  4: "bg-emerald-100 text-emerald-700",
  5: "bg-red-100 text-red-700",
};

export default function CustomerInvoicesPage() {
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const { data: invoices, isLoading, refetch } = useCustomerInvoices({ status: statusFilter });
  const post = usePostCustomerInvoice();

  const totalOutstanding = invoices?.reduce((s, i) => s + i.balanceDue, 0) ?? 0;
  const totalPosted = invoices?.filter(i => i.status >= 2).reduce((s, i) => s + i.totalAmount, 0) ?? 0;

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <FileText className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">Customer Invoices</h1>
        </div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg">
          <RefreshCw className="h-4 w-4" />
        </button>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 gap-4">
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Total Invoiced (Posted)</p>
          <p className="text-2xl font-bold mt-1">{totalPosted.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p>
        </div>
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Outstanding Balance</p>
          <p className="text-2xl font-bold mt-1 text-amber-600">{totalOutstanding.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p>
        </div>
      </div>

      {/* Status filter */}
      <div className="flex gap-2 flex-wrap">
        {[undefined, 1, 2, 3, 4].map(s => (
          <button key={String(s)} onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${
              statusFilter === s ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/80"
            }`}>
            {s === undefined ? "All" : InvoiceStatusLabels[s]}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">Loading invoices...</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30">
              <tr>
                <th className="text-left p-3 font-medium text-muted-foreground">Invoice #</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Due Date</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Total</th>
                <th className="text-right p-3 font-medium text-muted-foreground">VAT</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Balance Due</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
                <th className="p-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {invoices?.map(i => (
                <tr key={i.id} className="hover:bg-muted/20 transition-colors">
                  <td className="p-3 font-mono text-xs">{i.invoiceNumber}</td>
                  <td className="p-3 font-medium">{i.customerName}</td>
                  <td className="p-3 text-muted-foreground">{i.invoiceDate}</td>
                  <td className="p-3 text-muted-foreground">{i.dueDate}</td>
                  <td className="p-3 text-right tabular-nums">{i.totalAmount.toLocaleString("en-EG", { style: "currency", currency: i.currencyCode || "EGP" })}</td>
                  <td className="p-3 text-right tabular-nums text-muted-foreground">{i.taxAmount.toLocaleString("en-EG", { style: "currency", currency: i.currencyCode || "EGP" })}</td>
                  <td className={`p-3 text-right tabular-nums font-medium ${i.balanceDue > 0 ? "text-amber-600" : "text-emerald-600"}`}>
                    {i.balanceDue.toLocaleString("en-EG", { style: "currency", currency: i.currencyCode || "EGP" })}
                  </td>
                  <td className="p-3 text-center">
                    <span className={`text-xs px-2 py-0.5 rounded-full ${STATUS_COLORS[i.status]}`}>
                      {i.statusName}
                    </span>
                  </td>
                  <td className="p-3">
                    {i.status === 1 && (
                      <button
                        onClick={() => post.mutate(i.id)}
                        disabled={post.isPending}
                        className="text-blue-600 hover:text-blue-700 transition-colors flex items-center gap-1 text-xs"
                        title="Post Invoice"
                      >
                        <Send className="h-3.5 w-3.5" /> Post
                      </button>
                    )}
                    {i.status >= 2 && <CheckCircle2 className="h-4 w-4 text-emerald-500" />}
                  </td>
                </tr>
              ))}
              {!invoices?.length && (
                <tr><td colSpan={9} className="p-8 text-center text-muted-foreground">No invoices found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
