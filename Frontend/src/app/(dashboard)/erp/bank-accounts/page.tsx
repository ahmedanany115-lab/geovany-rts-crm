"use client";
import { useState } from "react";
import { useBankAccounts, useCreateBankAccount, useUpdateBankAccount } from "@/features/erp/hooks";
import { useAccounts } from "@/features/finance/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { Landmark, Plus, X, Pencil, Check, RefreshCw } from "lucide-react";

const INIT = { code:"", name:"", bankName:"", accountNumber:"", iban:"", glAccountId:"", currencyId:"", isActive:true };

export default function BankAccountsPage() {
  const { t } = useT();
  const { toast } = useToast();
  const { data, isLoading, refetch } = useBankAccounts({});
  const { data: glAccounts } = useAccounts();
  const { data: currencies } = useCurrencies();
  const create = useCreateBankAccount();
  const update = useUpdateBankAccount();

  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing]   = useState<any>(null);
  const [form, setForm]         = useState(INIT);

  const postingAccounts = (glAccounts ?? []).filter((a: any) => !a.isGroup && a.isActive);

  const openEdit = (b: any) => {
    setEditing(b);
    setForm({ code: b.code, name: b.name, bankName: b.bankName ?? "", accountNumber: b.accountNumber ?? "",
              iban: b.iban ?? "", glAccountId: b.glAccountId ?? "", currencyId: b.currencyId ?? "", isActive: b.isActive });
    setShowForm(true);
  };

  const closeForm = () => { setShowForm(false); setEditing(null); setForm(INIT); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editing) {
        await update.mutateAsync({ id: editing.id, data: form });
        toast(`"${form.name}" updated.`, "success");
      } else {
        await create.mutateAsync(form);
        toast(`Bank account "${form.name}" created.`, "success");
      }
      closeForm();
    } catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Landmark className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("bank_accounts")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={showForm ? closeForm : () => setShowForm(true)}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : t("add")}
          </button>
        </div>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-3 border border-primary/20">
          <h2 className="font-semibold text-sm">{editing ? `Edit: ${editing.name}` : "New Bank Account"}</h2>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">Code *</label>
              <input required value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Account Name *</label>
              <input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Bank Name *</label>
              <input required value={form.bankName} onChange={e => setForm(f => ({ ...f, bankName: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Account Number</label>
              <input value={form.accountNumber} onChange={e => setForm(f => ({ ...f, accountNumber: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">IBAN</label>
              <input value={form.iban} onChange={e => setForm(f => ({ ...f, iban: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">GL Account (Chart of Accounts)</label>
              <select value={form.glAccountId} onChange={e => setForm(f => ({ ...f, glAccountId: e.target.value }))} className="input w-full">
                <option value="">Select GL account…</option>
                {postingAccounts.map((a: any) => <option key={a.id} value={a.id}>{a.code} — {a.name}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Currency</label>
              <select value={form.currencyId} onChange={e => setForm(f => ({ ...f, currencyId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {currencies?.filter(c => c.isActive).map(c => <option key={c.id} value={c.id}>{c.code}</option>)}
              </select>
            </div>
            <div className="flex items-center gap-2 mt-4">
              <input type="checkbox" id="baActive" checked={form.isActive} onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))} />
              <label htmlFor="baActive" className="text-sm">Active</label>
            </div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={create.isPending || update.isPending}
              className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
              <Check className="h-4 w-4" />{(create.isPending || update.isPending) ? t("saving") : t("save")}
            </button>
            <button type="button" onClick={closeForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}

      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("code")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Bank</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("currency")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Balance</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3 text-right text-muted-foreground">{t("actions")}</th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(b => (
                <tr key={b.id} className={`hover:bg-muted/20 ${!b.isActive ? "opacity-60" : ""}`}>
                  <td className="p-3 font-mono text-xs">{b.code}</td>
                  <td className="p-3 font-semibold">{b.name}</td>
                  <td className="p-3 text-muted-foreground">{(b as any).bankName ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">{b.currencyCode}</td>
                  <td className="p-3 text-right tabular-nums font-semibold">{b.currentBalance?.toLocaleString()}</td>
                  <td className="p-3 text-center">
                    <span className={`text-xs px-2 py-0.5 rounded-full ${b.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>
                      {b.isActive ? t("active") : t("inactive")}
                    </span>
                  </td>
                  <td className="p-3 text-right">
                    <button onClick={() => openEdit(b)} className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground">
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              ))}
              {!data?.length && <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
