"use client";
import { useState } from "react";
import { Package, Plus, RefreshCw, PowerOff, AlertTriangle, X } from "lucide-react";
import { useProducts, useCreateProduct, useToggleProductStatus } from "@/features/erp/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";

interface TaxRateDto { id: string; name: string; rate: number; code: string; isActive: boolean; }

function StockBadge({ qty, min }: { qty: number; min: number }) {
  const isLow = qty > 0 && qty <= min;
  const isZero = qty === 0;
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${
      isZero ? "bg-red-100 text-red-700" : isLow ? "bg-amber-100 text-amber-700" : "bg-emerald-100 text-emerald-700"
    }`}>
      {(isZero || isLow) && <AlertTriangle className="h-3 w-3" />}
      {qty.toLocaleString()}
    </span>
  );
}

const UNITS = ["Piece", "Box", "Kg", "Liter", "Meter", "Set", "Carton", "Pack"];

export default function ProductsPage() {
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({
    sku: "", name: "", description: "", category: "", unit: "Piece", barcode: "",
    purchasePrice: "0", salesPrice: "0", currencyId: "", taxRateId: "", minimumStock: "0",
  });

  const { data: products, isLoading, refetch } = useProducts({ search: search || undefined });
  const { data: currencies } = useCurrencies();
  const { data: taxRates } = useQuery({
    queryKey: ["tax-rates"],
    queryFn: () => apiFetch<TaxRateDto[]>("/taxrates"),
  });
  const create = useCreateProduct();
  const toggle = useToggleProductStatus();

  const egpId = currencies?.find(c => c.code === "EGP")?.id ?? "";
  const vatId = taxRates?.find(t => t.code === "VAT14" || t.name.includes("14"))?.id ?? "";

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    await create.mutateAsync({
      ...form,
      purchasePrice: parseFloat(form.purchasePrice) || 0,
      salesPrice: parseFloat(form.salesPrice) || 0,
      minimumStock: parseFloat(form.minimumStock) || 0,
      currencyId: form.currencyId || egpId,
      taxRateId: form.taxRateId || vatId || undefined,
    });
    setForm({ sku: "", name: "", description: "", category: "", unit: "Piece", barcode: "", purchasePrice: "0", salesPrice: "0", currencyId: "", taxRateId: "", minimumStock: "0" });
    setShowForm(false);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Package className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Products & Inventory</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? "Cancel" : "Add Product"}
          </button>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-4">
        <div className="card p-4"><p className="text-sm text-muted-foreground">Total Products</p><p className="text-2xl font-bold mt-1">{products?.length ?? 0}</p></div>
        <div className="card p-4"><p className="text-sm text-muted-foreground">Active</p><p className="text-2xl font-bold mt-1 text-emerald-600">{products?.filter(p => p.isActive).length ?? 0}</p></div>
        <div className="card p-4"><p className="text-sm text-muted-foreground">Low / Zero Stock</p><p className="text-2xl font-bold mt-1 text-amber-600">{products?.filter(p => p.totalQuantity <= p.minimumStock).length ?? 0}</p></div>
      </div>

      {showForm && (
        <form onSubmit={handleCreate} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold">New Product</h2>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">SKU *</label><input required value={form.sku} onChange={e => setForm(f => ({ ...f, sku: e.target.value }))} className="input w-full" placeholder="PROD-001" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Name *</label><input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" placeholder="Product Name" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Category</label><input value={form.category} onChange={e => setForm(f => ({ ...f, category: e.target.value }))} className="input w-full" placeholder="Electronics, Food..." /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Unit</label>
              <select value={form.unit} onChange={e => setForm(f => ({ ...f, unit: e.target.value }))} className="input w-full">
                {UNITS.map(u => <option key={u} value={u}>{u}</option>)}
              </select></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Purchase Price *</label><input type="number" min="0" step="0.01" required value={form.purchasePrice} onChange={e => setForm(f => ({ ...f, purchasePrice: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Sales Price *</label><input type="number" min="0" step="0.01" required value={form.salesPrice} onChange={e => setForm(f => ({ ...f, salesPrice: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Currency</label>
              <select value={form.currencyId || egpId} onChange={e => setForm(f => ({ ...f, currencyId: e.target.value }))} className="input w-full">
                {currencies?.map(c => <option key={c.id} value={c.id}>{c.code} — {c.name}</option>)}
              </select></div>
            <div><label className="text-xs text-muted-foreground block mb-1">VAT Rate</label>
              <select value={form.taxRateId || vatId} onChange={e => setForm(f => ({ ...f, taxRateId: e.target.value }))} className="input w-full">
                <option value="">No VAT</option>
                {taxRates?.map(t => <option key={t.id} value={t.id}>{t.name} ({(t.rate * 100).toFixed(0)}%)</option>)}
              </select></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Minimum Stock</label><input type="number" min="0" step="0.01" value={form.minimumStock} onChange={e => setForm(f => ({ ...f, minimumStock: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Barcode</label><input value={form.barcode} onChange={e => setForm(f => ({ ...f, barcode: e.target.value }))} className="input w-full" /></div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Description</label><input value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} className="input w-full" /></div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={create.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">{create.isPending ? "Saving..." : "Save Product"}</button>
            <button type="button" onClick={() => setShowForm(false)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
          </div>
          {create.isError && <p className="text-sm text-red-600">Error saving product. Check all required fields.</p>}
        </form>
      )}

      <div className="relative max-w-md">
        <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by name or SKU..." className="input w-full pl-4" />
      </div>

      {isLoading ? <div className="text-center py-10 text-muted-foreground">Loading products...</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">SKU</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Product</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Category</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Purchase</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Sales</th>
              <th className="text-center p-3 font-medium text-muted-foreground">VAT</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Stock</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {products?.map(p => (
                <tr key={p.id} className={`hover:bg-muted/20 transition-colors ${!p.isActive ? "opacity-60" : ""}`}>
                  <td className="p-3 font-mono text-xs text-muted-foreground">{p.sku}</td>
                  <td className="p-3"><p className="font-medium">{p.name}</p><p className="text-xs text-muted-foreground">{p.unit}</p></td>
                  <td className="p-3 text-muted-foreground">{p.category ?? "—"}</td>
                  <td className="p-3 text-right tabular-nums text-muted-foreground">{p.purchasePrice.toLocaleString("en-EG", { style: "currency", currency: p.currencyCode || "EGP" })}</td>
                  <td className="p-3 text-right tabular-nums font-medium">{p.salesPrice.toLocaleString("en-EG", { style: "currency", currency: p.currencyCode || "EGP" })}</td>
                  <td className="p-3 text-center text-blue-600 font-medium">{p.taxRatePercent > 0 ? `${p.taxRatePercent}%` : "—"}</td>
                  <td className="p-3 text-center"><StockBadge qty={p.totalQuantity} min={p.minimumStock} /></td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${p.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>{p.isActive ? "Active" : "Inactive"}</span></td>
                  <td className="p-3"><button onClick={() => toggle.mutate(p.id)} className="text-muted-foreground hover:text-foreground" title={p.isActive ? "Deactivate" : "Activate"}><PowerOff className="h-4 w-4" /></button></td>
                </tr>
              ))}
              {!products?.length && <tr><td colSpan={9} className="p-8 text-center text-muted-foreground">No products found. Add your first product.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
