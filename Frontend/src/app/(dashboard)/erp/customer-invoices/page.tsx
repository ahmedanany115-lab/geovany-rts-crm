"use client";
import { useState } from "react";
import { FileText, RefreshCw, Send, CheckCircle2, Plus, X, Trash2 } from "lucide-react";
import { useCustomerInvoices, usePostCustomerInvoice, useCreateCustomerInvoice, useCustomers, useProducts } from "@/features/erp/hooks";
import { InvoiceStatusLabels } from "@/features/erp/types";

const STATUS_COLORS: Record<number, string> = {
  1: "bg-muted text-muted-foreground",
  2: "bg-blue-100 text-blue-700",
  3: "bg-amber-100 text-amber-700",
  4: "bg-emerald-100 text-emerald-700",
  5: "bg-red-100 text-red-700",
};

interface InvLine {
  productId: string; description: string;
  quantity: number; unitPrice: number; discountPercent: number; taxRate: number;
  lineTotal: number; taxAmount: number; netAmount: number;
}

function calc(qty: number, price: number, discPct: number, taxRate: number) {
  const lineTotal = qty * price;
  const taxAmount = lineTotal * taxRate;      // VAT 14% on gross BEFORE discount
  const discountAmount = lineTotal * (discPct / 100);
  return { lineTotal, taxAmount, netAmount: lineTotal - discountAmount + taxAmount };
}

