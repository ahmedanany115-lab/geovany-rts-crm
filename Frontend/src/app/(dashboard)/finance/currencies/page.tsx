"use client";
import { useState } from "react";
import { useCurrencies, useCreateCurrency, useUpdateCurrency } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { Coins, Plus, Pencil, X } from "lucide-react";

export default function CurrenciesPage() {
  const { t } = useT();
  const { data, isLoading } = useCurrencies();
  const create = useCreateCurrency();
  const update = useUpdateCurrency();
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<string | null>(null);
  const [form, setForm] = useState({ code: "", name: "", symbol: "", exchangeRate: 1, isActive: true });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (editing) {
      await update.mutateAsync({ id: editing, ...form });
      setEditing(null);
    } else {
      await create.mutateAsync(form);
    }
    setForm({ code: "", name: "", symbol: "", exchangeRate: 1, isActive: true });
    setShowForm(false);
  };

  const startEdit = (c: typeof data extends (infer T)[] | undefined ? T : never) => {
    setForm({ code: (c as any).code, name: (c as any).name, symbol: (c as any).symbol, exchangeRate: (c as any).exchangeRate, isActive: (c as any).isActive });
    setEditing((c as any).id);
    setShowForm(true);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Coins className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("currencies")}</h1></div>
        <button onClick={() => { setShowForm(v => !v); setEditing(null); setForm({ code: "", name: "", symbol: "", exchangeRate: 1, isActive: true }); }} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"><Plus className="h-4 w-4" />{showForm ? t("cancel") : t("add")}</button>
      </div>
      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-3 border border-primary/20">
          <div className="grid grid-cols-4 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">{t("code")} *</label><input required value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} className="input w-full" placeholder="EGP" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("name")} *</label><input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Symbol *</label><input required value={form.symbol} onChange={e => setForm(f => ({ ...f, symbol: e.target.value }))} className="input w-full" placeholder="ج.م" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Exchange Rate</label><input type="number" step="0.000001" value={form.exchangeRate} onChange={e => setForm(f => ({ ...f, exchangeRate: Number(e.target.value) }))} className="input w-full" /></div>
          </div>
          <button type="submit" disabled={create.isPending || update.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">{(create.isPending || update.isPending) ? t("saving") : t("save")}</button>
        </form>
      )}
      <div className="card overflow-hidden">
        {isLoading ? <div className="p-8 text-center text-muted-foreground">{t("loading")}</div> : (
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("code")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Symbol</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Rate</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Base</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(c => (
                <tr key={c.id} className="hover:bg-muted/20">
                  <td className="p-3 font-mono font-bold">{c.code}</td>
                  <td className="p-3">{c.name}</td>
                  <td className="p-3">{c.symbol}</td>
                  <td className="p-3 text-right tabular-nums">{c.exchangeRate.toFixed(4)}</td>
                  <td className="p-3 text-center">{c.isBaseCurrency ? <span className="text-xs px-2 py-0.5 rounded-full bg-blue-100 text-blue-700">Base</span> : "—"}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${c.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>{c.isActive ? t("active") : t("inactive")}</span></td>
                  <td className="p-3 text-right"><button onClick={() => startEdit(c as any)} className="text-muted-foreground hover:text-foreground"><Pencil className="h-4 w-4" /></button></td>
                </tr>
              ))}
              {!data?.length && <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
