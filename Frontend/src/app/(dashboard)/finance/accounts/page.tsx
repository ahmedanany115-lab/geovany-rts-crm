"use client";

import { useState } from "react";
import { AlertCircle, ChevronRight, Layers, Plus, PowerOff, RefreshCw } from "lucide-react";
import { useAccounts, useCreateAccount, useToggleAccountStatus } from "@/features/finance/hooks";
import { AccountType, AccountTypeLabels, type CreateAccountRequest } from "@/features/finance/types";

function Badge({ children, variant }: { children: React.ReactNode; variant: "success" | "muted" | "warning" }) {
  const cls = {
    success: "bg-emerald/10 text-emerald",
    muted:   "bg-accent text-muted-foreground",
    warning: "bg-warning/10 text-warning",
  }[variant];
  return <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${cls}`}>{children}</span>;
}

const ACCOUNT_TYPE_COLORS: Record<AccountType, string> = {
  [AccountType.Asset]:       "text-blue-600",
  [AccountType.Liability]:   "text-red-600",
  [AccountType.Equity]:      "text-purple-600",
  [AccountType.Revenue]:     "text-emerald",
  [AccountType.CostOfSales]: "text-orange-600",
  [AccountType.Expense]:     "text-rose-600",
};

export default function AccountsPage() {
  const { data: accounts, isLoading, isError, refetch } = useAccounts({ isActive: undefined });
  const createAccount = useCreateAccount();
  const toggleStatus  = useToggleAccountStatus();

  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CreateAccountRequest>({
    code: "", name: "", nameAr: "", accountType: AccountType.Asset, isGroup: false,
  });
  const [formError, setFormError] = useState<string | null>(null);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);
    try {
      await createAccount.mutateAsync(form);
      setShowForm(false);
      setForm({ code: "", name: "", nameAr: "", accountType: AccountType.Asset, isGroup: false });
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "Failed to create account.");
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Chart of Accounts</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Manage your accounting structure — assets, liabilities, equity, revenue, and expenses.
          </p>
        </div>
        <button
          onClick={() => setShowForm(!showForm)}
          className="flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90"
        >
          <Plus className="h-4 w-4" /> New Account
        </button>
      </div>

      {/* Create form */}
      {showForm && (
        <form onSubmit={handleCreate} className="rounded-lg border bg-background p-4 space-y-4">
          <h2 className="font-medium">New Account</h2>
          {formError && (
            <p className="text-sm text-danger flex items-center gap-2">
              <AlertCircle className="h-4 w-4 shrink-0" /> {formError}
            </p>
          )}
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
            <div className="space-y-1">
              <label className="text-sm font-medium">Code *</label>
              <input
                required value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))}
                placeholder="e.g. 1101" maxLength={20}
                className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div className="space-y-1 col-span-2">
              <label className="text-sm font-medium">Name *</label>
              <input
                required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                placeholder="e.g. Petty Cash" maxLength={200}
                className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div className="space-y-1 col-span-2">
              <label className="text-sm font-medium">Arabic Name</label>
              <input
                value={form.nameAr ?? ""} onChange={e => setForm(f => ({ ...f, nameAr: e.target.value }))}
                placeholder="e.g. الصندوق" dir="rtl"
                className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium">Account Type *</label>
              <select
                value={form.accountType}
                onChange={e => setForm(f => ({ ...f, accountType: Number(e.target.value) as AccountType }))}
                className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              >
                {Object.entries(AccountTypeLabels).map(([k, v]) => (
                  <option key={k} value={k}>{v}</option>
                ))}
              </select>
            </div>
            <div className="flex items-end pb-2">
              <label className="flex items-center gap-2 text-sm">
                <input type="checkbox" checked={form.isGroup}
                  onChange={e => setForm(f => ({ ...f, isGroup: e.target.checked }))}
                  className="h-4 w-4 rounded border"
                />
                Group account (has children)
              </label>
            </div>
          </div>
          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => setShowForm(false)}
              className="rounded-md border px-4 py-2 text-sm hover:bg-accent">Cancel</button>
            <button type="submit" disabled={createAccount.isPending}
              className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:opacity-50">
              {createAccount.isPending ? "Saving…" : "Create Account"}
            </button>
          </div>
        </form>
      )}

      {/* Table */}
      {isLoading && (
        <div className="space-y-2">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-12 animate-pulse rounded-lg bg-accent/40" />
          ))}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 rounded-lg border border-danger/30 bg-danger/5 p-4 text-sm">
          <AlertCircle className="h-4 w-4 shrink-0 text-danger" />
          Failed to load accounts.
          <button onClick={() => refetch()} className="ml-auto font-medium text-primary hover:underline">
            <RefreshCw className="h-4 w-4" />
          </button>
        </div>
      )}

      {accounts && (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-full text-sm">
            <thead className="bg-accent/40 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-3">Code</th>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Arabic</th>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Kind</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {accounts.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-10 text-center text-muted-foreground">
                    No accounts yet. Click "New Account" to get started.
                  </td>
                </tr>
              )}
              {accounts.map(acc => (
                <tr key={acc.id} className="hover:bg-accent/20 transition-colors">
                  <td className="px-4 py-3 font-mono font-medium">{acc.code}</td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1">
                      {acc.parentCode && (
                        <span className="text-muted-foreground text-xs">{acc.parentCode}</span>
                      )}
                      {acc.parentCode && <ChevronRight className="h-3 w-3 text-muted-foreground" />}
                      <span className={acc.isGroup ? "font-semibold" : ""}>{acc.name}</span>
                      {acc.isGroup && <Layers className="h-3 w-3 ml-1 text-muted-foreground" />}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-right" dir="rtl">{acc.nameAr ?? "—"}</td>
                  <td className="px-4 py-3">
                    <span className={`font-medium text-xs ${ACCOUNT_TYPE_COLORS[acc.accountType]}`}>
                      {acc.accountTypeName}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <Badge variant={acc.isGroup ? "muted" : "success"}>
                      {acc.isGroup ? "Group" : "Posting"}
                    </Badge>
                  </td>
                  <td className="px-4 py-3">
                    <Badge variant={acc.isActive ? "success" : "warning"}>
                      {acc.isActive ? "Active" : "Inactive"}
                    </Badge>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => toggleStatus.mutate(acc.id)}
                      disabled={toggleStatus.isPending}
                      title={acc.isActive ? "Deactivate" : "Activate"}
                      className="text-muted-foreground hover:text-foreground disabled:opacity-50"
                    >
                      <PowerOff className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