export default function CustomerInvoicesPage() {
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [showForm, setShowForm] = useState(false);
  const today = new Date().toISOString().split("T")[0];
  const dueDate = new Date(Date.now() + 30 * 86400000).toISOString().split("T")[0];

  const { data: invoices, isLoading, refetch } = useCustomerInvoices({ status: statusFilter });
  const post = usePostCustomerInvoice();
  const createInv = useCreateCustomerInvoice();
  const { data: customers } = useCustomers({ isActive: true });
  const { data: products } = useProducts({ isActive: true });

  const [form, setForm] = useState({ customerId: "", invoiceDate: today, dueDate, notes: "" });
  const [lines, setLines] = useState<InvLine[]>([]);

  const addLine = () => setLines(l => [...l, { productId: "", description: "", quantity: 1, unitPrice: 0, discountPercent: 0, taxRate: 0.14, lineTotal: 0, taxAmount: 0, netAmount: 0 }]);
  const removeLine = (i: number) => setLines(l => l.filter((_, idx) => idx !== i));
  const updateLine = (i: number, field: string, value: string | number) => {
    setLines(prev => prev.map((line, idx) => {
      if (idx !== i) return line;
      let u = { ...line, [field]: value };
      if (field === "productId") {
        const p = products?.find(p => p.id === value);
        if (p) u = { ...u, description: p.name, unitPrice: p.salesPrice, taxRate: p.taxRatePercent / 100 };
      }
      return { ...u, ...calc(Number(u.quantity), Number(u.unitPrice), Number(u.discountPercent), Number(u.taxRate)) };
    }));
  };

  const subTotal = lines.reduce((s, l) => s + l.lineTotal, 0);
  const taxTotal = lines.reduce((s, l) => s + l.taxAmount, 0);
  const discTotal = lines.reduce((s, l) => s + l.lineTotal * (l.discountPercent / 100), 0);
  const invTotal = subTotal - discTotal + taxTotal;
  const commissionAmount = invTotal * 0.015;

  const totalOutstanding = invoices?.reduce((s, i) => s + i.balanceDue, 0) ?? 0;
  const totalVat = invoices?.filter(i => i.status >= 2).reduce((s, i) => s + i.taxAmount, 0) ?? 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!lines.length) return alert("Add at least one line.");
    await createInv.mutateAsync({
      customerId: form.customerId,
      invoiceDate: form.invoiceDate,
      dueDate: form.dueDate,
      currencyId: "00000000-0000-0000-0000-000000000001",
      exchangeRate: 1,
      lines: lines.map(l => ({ productId: l.productId, description: l.description, quantity: l.quantity, unitPrice: l.unitPrice, discountPercent: l.discountPercent, taxRate: l.taxRate })),
    });
    setLines([]);
    setForm({ customerId: "", invoiceDate: today, dueDate, notes: "" });
    setShowForm(false);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><FileText className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Customer Invoices</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? "Cancel" : "New Invoice"}
          </button>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-4">
        <div className="card p-4"><p className="text-sm text-muted-foreground">Total Invoiced (Posted)</p><p className="text-2xl font-bold mt-1">{invoices?.filter(i => i.status >= 2).reduce((s, i) => s + i.totalAmount, 0).toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p></div>
        <div className="card p-4"><p className="text-sm text-muted-foreground">Outstanding Balance (AR)</p><p className="text-2xl font-bold mt-1 text-amber-600">{totalOutstanding.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p></div>
        <div className="card p-4"><p className="text-sm text-muted-foreground">Output VAT Collected</p><p className="text-2xl font-bold mt-1 text-blue-600">{totalVat.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p></div>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-5 border border-primary/20">
          <h2 className="font-semibold text-lg">New Customer Invoice</h2>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <div><label className="text-sm text-muted-foreground block mb-1">Customer *</label>
              <select required value={form.customerId} onChange={e => setForm(f => ({ ...f, customerId: e.target.value }))} className="input w-full">
                <option value="">Select customer...</option>
                {customers?.map(c => <option key={c.id} value={c.id}>{c.code} – {c.name}</option>)}
              </select></div>
            <div><label className="text-sm text-muted-foreground block mb-1">Invoice Date</label>
              <input type="date" value={form.invoiceDate} onChange={e => setForm(f => ({ ...f, invoiceDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-sm text-muted-foreground block mb-1">Due Date</label>
              <input type="date" value={form.dueDate} onChange={e => setForm(f => ({ ...f, dueDate: e.target.value }))} className="input w-full" /></div>
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <h3 className="font-medium text-sm">Invoice Lines</h3>
              <button type="button" onClick={addLine} className="btn-ghost text-xs px-3 py-1.5 rounded-lg flex items-center gap-1"><Plus className="h-3 w-3" /> Add Line</button>
            </div>
            {lines.length === 0 && <div className="text-center py-6 border border-dashed border-border rounded-lg text-muted-foreground text-sm">Click "Add Line" to add products</div>}
            {lines.map((line, i) => (
              <div key={i} className="grid grid-cols-12 gap-2 items-end p-3 bg-muted/20 rounded-lg">
                <div className="col-span-3">
                  {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Product</label>}
                  <select value={line.productId} onChange={e => updateLine(i, "productId", e.target.value)} className="input w-full text-sm" required>
                    <option value="">Select...</option>
                    {products?.map(p => <option key={p.id} value={p.id}>{p.sku} – {p.name}</option>)}
                  </select>
                </div>
                <div className="col-span-1">
                  {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Qty</label>}
                  <input type="number" min="0.01" step="0.01" value={line.quantity} onChange={e => updateLine(i, "quantity", parseFloat(e.target.value) || 0)} className="input w-full text-sm" />
                </div>
                <div className="col-span-2">
                  {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Unit Price</label>}
                  <input type="number" min="0" step="0.01" value={line.unitPrice} onChange={e => updateLine(i, "unitPrice", parseFloat(e.target.value) || 0)} className="input w-full text-sm" />
                </div>
                <div className="col-span-1">
                  {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Disc%</label>}
                  <input type="number" min="0" max="100" step="0.1" value={line.discountPercent} onChange={e => updateLine(i, "discountPercent", parseFloat(e.target.value) || 0)} className="input w-full text-sm" />
                </div>
                <div className="col-span-1">
                  {i === 0 && <label className="text-xs text-muted-foreground block mb-1">VAT%</label>}
                  <div className="input w-full text-sm bg-muted/30 text-muted-foreground">{(line.taxRate * 100).toFixed(0)}%</div>
                </div>
                <div className="col-span-2">
                  {i === 0 && <label className="text-xs text-muted-foreground block mb-1">VAT Amt</label>}
                  <div className="input w-full text-sm bg-muted/30 text-muted-foreground tabular-nums">{line.taxAmount.toFixed(2)}</div>
                </div>
                <div className="col-span-2">
                  {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Net Total</label>}
                  <div className="input w-full text-sm font-medium tabular-nums bg-muted/30">{line.netAmount.toFixed(2)}</div>
                </div>
                <div className="col-span-0 flex items-end pb-0.5">
                  <button type="button" onClick={() => removeLine(i)} className="p-2 text-red-500 hover:text-red-700"><Trash2 className="h-4 w-4" /></button>
                </div>
              </div>
            ))}
            {lines.length > 0 && (
              <div className="flex justify-end">
                <div className="w-80 space-y-1.5 p-4 bg-muted/30 rounded-lg text-sm border border-border">
                  <div className="flex justify-between text-muted-foreground"><span>Sub-total (gross)</span><span className="tabular-nums">{subTotal.toFixed(2)} EGP</span></div>
                  <div className="flex justify-between text-muted-foreground"><span>Discount</span><span className="tabular-nums text-red-500">−{discTotal.toFixed(2)} EGP</span></div>
                  <div className="flex justify-between text-blue-600"><span>VAT 14% (on gross before discount)</span><span className="tabular-nums">{taxTotal.toFixed(2)} EGP</span></div>
                  <div className="flex justify-between font-bold text-base border-t border-border pt-1.5 mt-1"><span>Invoice Total</span><span className="tabular-nums">{invTotal.toFixed(2)} EGP</span></div>
                  <div className="flex justify-between text-xs text-purple-600 pt-1 border-t border-dashed border-border"><span>Sales Commission (1.5% — auto on post)</span><span className="tabular-nums">{commissionAmount.toFixed(2)} EGP</span></div>
                </div>
              </div>
            )}
          </div>

          <div className="flex gap-2 pt-2">
            <button type="submit" disabled={createInv.isPending || !lines.length} className="btn-primary px-5 py-2 rounded-lg text-sm">
              {createInv.isPending ? "Saving..." : "Create Invoice (Draft)"}
            </button>
            <button type="button" onClick={() => { setShowForm(false); setLines([]); }} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
          </div>
          <p className="text-xs text-muted-foreground">After creating, click "Post" to record the journal entry (Dr AR / Cr Revenue / Cr Output VAT) and auto-generate the sales commission.</p>
        </form>
      )}

      <div className="flex gap-2 flex-wrap">
        {[undefined, 1, 2, 3, 4].map(s => (
          <button key={String(s)} onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${statusFilter === s ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/80"}`}>
            {s === undefined ? "All" : InvoiceStatusLabels[s]}
          </button>
        ))}
      </div>

      {isLoading ? <div className="text-center py-10 text-muted-foreground">Loading invoices...</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Invoice #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Due</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Total</th>
              <th className="text-right p-3 font-medium text-muted-foreground">VAT</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Balance Due</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
              <th className="p-3 w-20"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {invoices?.map(i => (
                <tr key={i.id} className="hover:bg-muted/20 transition-colors">
                  <td className="p-3 font-mono text-xs">{i.invoiceNumber}</td>
                  <td className="p-3 font-medium">{i.customerName}</td>
                  <td className="p-3 text-muted-foreground">{i.invoiceDate}</td>
                  <td className="p-3 text-muted-foreground">{i.dueDate}</td>
                  <td className="p-3 text-right tabular-nums">{i.totalAmount.toLocaleString("en-EG", { style: "currency", currency: i.currencyCode || "EGP" })}</td>
                  <td className="p-3 text-right tabular-nums text-blue-600">{i.taxAmount.toLocaleString("en-EG", { style: "currency", currency: i.currencyCode || "EGP" })}</td>
                  <td className={`p-3 text-right tabular-nums font-medium ${i.balanceDue > 0 ? "text-amber-600" : "text-emerald-600"}`}>
                    {i.balanceDue.toLocaleString("en-EG", { style: "currency", currency: i.currencyCode || "EGP" })}
                  </td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${STATUS_COLORS[i.status]}`}>{i.statusName}</span></td>
                  <td className="p-3">
                    {i.status === 1 && (
                      <button onClick={() => post.mutate(i.id)} disabled={post.isPending} className="text-blue-600 hover:text-blue-700 flex items-center gap-1 text-xs" title="Post — creates Dr AR / Cr Revenue / Cr VAT journal entry">
                        <Send className="h-3.5 w-3.5" /> Post
                      </button>
                    )}
                    {i.status >= 2 && <CheckCircle2 className="h-4 w-4 text-emerald-500" />}
                  </td>
                </tr>
              ))}
              {!invoices?.length && <tr><td colSpan={9} className="p-8 text-center text-muted-foreground">No invoices found. Create your first invoice.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
