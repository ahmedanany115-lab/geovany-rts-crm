"use client";
import { useState } from "react";
import { Warehouse, RefreshCw, PowerOff, Plus, X } from "lucide-react";
import { useWarehouses, useCreateWarehouse, useToggleWarehouseStatus, useProductStock } from "@/features/erp/hooks";

export default function WarehousesPage() {
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ code: "", name: "", location: "", notes: "" });
  const { data: warehouses, isLoading, refetch } = useWarehouses();
  const { data: stock } = useProductStock();
  const create = useCreateWarehouse();
  const toggle = useToggleWarehouseStatus();

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    await create.mutateAsync(form);
    setForm({ code: "", name: "", location: "", notes: "" });
    setShowForm(false);
  };

  const getWarehouseValue = (id: string) => {
    const items = stock?.filter(s => s.warehouseId === id) ?? [];
    return items.reduce((sum, s) => sum + s.quantity * s.averageCost, 0);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Warehouse className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Warehouses</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? "Cancel" : "Add Warehouse"}
          </button>
        </div>
      </div>

      {showForm && (
        <form onSubmit={handleCreate} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-medium">New Warehouse</h2>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">Code *</label><input required value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} className="input w-full" placeholder="WH-01" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Name *</label><input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" placeholder="Main Warehouse" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Location</label><input value={form.location} onChange={e => setForm(f => ({ ...f, location: e.target.value }))} className="input w-full" placeholder="Cairo, Egypt" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Notes</label><input value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" /></div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={create.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">{create.isPending ? "Saving..." : "Save Warehouse"}</button>
            <button type="button" onClick={() => setShowForm(false)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
          </div>
        </form>
      )}

      {isLoading ? <div className="text-center py-10 text-muted-foreground">Loading warehouses...</div> : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {warehouses?.map(w => {
            const val = getWarehouseValue(w.id);
            return (
              <div key={w.id} className={`card p-4 space-y-3 ${!w.isActive ? "opacity-60" : ""}`}>
                <div className="flex items-start justify-between">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-xs text-muted-foreground">{w.code}</span>
                      <span className={`text-xs px-2 py-0.5 rounded-full ${w.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>{w.isActive ? "Active" : "Inactive"}</span>
                    </div>
                    <h3 className="font-semibold mt-1">{w.name}</h3>
                    {w.location && <p className="text-xs text-muted-foreground mt-0.5">{w.location}</p>}
                  </div>
                  <button onClick={() => toggle.mutate(w.id)} className="text-muted-foreground hover:text-foreground p-1"><PowerOff className="h-4 w-4" /></button>
                </div>
                <div className="grid grid-cols-2 gap-2 pt-2 border-t border-border">
                  <div><p className="text-xs text-muted-foreground">Products</p><p className="font-semibold">{w.productCount ?? 0}</p></div>
                  <div><p className="text-xs text-muted-foreground">Value (EGP)</p><p className="font-semibold tabular-nums">{val.toLocaleString("en-EG", { maximumFractionDigits: 0 })}</p></div>
                </div>
              </div>
            );
          })}
          {!warehouses?.length && <div className="col-span-3 p-8 text-center text-muted-foreground card">No warehouses found. Add your first warehouse.</div>}
        </div>
      )}
    </div>
  );
}
