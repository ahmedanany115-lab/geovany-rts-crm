"use client";
import { useState } from "react";
import { useMaintenanceContracts, useMaintenanceContract, useUpsertEquipment } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { HardDrive, Plus, X, RefreshCw } from "lucide-react";

const INIT = { contractId:"", itemName:"", serialNumber:"", brand:"", model:"", location:"", notes:"" };

export default function EquipmentPage() {
  const { t } = useT();
  const { toast } = useToast();
  const [selectedContract, setSelectedContract] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState(INIT);

  const { data: contracts } = useMaintenanceContracts({ status: 2 });
  const { data: contractDetail, refetch } = useMaintenanceContract(selectedContract);
  const upsert = useUpsertEquipment();

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await upsert.mutateAsync({ ...form, contractId: selectedContract });
      toast("Equipment saved.", "success");
      setForm(INIT); setShowForm(false);
    } catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  const equipment = (contractDetail as any)?.equipment ?? [];

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><HardDrive className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("maintenance_equipment")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          {selectedContract && <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">{showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}{showForm ? t("cancel") : t("add_equipment")}</button>}
        </div>
      </div>

      <div className="max-w-sm">
        <label className="text-xs text-muted-foreground block mb-1">Select Contract</label>
        <select value={selectedContract} onChange={e => setSelectedContract(e.target.value)} className="input w-full">
          <option value="">Choose a contract...</option>
          {contracts?.map(c => <option key={c.id} value={c.id}>{c.customerName} / {c.contractNumber}</option>)}
        </select>
      </div>

      {showForm && (
        <form onSubmit={handleSave} className="card p-5 space-y-3 border border-primary/20">
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">{t("name")} *</label><input required value={form.itemName} onChange={e => setForm(f => ({ ...f, itemName: e.target.value }))} className="input w-full" placeholder="e.g. Central AC Unit" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("serial_number")}</label><input value={form.serialNumber} onChange={e => setForm(f => ({ ...f, serialNumber: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("brand")}</label><input value={form.brand} onChange={e => setForm(f => ({ ...f, brand: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("model")}</label><input value={form.model} onChange={e => setForm(f => ({ ...f, model: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("location")}</label><input value={form.location} onChange={e => setForm(f => ({ ...f, location: e.target.value }))} className="input w-full" /></div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">{t("notes")}</label><input value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" /></div>
          </div>
          <button type="submit" disabled={upsert.isPending} className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50">{upsert.isPending ? t("saving") : t("save")}</button>
        </form>
      )}

      {selectedContract && (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("serial_number")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("brand")} / {t("model")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("location")}</th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {equipment.map((eq: any) => (
                <tr key={eq.id} className="hover:bg-muted/20">
                  <td className="p-3 font-medium">{eq.itemName}</td>
                  <td className="p-3 font-mono text-xs">{eq.serialNumber ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">{[eq.brand, eq.model].filter(Boolean).join(" / ") || "—"}</td>
                  <td className="p-3 text-muted-foreground">{eq.location ?? "—"}</td>
                </tr>
              ))}
              {equipment.length === 0 && <tr><td colSpan={4} className="p-8 text-center text-muted-foreground">No equipment registered for this contract.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
