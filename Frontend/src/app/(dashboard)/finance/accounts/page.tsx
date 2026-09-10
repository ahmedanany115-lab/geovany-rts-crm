"use client";
import { useState } from "react";
import { useAccounts, useCreateAccount, useToggleAccountStatus } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { AccountType, AccountTypeLabels } from "@/features/finance/types";
import { BookOpen, Plus, RefreshCw, PowerOff, ChevronRight } from "lucide-react";

const ACCOUNT_TYPE_OPTIONS = [
  { value: AccountType.Asset,       label: "Asset" },
  { value: AccountType.Liability,   label: "Liability" },
  { value: AccountType.Equity,      label: "Equity" },
  { value: AccountType.Revenue,     label: "Revenue" },
  { value: AccountType.CostOfSales, label: "Cost of Sales" },
  { value: AccountType.Expense,     label: "Expense" },
];

const INIT = { code: "", name: "", nameAr: "", accountType: AccountType.Asset, parentId: "", isGroup: false };

export default function AccountsPage() {
  const { t } = useT();
  const { toast } = useToast();
  const { data, isLoading, refetch } = useAccounts();
  const create = useCreateAccount();
  const toggle = useToggleAccountStatus();
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState(INIT);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await create.mutateAsync({
        code:        form.code.trim(),
        name:        form.name.trim(),
        nameAr:      form.nameAr.trim() || undefined,
        accountType: form.accountType,          // already correct enum int
        isGroup:     form.isGroup,
        parentId:    form.parentId || undefined,
      });
      toast(`Account "${form.name}" created successfully.`, "success");
      setForm(INIT);
      setShowForm(false);
    } catch (err: any) {
      const msg = err?.message ?? "Failed to save account.";
      setError(msg);
      toast(msg, "error");
    }
  };

  const handleToggle = async (id: string, name: string, current: boolean) => {
    try {
      await toggle.mutateAsync(id);
      toast(`"${name}" ${current ? "deactivated" : "activated"}.`, "success");
    } catch {
      toast("Could not update status.", "error");
    }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <BookOpen className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">{t("chart_of_accounts")}</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg">
            <RefreshCw className="h-4 w-4" />
          </button>
          <button
            onClick={() => { setShowForm(v => !v); setError(null); setForm(INIT); }}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"
          >
            <Plus className="h-4 w-4" />
            {showForm ? t("cancel") : t("add")}
          </button>
        </div>
      </div>

      {/* Create form */}
      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">New Account</h2>
          {error && (
            <div className="rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700">
              {error}
            </div>
          )}
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("code")} *</label>
              <input
                required
                value={form.code}
                onChange={e => setForm(f => ({ ...f, code: e.target.value }))}
                className="input w-full font-mono"
                placeholder="e.g. 1010"
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("name")} (EN) *</label>
              <input
                required
                value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                className="input w-full"
                placeholder="e.g. Cash at Hand"
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Name (AR)</label>
              <input
                value={form.nameAr}
                onChange={e => setForm(f => ({ ...f, nameAr: e.target.value }))}
                className="input w-full"
                dir="rtl"
                placeholder="e.g. النقدية في الصندوق"
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("type")} *</label>
              <select
                value={form.accountType}
                onChange={e => setForm(f => ({ ...f, accountType: Number(e.target.value) as AccountType }))}
                className="input w-full"
              >
                {ACCOUNT_TYPE_OPTIONS.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">Parent Account (optional)</label>
              <select
                value={form.parentId}
                onChange={e => setForm(f => ({ ...f, parentId: e.target.value }))}
                className="input w-full"
              >
                <option value="">— None (top-level) —</option>
                {data?.filter(a => a.isGroup).map(a => (
                  <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
                ))}
              </select>
            </div>
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="isGroup"
                checked={form.isGroup}
                onChange={e => setForm(f => ({ ...f, isGroup: e.target.checked }))}
                className="h-4 w-4"
              />
              <label htmlFor="isGroup" className="text-sm cursor-pointer">Group / Header Account</label>
            </div>
          </div>
          <div className="flex gap-2">
            <button
              type="submit"
              disabled={create.isPending}
              className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50"
            >
              {create.isPending ? t("saving") : t("save")}
            </button>
            <button
              type="button"
              onClick={() => { setShowForm(false); setError(null); setForm(INIT); }}
              className="btn-ghost px-4 py-2 rounded-lg text-sm"
            >
              {t("cancel")}
            </button>
          </div>
        </form>
      )}

      {/* Accounts table */}
      <div className="card overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center text-muted-foreground">{t("loading")}</div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-muted/30">
              <tr>
                <th className="text-left p-3 font-medium text-muted-foreground">{t("code")}</th>
                <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
                <th className="text-left p-3 font-medium text-muted-foreground">{t("type")}</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Group</th>
                <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
                <th className="p-3 w-10"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {data?.map(a => (
                <tr
                  key={a.id}
                  className={`hover:bg-muted/20 ${a.isGroup ? "font-medium" : ""}`}
                >
                  <td className="p-3 font-mono text-xs">{a.code}</td>
                  <td className="p-3">
                    <div className="flex items-center gap-1">
                      {a.parentId && <ChevronRight className="h-3 w-3 text-muted-foreground/50" />}
                      {a.name}
                      {a.nameAr && <span className="text-xs text-muted-foreground mr-2">/ {a.nameAr}</span>}
                    </div>
                  </td>
                  <td className="p-3 text-muted-foreground">
                    {AccountTypeLabels[a.accountType as AccountType] ?? a.accountType}
                  </td>
                  <td className="p-3 text-center">
                    {a.isGroup && <span className="text-xs px-1.5 py-0.5 rounded bg-blue-100 text-blue-700">Group</span>}
                  </td>
                  <td className="p-3 text-center">
                    <span className={`text-xs px-2 py-0.5 rounded-full ${
                      a.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"
                    }`}>
                      {a.isActive ? t("active") : t("inactive")}
                    </span>
                  </td>
                  <td className="p-3 text-right">
                    <button
                      onClick={() => handleToggle(a.id, a.name, a.isActive)}
                      title={a.isActive ? "Deactivate" : "Activate"}
                      className="text-muted-foreground hover:text-foreground"
                    >
                      <PowerOff className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ))}
              {!data?.length && (
                <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
