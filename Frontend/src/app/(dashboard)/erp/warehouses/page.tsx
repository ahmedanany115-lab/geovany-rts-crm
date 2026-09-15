"use client";
import { useWarehouses, useCreateWarehouse, useUpdateWarehouse, useToggleWarehouseStatus } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { useState } from "react";
import { Warehouse, Plus, PowerOff, RefreshCw, Pencil, X, Check } from "lucide-react";

const INIT = { code: "", name: "", location: "", notes: "" };

export default function WarehousesPage() {
  const { t } = useT();
  const { toast } = useToast();
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing]   = useState<any>(null);
  const [form, setForm]         = useState(INIT);
  const { data, isLoading, refetch } = useWarehouses();
  const create = useCreateWarehouse();
  const update = useUpdateWarehouse();
  const toggle = useToggleWarehouseStatus();

  const openEdit = (w: any) => {
    setEditing(w);
    setForm({ code: w.code, name: w.name, location: w.location ?? "", notes: w.notes ?? "" });
    setShowForm(true);
  };
  const closeForm = () => { setShowForm(false); setEditing(null); setForm(INIT); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editing) { await update.mutateAsync({ id: editing.id, data: form }); toast(`"${form.name}" updated.`, "success"); }
      else         { await create.mutateAsync(form); toast(`Warehouse "${form.name}" created.`, "success"); }
      closeForm();
    } catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  const handleToggle = async (w: any) => {
    try { await toggle.mutateAsync(w.id); toast(`"${w.name}" ${w.isActive ? "deactivated" : "activated"}.`, "success"); }
    catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Warehouse className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("warehouses")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={showForm ? closeForm : () => setShowForm(true)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : t("add")}
          </button>
        </div>
      </div>
      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-3 border border-primary/20">
          <h2 className="font-semibold text-sm">{editing ? `Edit: ${editing.name}` : "New Warehouse"}</h2>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">{t("code")} *</label>
              <input required value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("name")} *</label>
              <input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Location</label>
              <input value={form.location} onChange={e => setForm(f => ({ ...f, location: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("notes")}</label>
              <input value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" /></div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={create.isPending || update.isPending}
              className="btn-primary px-4 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
              <Check className="h-4 w-4" />{(create.isPending || update.isPending) ? t("saving") : t("save")}
            </button>
            <button type="button" onClick={closeForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}
      {isLoading ? <div className="p-8 text-center text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("code")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Location</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3 text-right text-muted-foreground">{t("actions")}</th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(w => (
                <tr key={w.id} className={`hover:bg-muted/20 ${!w.isActive ? "opacity-60" : ""}`}>
                  <td className="p-3 font-mono text-xs">{w.code}</td>
                  <td className="p-3 font-medium">{w.name}</td>
                  <td className="p-3 text-muted-foreground">{w.location ?? "—"}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${w.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>{w.isActive ? t("active") : t("inactive")}</span></td>
                  <td className="p-3 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <button onClick={() => openEdit(w)} className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground"><Pencil className="h-3.5 w-3.5" /></button>
                      <button onClick={() => handleToggle(w)} className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground"><PowerOff className="h-3.5 w-3.5" /></button>
                    </div>
                  </td>
                </tr>
              ))}
              {!data?.length && <tr><td colSpan={5} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
