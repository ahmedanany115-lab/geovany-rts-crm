"use client";
import { useState } from "react";
import { useAccounts, useCreateAccount, useToggleAccountStatus } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { BookOpen, Plus, RefreshCw, PowerOff } from "lucide-react";

const ACCOUNT_TYPES = ["", "Asset", "Liability", "Equity", "Revenue", "Expense"];

export default function AccountsPage() {
  const { t } = useT();
  const { data, isLoading, refetch } = useAccounts();
  const create = useCreateAccount();
  const toggle = useToggleAccountStatus();
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ code: "", name: "", accountType: 1, parentId: "", isGroup: false });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await create.mutateAsync({ ...form, accountType: Number(form.accountType) });
    setForm({ code: "", name: "", accountType: 1, parentId: "", isGroup: false });
    setShowForm(false);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><BookOpen className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("chart_of_accounts")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"><Plus className="h-4 w-4" />{showForm ? t("cancel") : t("add")}</button>
        </div>
      </div>
      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-3 border border-primary/20">
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">{t("code")} *</label><input required value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("name")} *</label><input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("type")}</label>
              <select value={form.accountType} onChange={e => setForm(f => ({ ...f, accountType: Number(e.target.value) }))} className="input w-full">
                {ACCOUNT_TYPES.slice(1).map((t, i) => <option key={t} value={i + 1}>{t}</option>)}
              </select>
            </div>
            <div className="flex items-center gap-2 mt-5"><input type="checkbox" checked={form.isGroup} onChange={e => setForm(f => ({ ...f, isGroup: e.target.checked }))} /><label className="text-sm">Group Account</label></div>
          </div>
          <button type="submit" disabled={create.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">{create.isPending ? t("saving") : t("save")}</button>
        </form>
      )}
      <div className="card overflow-hidden">
        {isLoading ? <div className="p-8 text-center text-muted-foreground">{t("loading")}</div> : (
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("code")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("type")}</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(a => (
                <tr key={a.id} className="hover:bg-muted/20">
                  <td className="p-3 font-mono text-xs">{a.code}</td>
                  <td className="p-3 font-medium">{a.name}{a.isGroup ? <span className="ml-2 text-xs text-muted-foreground">(Group)</span> : null}</td>
                  <td className="p-3 text-muted-foreground">{ACCOUNT_TYPES[a.accountType] ?? a.accountType}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${a.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>{a.isActive ? t("active") : t("inactive")}</span></td>
                  <td className="p-3 text-right"><button onClick={() => toggle.mutate(a.id)} className="text-muted-foreground hover:text-foreground"><PowerOff className="h-4 w-4" /></button></td>
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
