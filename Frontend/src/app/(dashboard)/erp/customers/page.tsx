"use client";
import { useCustomers, useCreateCustomer, useToggleCustomerStatus } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { useState } from "react";
import { Users, Plus, RefreshCw, PowerOff, Search } from "lucide-react";

export default function ErpCustomersPage() {
  const { t } = useT();
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ code: "", name: "", taxNumber: "", phone: "", email: "", address: "", notes: "" });
  const { data, isLoading, refetch } = useCustomers({ search: search || undefined });
  const create = useCreateCustomer();
  const toggle = useToggleCustomerStatus();

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    await create.mutateAsync({ ...form, partnerType: 1 });
    setForm({ code: "", name: "", taxNumber: "", phone: "", email: "", address: "", notes: "" });
    setShowForm(false);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Users className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("customers")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"><Plus className="h-4 w-4" />{showForm ? t("cancel") : t("add")}</button>
        </div>
      </div>
      {showForm && (
        <form onSubmit={handleCreate} className="card p-5 space-y-4 border border-primary/20">
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">{t("code")} *</label><input required value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("name")} *</label><input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Tax #</label><input value={form.taxNumber} onChange={e => setForm(f => ({ ...f, taxNumber: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("phone")}</label><input value={form.phone} onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("email")}</label><input type="email" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("address")}</label><input value={form.address} onChange={e => setForm(f => ({ ...f, address: e.target.value }))} className="input w-full" /></div>
          </div>
          <button type="submit" disabled={create.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">{create.isPending ? t("saving") : t("save")}</button>
        </form>
      )}
      <div className="relative max-w-md"><Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" /><input value={search} onChange={e => setSearch(e.target.value)} placeholder={t("search") + "..."} className="input pl-10 w-full" /></div>
      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("code")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("phone")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("email")}</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(c => (
                <tr key={c.id} className="hover:bg-muted/20">
                  <td className="p-3 font-mono text-xs">{c.code}</td>
                  <td className="p-3 font-medium">{c.name}</td>
                  <td className="p-3 text-muted-foreground">{c.phone ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">{c.email ?? "—"}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${c.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>{c.isActive ? t("active") : t("inactive")}</span></td>
                  <td className="p-3 text-right"><button onClick={() => toggle.mutate(c.id)} className="text-muted-foreground hover:text-foreground"><PowerOff className="h-4 w-4" /></button></td>
                </tr>
              ))}
              {!data?.length && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
