"use client";
import { useState } from "react";
import { useProducts, useCreateProduct, useToggleProductStatus } from "@/features/erp/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { Package, Plus, RefreshCw, PowerOff, Search } from "lucide-react";

const INIT = { sku: "", name: "", category: "", unit: "Piece", purchasePrice: 0, salesPrice: 0, minimumStock: 0, currencyId: "" };

export default function ProductsPage() {
  const { t } = useT();
  const { toast } = useToast();
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState(INIT);
  const { data, isLoading, refetch } = useProducts({ search: search || undefined });
  const { data: currencies } = useCurrencies();
  const create = useCreateProduct();
  const toggle = useToggleProductStatus();

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await create.mutateAsync({
        ...form,
        purchasePrice: Number(form.purchasePrice),
        salesPrice: Number(form.salesPrice),
        minimumStock: Number(form.minimumStock),
        currencyId: form.currencyId || currencies?.[0]?.id || "",
      });
      toast(`Product "${form.name}" created.`, "success");
      setForm(INIT);
      setShowForm(false);
    } catch (err: any) {
      toast(err?.message ?? "Failed to create product.", "error");
    }
  };

  const handleToggle = async (id: string, name: string, active: boolean) => {
    try {
      await toggle.mutateAsync(id);
      toast(`"${name}" ${active ? "deactivated" : "activated"}.`, "success");
    } catch { toast("Could not update status.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Package className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("products")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"><Plus className="h-4 w-4" />{showForm ? t("cancel") : t("add")}</button>
        </div>
      </div>
      {showForm && (
        <form onSubmit={handleCreate} className="card p-5 space-y-4 border border-primary/20">
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
                  <td className="p-3 text-right"><button onClick={() => handleToggle(p.id, p.name, p.isActive)} className="text-muted-foreground hover:text-foreground"><PowerOff className="h-4 w-4" /></button></td>
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
