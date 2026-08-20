"use client";

import { useState } from "react";
import { DollarSign, RefreshCw, Plus, X } from "lucide-react";
import {
  useCustomerPayments, useCreateCustomerPayment,
  useSupplierPayments, useCreateSupplierPayment,
  useCustomers, useSuppliers, useBankAccounts,
} from "@/features/erp/hooks";

export default function PaymentsPage() {
  const [tab, setTab] = useState<"customer" | "supplier">("customer");
  const [showForm, setShowForm] = useState(false);
  const today = new Date().toISOString().split("T")[0];

  const { data: customerPayments, isLoading: cpLoading, refetch: cpRefetch } = useCustomerPayments();
  const { data: supplierPayments, isLoading: spLoading, refetch: spRefetch } = useSupplierPayments();
  const { data: customers } = useCustomers({ isActive: true });
  const { data: suppliers } = useSuppliers({ isActive: true });
  const { data: bankAccounts } = useBankAccounts({ isActive: true });

  const createCustPmt = useCreateCustomerPayment();
  const createSuppPmt = useCreateSupplierPayment();

  const [custForm, setCustForm] = useState({ customerId: "", paymentDate: today, amount: "", bankAccountId: "", paymentMethod: "1", notes: "" });
  const [suppForm, setSuppForm] = useState({ supplierId: "", paymentDate: today, amount: "", bankAccountId: "", notes: "" });

  const totalIn = customerPayments?.reduce((s, p) => s + p.amount, 0) ?? 0;
  const totalOut = supplierPayments?.reduce((s, p) => s + p.amount, 0) ?? 0;

  const handleCustSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const bank = bankAccounts?.find(b => b.id === custForm.bankAccountId);
    await createCustPmt.mutateAsync({
      customerId: custForm.customerId,
      paymentDate: custForm.paymentDate,
      amount: parseFloat(custForm.amount),
      amountBase: parseFloat(custForm.amount),
      currencyId: bank?.id ?? custForm.bankAccountId,
      bankAccountId: custForm.bankAccountId || undefined,
      paymentMethod: parseInt(custForm.paymentMethod),
      exchangeRate: 1,
      notes: custForm.notes,
    });
    setCustForm({ customerId: "", paymentDate: today, amount: "", bankAccountId: "", paymentMethod: "1", notes: "" });
    setShowForm(false);
  };

  const handleSuppSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await createSuppPmt.mutateAsync({
      supplierId: suppForm.supplierId,
      paymentDate: suppForm.paymentDate,
      amount: parseFloat(suppForm.amount),
      amountBase: parseFloat(suppForm.amount),
      bankAccountId: suppForm.bankAccountId,
      paymentMethod: 1,
      exchangeRate: 1,
      notes: suppForm.notes,
    });
    setSuppForm({ supplierId: "", paymentDate: today, amount: "", bankAccountId: "", notes: "" });
    setShowForm(false);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <DollarSign className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">Payments</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={() => { cpRefetch(); spRefetch(); }} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? "Cancel" : "New Payment"}
          </button>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="card p-4"><p className="text-sm text-muted-foreground">Customer Receipts</p><p className="text-2xl font-bold text-emerald-600 mt-1">{totalIn.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p></div>
        <div className="card p-4"><p className="text-sm text-muted-foreground">Supplier Payments</p><p className="text-2xl font-bold text-red-600 mt-1">{totalOut.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p></div>
      </div>

      <div className="flex gap-1 border-b border-border">
        {(["customer", "supplier"] as const).map(t => (
          <button key={t} onClick={() => setTab(t)}
            className={`px-4 py-2 text-sm font-medium transition-colors border-b-2 -mb-px ${tab === t ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}>
            {t === "customer" ? "Customer Receipts" : "Supplier Payments"}
          </button>
        ))}
      </div>

      {showForm && tab === "customer" && (
        <form onSubmit={handleCustSubmit} className="card p-5 space-y-4 border border-emerald-200 dark:border-emerald-800">
          <h2 className="font-semibold text-sm">New Customer Receipt</h2>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">Customer *</label>
              <select required value={custForm.customerId} onChange={e => setCustForm(f => ({ ...f, customerId: e.target.value }))} className="input w-full">
                <option value="">Select customer...</option>
                {customers?.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Date *</label>
              <input type="date" required value={custForm.paymentDate} onChange={e => setCustForm(f => ({ ...f, paymentDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Amount *</label>
              <input type="number" required min="0.01" step="0.01" value={custForm.amount} onChange={e => setCustForm(f => ({ ...f, amount: e.target.value }))} className="input w-full" placeholder="0.00" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Method</label>
              <select value={custForm.paymentMethod} onChange={e => setCustForm(f => ({ ...f, paymentMethod: e.target.value }))} className="input w-full">
                <option value="1">Bank Transfer</option><option value="2">Cheque</option><option value="3">Cash</option>
              </select></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Bank Account</label>
              <select value={custForm.bankAccountId} onChange={e => setCustForm(f => ({ ...f, bankAccountId: e.target.value }))} className="input w-full">
                <option value="">Select bank account...</option>
                {bankAccounts?.map(b => <option key={b.id} value={b.id}>{b.name} ({b.currencyCode})</option>)}
              </select></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Notes</label>
              <input value={custForm.notes} onChange={e => setCustForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" placeholder="Optional..." /></div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={createCustPmt.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">{createCustPmt.isPending ? "Saving..." : "Record Receipt"}</button>
            <button type="button" onClick={() => setShowForm(false)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
          </div>
        </form>
      )}

      {showForm && tab === "supplier" && (
        <form onSubmit={handleSuppSubmit} className="card p-5 space-y-4 border border-red-200 dark:border-red-800">
          <h2 className="font-semibold text-sm">New Supplier Payment</h2>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">Supplier *</label>
              <select required value={suppForm.supplierId} onChange={e => setSuppForm(f => ({ ...f, supplierId: e.target.value }))} className="input w-full">
                <option value="">Select supplier...</option>
                {suppliers?.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Date *</label>
              <input type="date" required value={suppForm.paymentDate} onChange={e => setSuppForm(f => ({ ...f, paymentDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Amount *</label>
              <input type="number" required min="0.01" step="0.01" value={suppForm.amount} onChange={e => setSuppForm(f => ({ ...f, amount: e.target.value }))} className="input w-full" placeholder="0.00" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Bank Account *</label>
              <select required value={suppForm.bankAccountId} onChange={e => setSuppForm(f => ({ ...f, bankAccountId: e.target.value }))} className="input w-full">
                <option value="">Select bank account...</option>
                {bankAccounts?.map(b => <option key={b.id} value={b.id}>{b.name} ({b.currencyCode})</option>)}
              </select></div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Notes</label>
              <input value={suppForm.notes} onChange={e => setSuppForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" placeholder="Optional..." /></div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={createSuppPmt.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">{createSuppPmt.isPending ? "Saving..." : "Record Payment"}</button>
            <button type="button" onClick={() => setShowForm(false)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
          </div>
        </form>
      )}

      {tab === "customer" && (cpLoading ? <div className="text-center py-8 text-muted-foreground">Loading...</div> : (
        <div className="card overflow-hidden"><table className="w-full text-sm">
          <thead className="bg-muted/30"><tr>
            <th className="text-left p-3 font-medium text-muted-foreground">Ref</th>
            <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
            <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
            <th className="text-left p-3 font-medium text-muted-foreground">Method</th>
            <th className="text-right p-3 font-medium text-muted-foreground">Amount</th>
            <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
          </tr></thead>
          <tbody className="divide-y divide-border">
            {customerPayments?.map(p => (
              <tr key={p.id} className="hover:bg-muted/20">
                <td className="p-3 font-mono text-xs">{p.paymentNumber}</td>
                <td className="p-3 font-medium">{p.customerName}</td>
                <td className="p-3 text-muted-foreground">{p.paymentDate}</td>
                <td className="p-3 text-muted-foreground">{p.paymentMethodName}</td>
                <td className="p-3 text-right tabular-nums font-medium text-emerald-600">{p.amount.toLocaleString("en-EG", { style: "currency", currency: p.currencyCode || "EGP" })}</td>
                <td className="p-3 text-center"><span className="text-xs px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-700">{p.statusName}</span></td>
              </tr>
            ))}
            {!customerPayments?.length && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">No customer payments found</td></tr>}
          </tbody>
        </table></div>
      ))}

      {tab === "supplier" && (spLoading ? <div className="text-center py-8 text-muted-foreground">Loading...</div> : (
        <div className="card overflow-hidden"><table className="w-full text-sm">
          <thead className="bg-muted/30"><tr>
            <th className="text-left p-3 font-medium text-muted-foreground">Ref</th>
            <th className="text-left p-3 font-medium text-muted-foreground">Supplier</th>
            <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
            <th className="text-right p-3 font-medium text-muted-foreground">Amount</th>
            <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
          </tr></thead>
          <tbody className="divide-y divide-border">
            {supplierPayments?.map(p => (
              <tr key={p.id} className="hover:bg-muted/20">
                <td className="p-3 font-mono text-xs">{p.paymentNumber}</td>
                <td className="p-3 font-medium">{p.supplierName}</td>
                <td className="p-3 text-muted-foreground">{p.paymentDate}</td>
                <td className="p-3 text-right tabular-nums font-medium text-red-600">{p.amount.toLocaleString("en-EG", { style: "currency", currency: p.currencyCode || "EGP" })}</td>
                <td className="p-3 text-center"><span className="text-xs px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-700">{p.statusName}</span></td>
              </tr>
            ))}
            {!supplierPayments?.length && <tr><td colSpan={5} className="p-8 text-center text-muted-foreground">No supplier payments found</td></tr>}
          </tbody>
        </table></div>
      ))}
    </div>
  );
}
