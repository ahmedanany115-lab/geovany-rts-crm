"use client";
import { useState } from "react";
import { ShoppingBag, RefreshCw, CheckCircle, Plus, X, Trash2, ChevronDown, ChevronUp } from "lucide-react";
import { usePurchaseOrders, useApprovePurchaseOrder, useSuppliers, useProducts, useWarehouses } from "@/features/erp/hooks";
import { PurchaseOrderStatusLabels } from "@/features/erp/types";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useEgpCurrencyId } from "@/features/erp/hooks/useCurrency";

const STATUS_COLORS: Record<number, string> = {
  1: "bg-muted text-muted-foreground",
  2: "bg-blue-100 text-blue-700",
  3: "bg-amber-100 text-amber-700",
  4: "bg-emerald-100 text-emerald-700",
  5: "bg-red-100 text-red-700",
};

interface POLine {
  productId: string; productName: string;
  quantity: number; unitPrice: number; discountPercent: number; taxRate: number;
  lineTotal: number; taxAmount: number; netAmount: number;
}

function calcLine(qty: number, price: number, discPct: number, taxRate: number) {
  const lineTotal = qty * price;
  const taxAmount = lineTotal * taxRate;       // VAT on GROSS before discount
  const discountAmount = lineTotal * (discPct / 100);
  return { lineTotal, taxAmount, netAmount: lineTotal - discountAmount + taxAmount };
}

