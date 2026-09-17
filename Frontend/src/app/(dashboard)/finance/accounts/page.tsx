"use client";

import { useState } from "react";
import {
  useAccounts,
  useCreateAccount,
  useUpdateAccount,
  useDeleteAccount,
  useToggleAccountStatus,
} from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { AccountType, AccountTypeLabels, type AccountDto } from "@/features/finance/types";
import {
  BookOpen, Plus, RefreshCw, PowerOff, ChevronRight,
  Pencil, Trash2, X, Check, AlertTriangle,
} from "lucide-react";

const TYPE_OPTS = [
  { value: AccountType.Asset,       label: "Asset" },
  { value: AccountType.Liability,   label: "Liability" },
  { value: AccountType.Equity,      label: "Equity" },
  { value: AccountType.Revenue,     label: "Revenue" },
  { value: AccountType.CostOfSales, label: "Cost of Sales" },
  { value: AccountType.Expense,     label: "Expense" },
];

const INIT = {
  code: "", name: "", nameAr: "",
  accountType: AccountType.Asset as AccountType,
  parentId: "", isGroup: false,
};

type FormState = typeof INIT;

export default function AccountsPage() {
  const { t } = useT();
  const { toast } = useToast();
  const { data, isLoading, refetch } = useAccounts();

  const create = useCreateAccount();
  const update = useUpdateAccount();
  const del    = useDeleteAccount();
  const toggle = useToggleAccountStatus();

  // Create / edit form
  const [showForm,  setShowForm]  = useState(false);
  const [editing,   setEditing]   = useState<AccountDto | null>(null);
  const [form,      setForm]      = useState<FormState>(INIT);
  const [formError, setFormError] = useState<string | null>(null);

  // Delete confirm
  const [deleteTarget, setDeleteTarget] = useState<AccountDto | null>(null);

  /* ── helpers ── */
  const openCreate = () => {
    setEditing(null);
    setForm(INIT);
    setFormError(null);
    setShowForm(true);
  };

  const openEdit = (a: AccountDto) => {
    setEditing(a);
    setForm({
      code:        a.code,
      name:        a.name,
      nameAr:      a.nameAr ?? "",
      accountType: a.accountType as AccountType,
      parentId:    a.parentId ?? "",
      isGroup:     a.isGroup,
    });
    setFormError(null);
    setShowForm(true);
  };

  const closeForm = () => {
    setShowForm(false);
    setEditing(null);
    setForm(INIT);
    setFormError(null);
  };

  /* ── submit (create or update) ── */
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (!form.name.trim()) { setFormError("Name is required."); return; }
    if (!editing && !form.code.trim()) { setFormError("Code is required."); return; }

    try {
      if (editing) {
        await update.mutateAsync({
          id: editing.id,
          data: {
            ...(form.code.trim() !== editing.code && { code: form.code.trim() }),
            name:     form.name.trim(),
            nameAr:   form.nameAr.trim() || undefined,
            isGroup:  form.isGroup,
            parentId: form.parentId || undefined,
          },
        });
        toast(`"${form.name}" updated.`, "success");
      } else {
        await create.mutateAsync({
          code:        form.code.trim(),
          name:        form.name.trim(),
          nameAr:      form.nameAr.trim() || undefined,
          accountType: form.accountType,
          isGroup:     form.isGroup,
          parentId:    form.parentId || undefined,
        });
        toast(`Account "${form.name}" created.`, "success");
      }
      closeForm();
    } catch (err: any) {
      const msg = err?.message ?? "Failed to save.";
      setFormError(msg);
      toast(msg, "error");
    }
  };

  /* ── toggle active ── */
  const handleToggle = async (a: AccountDto) => {
    try {
      await toggle.mutateAsync(a.id);
      toast(`"${a.name}" ${a.isActive ? "deactivated" : "activated"}.`, "success");
    } catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  /* ── delete ── */
  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      await del.mutateAsync(deleteTarget.id);
      toast(`"${deleteTarget.name}" deleted.`, "info");
      setDeleteTarget(null);
    } catch (err: any) {
      toast(err?.message ?? "Failed to delete.", "error");
      setDeleteTarget(null);
    }
  };

  /* ── group accounts for parent selector ── */
  const groupAccounts = data?.filter(a => a.isGroup && a.isActive) ?? [];

  /* ── type badge colours ── */
  const TYPE_CLS: Record<AccountType, string> = {
    [AccountType.Asset]:       "bg-blue-100 text-blue-700",
    [AccountType.Liability]:   "bg-red-100 text-red-700",
    [AccountType.Equity]:      "bg-purple-100 text-purple-700",
    [AccountType.Revenue]:     "bg-emerald-100 text-emerald-700",
    [AccountType.CostOfSales]: "bg-orange-100 text-orange-700",
    [AccountType.Expense]:     "bg-amber-100 text-amber-700",
  };

  return (
    <div className="p-6 space-y-6">

      {/* ── Delete confirm modal ── */}
      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <div className="flex items-center gap-3 text-red-600">
              <AlertTriangle className="h-5 w-5 shrink-0" />
              <h2 className="font-semibold">{t("confirm_delete")}</h2>
            </div>
            <p className="text-sm text-muted-foreground">
              Delete <strong>{deleteTarget.code} — {deleteTarget.name}</strong>?
              {" "}Accounts with existing journal entries cannot be deleted.
            </p>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setDeleteTarget(null)}
                className="btn-ghost px-4 py-2 rounded-lg text-sm">
                Cancel
              </button>
              <button onClick={handleDelete} disabled={del.isPending}
                className="px-4 py-2 rounded-lg text-sm bg-red-600 text-white hover:bg-red-700 disabled:opacity-50">
                {del.isPending ? "Deleting…" : "Delete"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Header ── */}
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
            onClick={showForm ? closeForm : openCreate}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"
          >
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : t("add")}
          </button>
        </div>
      </div>

      {/* ── Create / Edit form ── */}
      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">
            {editing ? `Edit: ${editing.code} — ${editing.name}` : "New Account"}
          </h2>

          {formError && (
            <div className="rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700">
              {formError}
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            {/* Code — editable on both create and edit */}
            <div>
              <label className="text-xs text-muted-foreground block mb-1">
                {t("code")} *
              </label>
              <input
                required
                value={form.code}
                onChange={e => setForm(f => ({ ...f, code: e.target.value }))}
                className="input w-full font-mono"
                placeholder="e.g. 1010"
              />
              {editing && (
                <p className="text-xs text-muted-foreground mt-0.5">
                  Changing the code updates the account number. Duplicate codes are rejected.
                </p>
              )}
            </div>

            {/* English name */}
            <div>
              <label className="text-xs text-muted-foreground block mb-1">
                {t("name")} (EN) *
              </label>
              <input
                required
                value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                className="input w-full"
              />
            </div>

            {/* Arabic name */}
            <div>
              <label className="text-xs text-muted-foreground block mb-1">
                {t("name")} (AR)
              </label>
              <input
                value={form.nameAr}
                onChange={e => setForm(f => ({ ...f, nameAr: e.target.value }))}
                className="input w-full"
                dir="rtl"
                placeholder="الاسم بالعربية"
              />
            </div>

            {/* Type — only on create */}
            {!editing && (
              <div>
                <label className="text-xs text-muted-foreground block mb-1">
                  {t("type")} *
                </label>
                <select
                  value={form.accountType}
                  onChange={e => setForm(f => ({ ...f, accountType: Number(e.target.value) as AccountType }))}
                  className="input w-full"
                >
                  {TYPE_OPTS.map(o => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>
            )}

            {/* Parent account */}
            <div className={editing ? "col-span-2 md:col-span-1" : ""}>
              <label className="text-xs text-muted-foreground block mb-1">
                Parent Account (optional)
              </label>
              <select
                value={form.parentId}
                onChange={e => setForm(f => ({ ...f, parentId: e.target.value }))}
                className="input w-full"
              >
                <option value="">— None (top-level) —</option>
                {groupAccounts
                  .filter(a => !editing || a.id !== editing.id)
                  .map(a => (
                    <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
                  ))}
              </select>
            </div>

            {/* Group checkbox */}
            <div className="flex items-center gap-2 mt-4">
              <input
                type="checkbox"
                id="isGroup"
                checked={form.isGroup}
                onChange={e => setForm(f => ({ ...f, isGroup: e.target.checked }))}
                className="h-4 w-4"
              />
              <label htmlFor="isGroup" className="text-sm cursor-pointer">
                Group / Header Account
              </label>
            </div>
          </div>

          <div className="flex gap-2">
            <button
              type="submit"
              disabled={create.isPending || update.isPending}
              className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2"
            >
              <Check className="h-4 w-4" />
              {(create.isPending || update.isPending) ? t("saving") : t("save")}
            </button>
            <button type="button" onClick={closeForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">
              {t("cancel")}
            </button>
          </div>
        </form>
      )}

      {/* ── Accounts table ── */}
      <div className="card overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center text-muted-foreground">{t("loading")}</div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-muted/30">
              <tr>
                <th className="text-left p-3 font-medium text-muted-foreground w-24">{t("code")}</th>
                <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
                <th className="text-left p-3 font-medium text-muted-foreground w-32">{t("type")}</th>
                <th className="text-center p-3 font-medium text-muted-foreground w-20">Group</th>
                <th className="text-center p-3 font-medium text-muted-foreground w-24">{t("status")}</th>
                <th className="p-3 w-28 text-right text-muted-foreground">{t("actions")}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {data?.map(a => (
                <tr
                  key={a.id}
                  className={`hover:bg-muted/20 ${a.isGroup ? "font-semibold" : ""} ${!a.isActive ? "opacity-50" : ""}`}
                >
                  <td className="p-3 font-mono text-xs">{a.code}</td>
                  <td className="p-3">
                    <div className="flex items-center gap-1">
                      {a.parentId && !a.isGroup && (
                        <ChevronRight className="h-3 w-3 text-muted-foreground/40 shrink-0" />
                      )}
                      <span>{a.name}</span>
                      {a.nameAr && (
                        <span className="text-muted-foreground font-normal text-xs mr-1">
                          / {a.nameAr}
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="p-3">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${TYPE_CLS[a.accountType as AccountType] ?? "bg-muted"}`}>
                      {AccountTypeLabels[a.accountType as AccountType] ?? a.accountType}
                    </span>
                  </td>
                  <td className="p-3 text-center">
                    {a.isGroup && (
                      <span className="text-xs px-1.5 py-0.5 rounded bg-blue-50 text-blue-600 font-medium">
                        Group
                      </span>
                    )}
                  </td>
                  <td className="p-3 text-center">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${a.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>
                      {a.isActive ? t("active") : t("inactive")}
                    </span>
                  </td>
                  <td className="p-3">
                    <div className="flex items-center justify-end gap-1">
                      {/* Edit */}
                      <button
                        onClick={() => openEdit(a)}
                        title={t("edit")}
                        className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground"
                      >
                        <Pencil className="h-3.5 w-3.5" />
                      </button>
                      {/* Toggle active */}
                      <button
                        onClick={() => handleToggle(a)}
                        title={a.isActive ? t("deactivate") : t("activate")}
                        className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground"
                      >
                        <PowerOff className="h-3.5 w-3.5" />
                      </button>
                      {/* Delete */}
                      <button
                        onClick={() => setDeleteTarget(a)}
                        title={t("delete")}
                        className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {!data?.length && (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-muted-foreground">
                    {t("no_data")}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
