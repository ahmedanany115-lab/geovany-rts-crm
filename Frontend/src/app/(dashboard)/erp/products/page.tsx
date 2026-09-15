"use client";
import { useState } from "react";
import { useProducts, useCreateProduct, useUpdateProduct, useDeleteProduct, useToggleProductStatus } from "@/features/erp/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { Package, Plus, RefreshCw, PowerOff, Search, Pencil, Trash2, X, Check, AlertTriangle } from "lucide-react";
import type { ProductDto } from "@/features/erp/types";

const INIT = { sku: "", name: "", nameAr: "", category: "", unit: "Piece", purchasePrice: 0, salesPrice: 0, minimumStock: 0, currencyId: "" };

export default function ProductsPage() {
  const { t } = useT();
  const { toast } = useToast();
  const [search, setSearch]       = useState("");
  const [showForm, setShowForm]   = useState(false);
  const [editing, setEditing]     = useState<ProductDto | null>(null);
  const [deleteTarget, setDelete] = useState<ProductDto | null>(null);
  const [form, setForm]           = useState(INIT);
  const { data, isLoading, refetch } = useProducts({ search: search || undefined });
  const { data: currencies } = useCurrencies();
  const create = useCreateProduct();
  const update = useUpdateProduct();
  const del    = useDeleteProduct();
  const toggle = useToggleProductStatus();

  const openEdit = (p: ProductDto) => {
    setEditing(p);
    setForm({ sku: p.sku, name: p.name, nameAr: (p as any).nameAr ?? "", category: p.category ?? "",
              unit: p.unit, purchasePrice: p.purchasePrice, salesPrice: p.salesPrice,
              minimumStock: p.minimumStock, currencyId: (p as any).currencyId ?? "" });
    setShowForm(true);
  };
  const closeForm = () => { setShowForm(false); setEditing(null); setForm(INIT); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload = { ...form, purchasePrice: Number(form.purchasePrice), salesPrice: Number(form.salesPrice),
                      minimumStock: Number(form.minimumStock), currencyId: form.currencyId || currencies?.[0]?.id || "" };
    try {
      if (editing) { await update.mutateAsync({ id: editing.id, data: payload }); toast(`"${form.name}" updated.`, "success"); }
      else         { await create.mutateAsync(payload); toast(`Product "${form.name}" created.`, "success"); }
      closeForm();
    } catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try { await del.mutateAsync(deleteTarget.id); toast(`"${deleteTarget.name}" deleted.`, "info"); setDelete(null); }
    catch (err: any) { toast(err?.message ?? "Failed to delete — deactivate instead.", "error"); setDelete(null); }
  };

  const handleToggle = async (id: string, name: string, active: boolean) => {
    try { await toggle.mutateAsync(id); toast(`"${name}" ${active ? "deactivated" : "activated"}.`, "success"); }
    catch { toast("Could not update status.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Package className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("products")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={showForm ? closeForm : () => setShowForm(true)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"><Plus className="h-4 w-4" />{showForm ? t("cancel") : t("add")}</button>
        </div>
      </div>
      {/* Delete confirm */}
      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <div className="flex items-center gap-3 text-red-600"><AlertTriangle className="h-5 w-5" /><h2 className="font-semibold">{t("confirm_delete")}</h2></div>
            <p className="text-sm text-muted-foreground">Delete <strong>{deleteTarget.name}</strong>? Products with inventory history cannot be deleted — they will be deactivated instead.</p>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setDelete(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={handleDelete} disabled={del.isPending} className="px-4 py-2 rounded-lg text-sm bg-red-600 text-white hover:bg-red-700 disabled:opacity-50">{del.isPending ? "Deleting…" : "Delete"}</button>
            </div>
          </div>
        </div>
      )}
      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">{editing ? `Edit: ${editing.name}` : "New Product"}</h2>
          <h2 className="font-semibold text-sm">New Product</h2>
          <div className="grid grid-cols-3 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">SKU *</label><input required value={form.sku} onChange={e => setForm(f => ({ ...f, sku: e.target.value }))} className="input w-full font-mono" /></div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">{t("name")} *</label><input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Category</label><input value={form.category} onChange={e => setForm(f => ({ ...f, category: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Unit</label><input value={form.unit} onChange={e => setForm(f => ({ ...f, unit: e.target.value }))} className="input w-full" placeholder="Piece / Box / Kg" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("currency")}</label>
              <select value={form.currencyId} onChange={e => setForm(f => ({ ...f, currencyId: e.target.value }))} className="input w-full">
                {currencies?.map(c => <option key={c.id} value={c.id}>{c.code} — {c.name}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Purchase {t("price")}</label><input type="number" step="0.01" min="0" value={form.purchasePrice} onChange={e => setForm(f => ({ ...f, purchasePrice: Number(e.target.value) }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Sales {t("price")}</label><input type="number" step="0.01" min="0" value={form.salesPrice} onChange={e => setForm(f => ({ ...f, salesPrice: Number(e.target.value) }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Min Stock</label><input type="number" step="0.01" min="0" value={form.minimumStock} onChange={e => setForm(f => ({ ...f, minimumStock: Number(e.target.value) }))} className="input w-full" /></div>
          </div>
          <button type="submit" disabled={create.isPending} className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50">{create.isPending ? t("saving") : t("save")}</button>
        </form>
      )}
      <div className="relative max-w-sm"><Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" /><input value={search} onChange={e => setSearch(e.target.value)} placeholder={t("search") + "..."} className="input pl-10 w-full" /></div>
      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">SKU</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Category</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Unit</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Purchase</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Sales</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(p => (
                <tr key={p.id} className="hover:bg-muted/20">
                  <td className="p-3 font-mono text-xs">{p.sku}</td>
                  <td className="p-3 font-medium">{p.name}</td>
                  <td className="p-3 text-muted-foreground">{p.category ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">{p.unit}</td>
                  <td className="p-3 text-right tabular-nums">{p.purchasePrice.toLocaleString()}</td>
                  <td className="p-3 text-right tabular-nums">{p.salesPrice.toLocaleString()}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${p.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>{p.isActive ? t("active") : t("inactive")}</span></td>
                  <td className="p-3 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <button onClick={() => openEdit(p)} title={t("edit")} className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground"><Pencil className="h-3.5 w-3.5" /></button>
                      <button onClick={() => handleToggle(p.id, p.name, p.isActive)} title={p.isActive ? t("deactivate") : t("activate")} className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground"><PowerOff className="h-3.5 w-3.5" /></button>
                      <button onClick={() => setDelete(p)} title={t("delete")} className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600"><Trash2 className="h-3.5 w-3.5" /></button>
                    </div>
                  </td>
                </tr>
              ))}
              {!data?.length && <tr><td colSpan={8} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
