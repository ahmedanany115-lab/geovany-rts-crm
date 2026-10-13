"use client";

import { useState } from "react";
import { FileText, RefreshCw, Send, CheckCircle2, Plus, X, Check, AlertTriangle } from "lucide-react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useToast } from "@/components/ui/toast";
import { useT } from "@/hooks/useT";
import { useSuppliers } from "@/features/erp/hooks";
import { useCurrencies } from "@/features/finance/hooks";

const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Draft",          cls: "bg-muted text-muted-foreground" },
  2: { label: "Approved",       cls: "bg-blue-100 text-blue-700" },
  3: { label: "Partially Paid", cls: "bg-amber-100 text-amber-700" },
  4: { label: "Paid",           cls: "bg-emerald-100 text-emerald-700" },
  5: { label: "Overdue",        cls: "bg-red-100 text-red-700" },
  6: { label: "Cancelled",      cls: "bg-muted text-muted-foreground" },
};

const EMPTY_LINE = { description: "", quantity: 1, unitPrice: 0, accountId: "" };

export default function SupplierInvoicesPage() {
  const { t } = useT();
  const { toast } = useToast();
  const qc = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [form, setForm] = useState({
    supplierId: "", supplierInvoiceNumber: "",
    invoiceDate: new Date().toISOString().split("T")[0],
    dueDate: "", currencyId: "",
  });
  const [lines, setLines] = useState([{ ...EMPTY_LINE }]);

  const { data: suppliers } = useSuppliers({});
  const { data: currencies } = useCurrencies();

  const { data, isLoading, refetch } = useQuery({
    queryKey: ["supplier-invoices"],
    queryFn: () => apiFetch<any[]>("/supplierinvoices"),
  });

  const createMut = useMutation({
    mutationFn: (payload: any) => apiFetch<{ id: string }>("/supplierinvoices", { method: "POST", body: JSON.stringify(payload) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["supplier-invoices"] }); toast("Supplier invoice created.", "success"); resetForm(); },
    onError: (e: any) => { const msg = e?.message ?? "Failed to create."; setFormError(msg); toast(msg, "error"); },
  });

  const postMut = useMutation({
    mutationFn: (id: string) => apiFetch<void>(`/supplierinvoices/${id}/post`, { method: "POST" }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["supplier-invoices"] }); toast("Invoice posted to accounting.", "success"); },
    onError: (e: any) => toast(e?.message ?? "Failed to post.", "error"),
  });

  const resetForm = () => { setShowForm(false); setFormError(null); setForm({ supplierId: "", supplierInvoiceNumber: "", invoiceDate: new Date().toISOString().split("T")[0], dueDate: "", currencyId: "" }); setLines([{ ...EMPTY_LINE }]); };

  const setLine = (i: number, k: string, v: string | number) =>
    setLines(p => p.map((l, idx) => idx === i ? { ...l, [k]: v } : l));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setFormError(null);
    if (!form.supplierId) { setFormError("Select a supplier."); return; }
    if (!form.currencyId) { setFormError("Select a currency."); return; }
    if (!form.dueDate)    { setFormError("Due date is required."); return; }
    if (lines.some(l => !l.description || Number(l.unitPrice) <= 0)) { setFormError("All lines need a description and price > 0."); return; }

    createMut.mutate({
      supplierId: form.supplierId,
      supplierInvoiceNumber: form.supplierInvoiceNumber || undefined,
      invoiceDate: form.invoiceDate,
      dueDate: form.dueDate,
      currencyId: form.currencyId,
      exchangeRate: 1,
      lines: lines.map(l => ({
        description: l.description,
        quantity: Number(l.quantity),
        unitPrice: Number(l.unitPrice),
        lineTotal: Number(l.quantity) * Number(l.unitPrice),
        taxRate: 0,
      })),
    });
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><FileText className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Supplier Invoices</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={showForm ? resetForm : () => setShowForm(true)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : "New Supplier Invoice"}
          </button>
        </div>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">New Supplier Invoice</h2>
          {formError && <div className="flex items-center gap-2 rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700"><AlertTriangle className="h-4 w-4 shrink-0" />{formError}</div>}
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Supplier *</label>
              <select required value={form.supplierId} onChange={e => setForm(f => ({ ...f, supplierId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {suppliers?.filter((s: any) => s.isActive).map((s: any) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Supplier Invoice #</label>
              <input value={form.supplierInvoiceNumber} onChange={e => setForm(f => ({ ...f, supplierInvoiceNumber: e.target.value }))} className="input w-full" placeholder="Optional" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Invoice Date *</label>
              <input type="date" required value={form.invoiceDate} onChange={e => setForm(f => ({ ...f, invoiceDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Due Date *</label>
              <input type="date" required value={form.dueDate} min={form.invoiceDate} onChange={e => setForm(f => ({ ...f, dueDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Currency *</label>
              <select required value={form.currencyId} onChange={e => setForm(f => ({ ...f, currencyId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {currencies?.filter(c => c.isActive).map(c => <option key={c.id} value={c.id}>{c.code}</option>)}
              </select>
            </div>
          </div>
          {/* Line items */}
          <div>
            <div className="grid grid-cols-12 gap-2 text-xs font-medium text-muted-foreground pb-1 border-b">
              <span className="col-span-6">Description</span><span className="col-span-2">Qty</span><span className="col-span-3">Unit Price</span><span className="col-span-1"></span>
            </div>
            <div className="space-y-1.5 mt-2">
              {lines.map((l, i) => (
                <div key={i} className="grid grid-cols-12 gap-2 items-center">
                  <div className="col-span-6"><input value={l.description} onChange={e => setLine(i, "description", e.target.value)} className="input w-full text-sm py-1.5" placeholder="Item description" /></div>
                  <div className="col-span-2"><input type="number" min="0.01" step="0.01" value={l.quantity} onChange={e => setLine(i, "quantity", e.target.value)} className="input w-full text-sm py-1.5" /></div>
                  <div className="col-span-3"><input type="number" min="0" step="0.01" value={l.unitPrice} onChange={e => setLine(i, "unitPrice", e.target.value)} className="input w-full text-sm py-1.5 text-right" /></div>
                  <div className="col-span-1 flex justify-end">
                    <button type="button" onClick={() => setLines(p => p.length > 1 ? p.filter((_, idx) => idx !== i) : p)} disabled={lines.length <= 1} className="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 disabled:opacity-20">×</button>
                  </div>
                </div>
              ))}
            </div>
            <button type="button" onClick={() => setLines(l => [...l, { ...EMPTY_LINE }])} className="mt-2 text-xs flex items-center gap-1 text-primary hover:underline"><Plus className="h-3.5 w-3.5" /> Add Line</button>
            <div className="text-right text-sm font-semibold mt-2 border-t pt-2">
              Total: {lines.reduce((s, l) => s + Number(l.quantity) * Number(l.unitPrice), 0).toLocaleString()}
            </div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={createMut.isPending} className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
              <Check className="h-4 w-4" />{createMut.isPending ? t("saving") : "Create Invoice"}
            </button>
            <button type="button" onClick={resetForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}

      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Invoice #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Supplier</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Total</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Balance</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {(data ?? []).map((inv: any) => {
                const st = STATUS[inv.status] ?? { label: "?", cls: "bg-muted" };
                return (
                  <tr key={inv.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{inv.invoiceNumber}</td>
                    <td className="p-3 font-medium">{inv.supplierName}</td>
                    <td className="p-3 text-muted-foreground">{inv.invoiceDate}</td>
                    <td className="p-3 text-right tabular-nums">{inv.totalAmount?.toLocaleString()}</td>
                    <td className="p-3 text-right tabular-nums text-red-600">{inv.balanceDue > 0 ? inv.balanceDue.toLocaleString() : "—"}</td>
                    <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full font-medium ${st.cls}`}>{st.label}</span></td>
                    <td className="p-3 text-right">
                      {inv.status === 1 && (
                        <button onClick={() => postMut.mutate(inv.id)} disabled={postMut.isPending}
                          className="flex items-center gap-1 text-xs px-2 py-1.5 rounded bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50">
                          <Send className="h-3.5 w-3.5" /> Post
                        </button>
                      )}
                      {inv.status === 2 && <CheckCircle2 className="h-4 w-4 text-emerald-500 ml-auto" />}
                    </td>
                  </tr>
                );
              })}
              {(data ?? []).length === 0 && <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">No supplier invoices yet.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