export default function PurchaseOrdersPage() {
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [showForm, setShowForm] = useState(false);
  const [expanded, setExpanded] = useState<string | null>(null);
  const today = new Date().toISOString().split("T")[0];

  const { data: orders, isLoading, refetch } = usePurchaseOrders({ status: statusFilter });
  const approve = useApprovePurchaseOrder();
  const { data: suppliers } = useSuppliers({ isActive: true });
  const { data: products } = useProducts({ isActive: true });
  const { data: warehouses } = useWarehouses({ isActive: true });
  const egpId = useEgpCurrencyId();

  const qc = useQueryClient();
  const createPO = useMutation({
    mutationFn: (data: unknown) => apiFetch<{ id: string }>("/purchaseorders", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["purchase-orders"] }); setShowForm(false); setLines([]); setForm({ supplierId: "", warehouseId: "", orderDate: today, notes: "" }); },
  });

  const [form, setForm] = useState({ supplierId: "", warehouseId: "", orderDate: today, notes: "" });
  const [lines, setLines] = useState<POLine[]>([]);

  const addLine = () => setLines(l => [...l, { productId: "", productName: "", quantity: 1, unitPrice: 0, discountPercent: 0, taxRate: 0.14, lineTotal: 0, taxAmount: 0, netAmount: 0 }]);
  const removeLine = (i: number) => setLines(l => l.filter((_, idx) => idx !== i));

  const updateLine = (i: number, field: string, value: string | number) => {
    setLines(prev => prev.map((line, idx) => {
      if (idx !== i) return line;
      let u = { ...line, [field]: value };
      if (field === "productId") {
        const p = products?.find(p => p.id === value);
        if (p) u = { ...u, productName: p.name, unitPrice: p.purchasePrice, taxRate: p.taxRatePercent / 100 };
      }
      return { ...u, ...calcLine(Number(u.quantity), Number(u.unitPrice), Number(u.discountPercent), Number(u.taxRate)) };
    }));
  };

  const subTotal = lines.reduce((s, l) => s + l.lineTotal, 0);
  const taxTotal = lines.reduce((s, l) => s + l.taxAmount, 0);
  const discTotal = lines.reduce((s, l) => s + l.lineTotal * (l.discountPercent / 100), 0);
  const orderTotal = subTotal - discTotal + taxTotal;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!lines.length) return alert("Add at least one line.");
    await createPO.mutateAsync({
      supplierId: form.supplierId, warehouseId: form.warehouseId,
      orderDate: form.orderDate, currencyId: egpId || "00000000-0000-0000-0000-000000000001",
      exchangeRate: 1, notes: form.notes,
      lines: lines.map(l => ({ productId: l.productId, quantity: l.quantity, unitPrice: l.unitPrice, discountPercent: l.discountPercent })),
    });
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><ShoppingBag className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Purchase Orders</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? "Cancel" : "New PO"}
          </button>
        </div>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-5 border border-primary/20">
          <h2 className="font-semibold text-lg">New Purchase Order</h2>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <div><label className="text-sm text-muted-foreground block mb-1">Supplier *</label>
              <select required value={form.supplierId} onChange={e => setForm(f => ({ ...f, supplierId: e.target.value }))} className="input w-full">
                <option value="">Select supplier...</option>
                {suppliers?.map(s => <option key={s.id} value={s.id}>{s.code} – {s.name}</option>)}
              </select></div>
            <div><label className="text-sm text-muted-foreground block mb-1">Warehouse *</label>
              <select required value={form.warehouseId} onChange={e => setForm(f => ({ ...f, warehouseId: e.target.value }))} className="input w-full">
                <option value="">Select warehouse...</option>
                {warehouses?.map(w => <option key={w.id} value={w.id}>{w.code} – {w.name}</option>)}
              </select></div>
            <div><label className="text-sm text-muted-foreground block mb-1">Order Date *</label>
              <input type="date" required value={form.orderDate} onChange={e => setForm(f => ({ ...f, orderDate: e.target.value }))} className="input w-full" /></div>
            <div className="md:col-span-3"><label className="text-sm text-muted-foreground block mb-1">Notes</label>
              <input value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" /></div>
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <h3 className="font-medium text-sm">Order Lines</h3>
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
                  {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Unit Cost</label>}
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
                <div className="w-72 space-y-1.5 p-3 bg-muted/30 rounded-lg text-sm">
                  <div className="flex justify-between text-muted-foreground"><span>Sub-total (gross)</span><span className="tabular-nums">{subTotal.toFixed(2)} EGP</span></div>
                  <div className="flex justify-between text-muted-foreground"><span>Discount</span><span className="tabular-nums text-red-500">−{discTotal.toFixed(2)} EGP</span></div>
                  <div className="flex justify-between text-muted-foreground"><span>VAT 14% (on gross)</span><span className="tabular-nums">{taxTotal.toFixed(2)} EGP</span></div>
                  <div className="flex justify-between font-semibold text-base border-t border-border pt-1.5 mt-1"><span>Order Total</span><span className="tabular-nums">{orderTotal.toFixed(2)} EGP</span></div>
                </div>
              </div>
            )}
          </div>

          <div className="flex gap-2 pt-2">
            <button type="submit" disabled={createPO.isPending || !lines.length} className="btn-primary px-5 py-2 rounded-lg text-sm">
              {createPO.isPending ? "Saving..." : "Create Purchase Order"}
            </button>
            <button type="button" onClick={() => { setShowForm(false); setLines([]); }} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
          </div>
          {createPO.isError && <p className="text-sm text-red-600">Error creating order. Check all required fields.</p>}
        </form>
      )}

      <div className="flex gap-2 flex-wrap">
        {[undefined, 1, 2, 3, 4, 5].map(s => (
          <button key={String(s)} onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${statusFilter === s ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/80"}`}>
            {s === undefined ? "All" : PurchaseOrderStatusLabels[s]}
          </button>
        ))}
      </div>

      {isLoading ? <div className="text-center py-10 text-muted-foreground">Loading purchase orders...</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground w-8"></th>
              <th className="text-left p-3 font-medium text-muted-foreground">PO #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Supplier</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Warehouse</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Sub-total</th>
              <th className="text-right p-3 font-medium text-muted-foreground">VAT</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Total</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {orders?.map(o => (
                <>
                  <tr key={o.id} className="hover:bg-muted/20 transition-colors">
                    <td className="p-3">
                      {o.lines?.length > 0 && (
                        <button onClick={() => setExpanded(expanded === o.id ? null : o.id)} className="text-muted-foreground">
                          {expanded === o.id ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                        </button>
                      )}
                    </td>
                    <td className="p-3 font-mono text-xs">{o.poNumber}</td>
                    <td className="p-3 font-medium">{o.supplierName}</td>
                    <td className="p-3 text-muted-foreground">{o.orderDate}</td>
                    <td className="p-3 text-muted-foreground">{o.warehouseName}</td>
                    <td className="p-3 text-right tabular-nums">{o.subTotal.toLocaleString()}</td>
                    <td className="p-3 text-right tabular-nums text-muted-foreground">{o.taxAmount.toLocaleString()}</td>
                    <td className="p-3 text-right tabular-nums font-medium">{o.totalAmount.toLocaleString("en-EG", { style: "currency", currency: o.currencyCode || "EGP" })}</td>
                    <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${STATUS_COLORS[o.status]}`}>{o.statusName}</span></td>
                    <td className="p-3">
                      {o.status === 1 && (
                        <button onClick={() => approve.mutate(o.id)} disabled={approve.isPending} className="text-blue-600 hover:text-blue-700" title="Approve">
                          <CheckCircle className="h-4 w-4" />
                        </button>
                      )}
                    </td>
                  </tr>
                  {expanded === o.id && o.lines?.map((l, li) => (
                    <tr key={`${o.id}-${li}`} className="bg-muted/10 text-xs">
                      <td></td>
                      <td className="px-3 py-1.5 text-muted-foreground" colSpan={2}>{l.productSKU} – {l.productName}</td>
                      <td className="px-3 py-1.5 text-muted-foreground">{l.quantity} × {l.unitPrice.toFixed(2)}</td>
                      <td className="px-3 py-1.5 text-muted-foreground">Rcvd: {l.receivedQuantity}</td>
                      <td className="px-3 py-1.5 text-right tabular-nums text-muted-foreground" colSpan={2}>Disc: {l.discountPercent}% | VAT: {l.taxAmount.toFixed(2)}</td>
                      <td className="px-3 py-1.5 text-right tabular-nums font-medium">{l.netAmount.toFixed(2)}</td>
                      <td colSpan={2}></td>
                    </tr>
                  ))}
                </>
              ))}
              {!orders?.length && <tr><td colSpan={10} className="p-8 text-center text-muted-foreground">No purchase orders found</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
