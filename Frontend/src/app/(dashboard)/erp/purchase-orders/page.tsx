"use client";
import { useState } from "react";
import {
  usePurchaseOrders, useApprovePurchaseOrder, useCreatePurchaseOrder,
  useSuppliers, useWarehouses, useProducts,
} from "@/features/erp/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { ShoppingBag, RefreshCw, CheckCircle, Plus, X, Check, Trash2 } from "lucide-react";

const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Draft",     cls: "bg-muted text-muted-foreground" },
  2: { label: "Confirmed", cls: "bg-blue-100 text-blue-700" },
  3: { label: "Received",  cls: "bg-emerald-100 text-emerald-700" },
  4: { label: "Invoiced",  cls: "bg-purple-100 text-purple-700" },
  5: { label: "Cancelled", cls: "bg-red-100 text-red-700" },
};

const EMPTY_LINE = { productId: "", quantity: 1, unitPrice: 0, discountPercent: 0 };
const INIT = { supplierId:"", orderDate:new Date().toISOString().split("T")[0], currencyId:"", warehouseId:"", notes:"" };

export default function PurchaseOrdersPage() {
  const { t } = useT();
  const { toast } = useToast();
  const { data, isLoading, refetch } = usePurchaseOrders({});
  const approve  = useApprovePurchaseOrder();
  const create   = useCreatePurchaseOrder();
  const { data: suppliers  } = useSuppliers({});
  const { data: warehouses } = useWarehouses();
  const { data: products   } = useProducts({});
  const { data: currencies } = useCurrencies();

  const [showForm, setShowForm] = useState(false);
  const [form, setForm]         = useState(INIT);
  const [lines, setLines]       = useState([{ ...EMPTY_LINE }]);
  const [formError, setFormError] = useState<string | null>(null);

  const setLine = (i: number, field: string, val: string | number) =>
    setLines(prev => prev.map((l, idx) => idx === i ? { ...l, [field]: val } : l));

  const pickProduct = (i: number, pid: string) => {
    const p = products?.find((x: any) => x.id === pid);
    setLines(prev => prev.map((l, idx) => idx === i ? { ...l, productId: pid, unitPrice: (p as any)?.purchasePrice ?? 0 } : l));
  };

  const resetForm = () => { setForm(INIT); setLines([{ ...EMPTY_LINE }]); setFormError(null); setShowForm(false); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setFormError(null);
    if (!form.supplierId)  { setFormError("Select a supplier."); return; }
    if (!form.currencyId)  { setFormError("Select a currency."); return; }
    if (!form.warehouseId) { setFormError("Select a warehouse."); return; }
    if (lines.some(l => !l.productId)) { setFormError("All lines need a product."); return; }
    try {
      await create.mutateAsync({ ...form, exchangeRate: 1,
        lines: lines.map((l, i) => ({ ...l, quantity: Number(l.quantity), unitPrice: Number(l.unitPrice), discountPercent: Number(l.discountPercent), sortOrder: i + 1 })) });
      toast("Purchase Order created.", "success");
      resetForm();
    } catch (err: any) { const msg = err?.message ?? "Failed."; setFormError(msg); toast(msg, "error"); }
  };

  const handleApprove = async (id: string, num: string) => {
    try { await approve.mutateAsync(id); toast(`PO ${num} confirmed.`, "success"); }
    catch (err: any) { toast(err?.message ?? "Action failed.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><ShoppingBag className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("purchase_orders")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={showForm ? resetForm : () => setShowForm(true)}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : "New PO"}
          </button>
        </div>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">New Purchase Order</h2>
          {formError && <div className="rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700">{formError}</div>}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Supplier *</label>
              <select required value={form.supplierId} onChange={e => setForm(f => ({ ...f, supplierId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {suppliers?.filter((s: any) => s.isActive).map((s: any) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Date *</label>
              <input type="date" value={form.orderDate} onChange={e => setForm(f => ({ ...f, orderDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Currency *</label>
              <select required value={form.currencyId} onChange={e => setForm(f => ({ ...f, currencyId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {currencies?.filter(c => c.isActive).map(c => <option key={c.id} value={c.id}>{c.code}</option>)}
              </select>
            </div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Warehouse *</label>
              <select required value={form.warehouseId} onChange={e => setForm(f => ({ ...f, warehouseId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {warehouses?.filter((w: any) => w.isActive).map((w: any) => <option key={w.id} value={w.id}>{w.name}</option>)}
              </select>
            </div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Notes</label>
              <input value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" /></div>
          </div>
          {/* Lines */}
          <div>
            <div className="grid grid-cols-12 gap-2 text-xs font-medium text-muted-foreground pb-1 border-b">
              <span className="col-span-5">Product</span><span className="col-span-2">Qty</span>
              <span className="col-span-2">Unit Price</span><span className="col-span-2">Disc%</span><span className="col-span-1"></span>
            </div>
            <div className="space-y-1.5 mt-2">
              {lines.map((l, i) => (
                <div key={i} className="grid grid-cols-12 gap-2 items-center">
                  <div className="col-span-5"><select value={l.productId} onChange={e => pickProduct(i, e.target.value)} className="input w-full text-sm py-1.5">
                    <option value="">Select product…</option>
                    {products?.filter((p: any) => p.isActive).map((p: any) => <option key={p.id} value={p.id}>{p.sku} — {p.name}</option>)}
                  </select></div>
                  <div className="col-span-2"><input type="number" min="0.01" step="0.01" value={l.quantity} onChange={e => setLine(i, "quantity", e.target.value)} className="input w-full text-sm py-1.5" /></div>
                  <div className="col-span-2"><input type="number" min="0" step="0.01" value={l.unitPrice} onChange={e => setLine(i, "unitPrice", e.target.value)} className="input w-full text-sm py-1.5" /></div>
                  <div className="col-span-2"><input type="number" min="0" max="100" step="0.1" value={l.discountPercent} onChange={e => setLine(i, "discountPercent", e.target.value)} className="input w-full text-sm py-1.5" /></div>
                  <div className="col-span-1 flex justify-end">
                    <button type="button" onClick={() => setLines(p => p.length > 1 ? p.filter((_, idx) => idx !== i) : p)} disabled={lines.length <= 1}
                      className="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 disabled:opacity-20"><Trash2 className="h-3.5 w-3.5" /></button>
                  </div>
                </div>
              ))}
            </div>
            <button type="button" onClick={() => setLines(l => [...l, { ...EMPTY_LINE }])} className="mt-2 text-xs flex items-center gap-1 text-primary hover:underline"><Plus className="h-3.5 w-3.5" /> Add Line</button>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={create.isPending} className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
              <Check className="h-4 w-4" />{create.isPending ? t("saving") : "Create Draft PO"}
            </button>
            <button type="button" onClick={resetForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}

      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">PO #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Supplier</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Total</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(o => {
                const st = STATUS[o.status as keyof typeof STATUS] ?? { label: "?", cls: "bg-muted" };
                return (
                  <tr key={o.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{o.poNumber}</td>
                    <td className="p-3 font-semibold">{o.supplierName}</td>
                    <td className="p-3 text-muted-foreground">{o.orderDate?.toString()}</td>
                    <td className="p-3 text-right tabular-nums">{o.totalAmount.toLocaleString()}</td>
                    <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full font-medium ${st.cls}`}>{st.label}</span></td>
                    <td className="p-3 text-right">
                      {o.status === 1 && (
                        <button onClick={() => handleApprove(o.id, o.poNumber)} disabled={approve.isPending}
                          className="flex items-center gap-1 text-xs px-2 py-1.5 rounded bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50">
                          <CheckCircle className="h-3.5 w-3.5" /> Confirm
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
              {!data?.length && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
