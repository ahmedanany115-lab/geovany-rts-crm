"use client";

import { useState } from "react";
import { useCustomers, useCreateCustomer, useUpdateCustomer, useToggleCustomerStatus, useDeleteCustomer } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import type { BusinessPartnerDto } from "@/features/erp/types";
import { Users, Plus, Search, RefreshCw, PowerOff, Pencil, Trash2, X, Check, AlertTriangle } from "lucide-react";

type FormState = {
  code: string; name: string; taxNumber: string;
  phone: string; email: string; address: string; notes: string;
};
const EMPTY: FormState = { code: "", name: "", taxNumber: "", phone: "", email: "", address: "", notes: "" };

export default function CrmCustomersPage() {
  const { t } = useT();
  const { toast } = useToast();
  const [search, setSearch]     = useState("");
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing]   = useState<BusinessPartnerDto | null>(null);
  const [form, setForm]         = useState<FormState>(EMPTY);
  const [formError, setFormError] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<BusinessPartnerDto | null>(null);

  const { data: customers, isLoading, refetch } = useCustomers({ search: search || undefined });
  const create = useCreateCustomer();
  const update = useUpdateCustomer();
  const toggle = useToggleCustomerStatus();
  const del    = useDeleteCustomer();

  const openCreate = () => {
    setEditing(null); setForm(EMPTY); setFormError(null); setShowForm(true);
  };
  const openEdit = (c: BusinessPartnerDto) => {
    setEditing(c);
    setForm({ code: c.code, name: c.name, taxNumber: c.taxNumber ?? "", phone: c.phone ?? "", email: c.email ?? "", address: c.address ?? "", notes: c.notes ?? "" });
    setFormError(null); setShowForm(true);
  };
  const closeForm = () => { setShowForm(false); setEditing(null); setForm(EMPTY); setFormError(null); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!form.code.trim()) { setFormError("Code is required."); return; }
    if (!form.name.trim()) { setFormError("Name is required."); return; }
    if (form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
      setFormError("Enter a valid email."); return;
    }
    const payload = { ...form, code: form.code.trim(), name: form.name.trim(), partnerType: 1 };
    try {
      if (editing) {
        await update.mutateAsync({ id: editing.id, data: payload });
        toast(`"${form.name}" updated.`, "success");
      } else {
        await create.mutateAsync(payload);
        toast(`Customer "${form.name}" created.`, "success");
      }
      closeForm();
    } catch (err: any) {
      const msg = err?.message ?? "Failed to save customer.";
      setFormError(msg); toast(msg, "error");
    }
  };

  const handleToggle = async (c: BusinessPartnerDto) => {
    try {
      await toggle.mutateAsync(c.id);
      toast(`"${c.name}" ${c.isActive ? "deactivated" : "activated"}.`, "success");
    } catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    try {
      await del.mutateAsync(deleteTarget.id);
      toast(`"${deleteTarget.name}" deleted.`, "info");
      setDeleteTarget(null);
    } catch (err: any) { toast(err?.message ?? "Failed to delete.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      {/* Delete confirm dialog */}
      {deleteTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <div className="flex items-center gap-3 text-red-600">
              <AlertTriangle className="h-5 w-5 shrink-0" />
              <h2 className="font-semibold">{t("confirm_delete")}</h2>
            </div>
            <p className="text-sm text-muted-foreground">
              Delete <strong>{deleteTarget.name}</strong>? This cannot be undone.
            </p>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setDeleteTarget(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={handleDelete} disabled={del.isPending}
                className="px-4 py-2 rounded-lg text-sm bg-red-600 text-white hover:bg-red-700 disabled:opacity-50">
                {del.isPending ? "Deleting…" : "Delete"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Users className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">CRM — {t("customers")}</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={showForm ? closeForm : openCreate}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : `${t("add")} ${t("customers")}`}
          </button>
        </div>
      </div>

      {/* Create / Edit form */}
      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">{editing ? `Edit: ${editing.name}` : "New Customer"}</h2>
          {formError && (
            <div className="rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700">{formError}</div>
          )}
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">{t("code")} *</label>
              <input required value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} className="input w-full font-mono" placeholder="CUST-001" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("name")} *</label>
              <input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Tax #</label>
              <input value={form.taxNumber} onChange={e => setForm(f => ({ ...f, taxNumber: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("phone")}</label>
              <input value={form.phone} onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("email")}</label>
              <input type="email" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("address")}</label>
              <input value={form.address} onChange={e => setForm(f => ({ ...f, address: e.target.value }))} className="input w-full" />
            </div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">{t("notes")}</label>
              <textarea rows={2} value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} className="input w-full resize-none" />
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

      {/* Search */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <input value={search} onChange={e => setSearch(e.target.value)}
          placeholder={`${t("search")} ${t("customers")}…`} className="input pl-10 w-full" />
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("code")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("phone")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("email")}</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3 text-right text-muted-foreground">{t("actions")}</th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {customers?.map(c => (
                <tr key={c.id} className={`hover:bg-muted/20 ${!c.isActive ? "opacity-60" : ""}`}>
                  <td className="p-3 font-mono text-xs">{c.code}</td>
                  <td className="p-3 font-semibold">{c.name}</td>
                  <td className="p-3 text-muted-foreground">{c.phone ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">{c.email ?? "—"}</td>
                  <td className="p-3 text-center">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${c.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>
                      {c.isActive ? t("active") : t("inactive")}
                    </span>
                  </td>
                  <td className="p-3 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <button onClick={() => openEdit(c)} title={t("edit")}
                        className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground">
                        <Pencil className="h-3.5 w-3.5" />
                      </button>
                      <button onClick={() => handleToggle(c)} title={c.isActive ? t("deactivate") : t("activate")}
                        className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground">
                        <PowerOff className="h-3.5 w-3.5" />
                      </button>
                      <button onClick={() => setDeleteTarget(c)} title={t("delete")}
                        className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600">
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {!customers?.length && (
                <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">
                  {search ? `No customers match "${search}".` : t("no_data")}
                </td></tr>
              )}
            </tbody>
          </table>
          {customers && customers.length > 0 && (
            <div className="px-3 py-2 border-t text-xs text-muted-foreground">
              {customers.length} customer{customers.length !== 1 ? "s" : ""}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
