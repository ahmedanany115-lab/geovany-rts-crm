"use client";
import { useState } from "react";
import { useFiscalPeriods, useCreateFiscalPeriod, useCloseFiscalPeriod, useOpenFiscalPeriod } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { CalendarDays, Plus, Lock, Unlock } from "lucide-react";

export default function FiscalPeriodsPage() {
  const { t } = useT();
  const { data, isLoading } = useFiscalPeriods();
  const create = useCreateFiscalPeriod();
  const close  = useCloseFiscalPeriod();
  const open   = useOpenFiscalPeriod();
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: "", startDate: "", endDate: "" });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await create.mutateAsync(form);
    setForm({ name: "", startDate: "", endDate: "" });
    setShowForm(false);
  };

  const STATUS_LABELS: Record<number, string> = { 1: "Open", 2: "Closed", 3: "Locked" };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><CalendarDays className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("fiscal_periods")}</h1></div>
        <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"><Plus className="h-4 w-4" />{showForm ? t("cancel") : t("add")}</button>
      </div>
      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-3 border border-primary/20">
          <div className="grid grid-cols-3 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">{t("name")} *</label><input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Start {t("date")} *</label><input required type="date" value={form.startDate} onChange={e => setForm(f => ({ ...f, startDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">End {t("date")} *</label><input required type="date" value={form.endDate} onChange={e => setForm(f => ({ ...f, endDate: e.target.value }))} className="input w-full" /></div>
          </div>
          <button type="submit" disabled={create.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">{create.isPending ? t("saving") : t("save")}</button>
        </form>
      )}
      <div className="card overflow-hidden">
        {isLoading ? <div className="p-8 text-center text-muted-foreground">{t("loading")}</div> : (
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Start</th>
              <th className="text-left p-3 font-medium text-muted-foreground">End</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(p => (
                <tr key={p.id} className="hover:bg-muted/20">
                  <td className="p-3 font-medium">{p.name}</td>
                  <td className="p-3 text-muted-foreground">{p.startDate}</td>
                  <td className="p-3 text-muted-foreground">{p.endDate}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${p.status === 1 ? "bg-emerald-100 text-emerald-700" : p.status === 2 ? "bg-amber-100 text-amber-700" : "bg-muted text-muted-foreground"}`}>{STATUS_LABELS[p.status] ?? p.status}</span></td>
                  <td className="p-3 text-right flex gap-2 justify-end">
                    {p.status === 1 && <button onClick={() => close.mutate(p.id)} className="flex items-center gap-1 text-xs px-2 py-1 rounded hover:bg-accent text-muted-foreground"><Lock className="h-3.5 w-3.5" />Close</button>}
                    {p.status === 2 && <button onClick={() => open.mutate(p.id)} className="flex items-center gap-1 text-xs px-2 py-1 rounded hover:bg-accent text-muted-foreground"><Unlock className="h-3.5 w-3.5" />Reopen</button>}
                  </td>
                </tr>
              ))}
              {!data?.length && <tr><td colSpan={5} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
