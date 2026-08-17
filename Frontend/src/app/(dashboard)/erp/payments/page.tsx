"use client";
import { DollarSign, RefreshCw } from "lucide-react";
import { useCustomerPayments, useSupplierPayments } from "@/features/erp/hooks";
export default function PaymentsPage() {
  const { data: customerPayments, isLoading: cpLoading, refetch: cpRefetch } = useCustomerPayments();
  const { data: supplierPayments, isLoading: spLoading } = useSupplierPayments();
  const totalIn = customerPayments?.reduce((s, p) => s + p.amount, 0) ?? 0;
  const totalOut = supplierPayments?.reduce((s, p) => s + p.amount, 0) ?? 0;
  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><DollarSign className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Payments</h1></div>
        <button onClick={() => cpRefetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="card p-4"><p className="text-sm text-muted-foreground">Total Customer Receipts</p><p className="text-2xl font-bold text-emerald-600 mt-1">{totalIn.toLocaleString("en-EG",{style:"currency",currency:"EGP"})}</p></div>
        <div className="card p-4"><p className="text-sm text-muted-foreground">Total Supplier Payments</p><p className="text-2xl font-bold text-red-600 mt-1">{totalOut.toLocaleString("en-EG",{style:"currency",currency:"EGP"})}</p></div>
      </div>
      <h2 className="font-semibold">Customer Receipts</h2>
      {cpLoading ? <div className="text-center py-6 text-muted-foreground">Loading...</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr><th className="text-left p-3 font-medium text-muted-foreground">Ref</th><th className="text-left p-3 font-medium text-muted-foreground">Customer</th><th className="text-left p-3 font-medium text-muted-foreground">Date</th><th className="text-left p-3 font-medium text-muted-foreground">Method</th><th className="text-right p-3 font-medium text-muted-foreground">Amount</th></tr></thead>
            <tbody className="divide-y divide-border">
              {customerPayments?.slice(0,20).map(p=>(
                <tr key={p.id} className="hover:bg-muted/20"><td className="p-3 font-mono text-xs">{p.paymentNumber}</td><td className="p-3">{p.customerName}</td><td className="p-3 text-muted-foreground">{p.paymentDate}</td><td className="p-3 text-muted-foreground">{p.paymentMethodName}</td><td className="p-3 text-right tabular-nums font-medium text-emerald-600">{p.amount.toLocaleString("en-EG",{style:"currency",currency:p.currencyCode||"EGP"})}</td></tr>
              ))}
              {!customerPayments?.length && <tr><td colSpan={5} className="p-6 text-center text-muted-foreground">No payments found</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
