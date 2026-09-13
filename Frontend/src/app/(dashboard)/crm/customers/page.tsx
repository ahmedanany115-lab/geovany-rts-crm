"use client";
import { useState } from "react";
import { useCustomers, useCreateCustomer, useToggleCustomerStatus } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { Users, Plus, Search, RefreshCw, PowerOff, X } from "lucide-react";

const INIT = { code: "", name: "", taxNumber: "", phone: "", email: "", address: "", notes: "" };

export default function CrmCustomersPage() {
  const { t } = useT();
  const { toast } = useToast();
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState(INIT);
  const [formError, setFormError] = useState<string | null>(null);

  const { data: customers, isLoading, refetch } = useCustomers({ search: search || undefined });
  const create = useCreateCustomer();
  const toggle = useToggleCustomerStatus();

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    // Client-side validation
    if (!form.code.trim()) { setFormError("Customer code is required."); return; }
    if (!form.name.trim()) { setFormError("Customer name is required."); return; }
    if (form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
      setFormError("Please enter a valid email address.");
      return;
    }

    try {
      await create.mutateAsync({
        code:       form.code.trim(),
        name:       form.name.trim(),
        taxNumber:  form.taxNumber.trim() || undefined,
        phone:      form.phone.trim() || undefined,
        email:      form.email.trim() || undefined,
        address:    form.address.trim() || undefined,
        notes:      form.notes.trim() || undefined,
        partnerType: 1,
      });
      toast(`Customer "${form.name}" created successfully.`, "success");
      setForm(INIT);
      setShowForm(false);
    } catch (err: any) {
      const msg = err?.message ?? "Failed to save customer. Please try again.";
      setFormError(msg);
      toast(msg, "error");
    }
  };

  const handleToggle = async (id: string, name: string, active: boolean) => {
    try {
      await toggle.mutateAsync(id);
      toast(`"${name}" ${active ? "deactivated" : "activated"}.`, "success");
    } catch (err: any) {
      toast(err?.message ?? "Could not update status.", "error");
    }
  };

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Users className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">CRM — {t("customers")}</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg" title={t("refresh")}>
            <RefreshCw className="h-4 w-4" />
          </button>
          <button
            onClick={() => { setShowForm(v => !v); setFormError(null); setForm(INIT); }}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"
          >
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : `${t("add")} ${t("customers")}`}
          </button>
        </div>
      </div>

      {/* Create form */}
      {showForm && (
        <form onSubmit={handleCreate} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">New Customer</h2>

          {formError && (
            <div className="rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700">
              {formError}
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
                placeholder="e.g. CUST-001"
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("name")} *</label>
              <input
                required
                value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                className="input w-full"
                placeholder="Customer / Company name"
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Tax Number</label>
              <input
                value={form.taxNumber}
                onChange={e => setForm(f => ({ ...f, taxNumber: e.target.value }))}
                className="input w-full"
                placeholder="e.g. 123-456-789"
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("phone")}</label>
              <input
                value={form.phone}
                onChange={e => setForm(f => ({ ...f, phone: e.target.value }))}
                className="input w-full"
                placeholder="+20 1xx xxxx xxxx"
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("email")}</label>
              <input
                type="email"
                value={form.email}
                onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
                className="input w-full"
                placeholder="contact@company.com"
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">{t("address")}</label>
              <input
                value={form.address}
                onChange={e => setForm(f => ({ ...f, address: e.target.value }))}
                className="input w-full"
              />
            </div>
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">{t("notes")}</label>
              <textarea
                rows={2}
                value={form.notes}
                onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
                className="input w-full resize-none"
              />
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
              onClick={() => { setShowForm(false); setFormError(null); setForm(INIT); }}
              className="btn-ghost px-4 py-2 rounded-lg text-sm"
            >
              {t("cancel")}
            </button>
          </div>
        </form>
      )}

      {/* Search */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <input
          value={search}
          onChange={e => setSearch(e.target.value)}
          placeholder={`${t("search")} ${t("customers")}...`}
          className="input pl-10 w-full"
        />
      </div>

      {/* Customers table */}
      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30">
              <tr>
                <th className="text-left p-3 font-medium text-muted-foreground">{t("code")}</th>
                <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
                <th className="text-left p-3 font-medium text-muted-foreground">{t("phone")}</th>
                <th className="text-left p-3 font-medium text-muted-foreground">{t("email")}</th>
                <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
                <th className="p-3 w-10"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {customers?.map(c => (
                <tr key={c.id} className="hover:bg-muted/20">
                  <td className="p-3 font-mono text-xs">{c.code}</td>
                  <td className="p-3 font-medium">{c.name}</td>
                  <td className="p-3 text-muted-foreground">{c.phone ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">{c.email ?? "—"}</td>
                  <td className="p-3 text-center">
                    <span className={`text-xs px-2 py-0.5 rounded-full ${
                      c.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"
                    }`}>
                      {c.isActive ? t("active") : t("inactive")}
                    </span>
                  </td>
                  <td className="p-3 text-right">
                    <button
                      onClick={() => handleToggle(c.id, c.name, c.isActive)}
                      title={c.isActive ? t("deactivate") : t("activate")}
                      className="text-muted-foreground hover:text-foreground"
                    >
                      <PowerOff className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ))}
              {!customers?.length && (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-muted-foreground">
                    {search ? `No customers match "${search}".` : t("no_data")}
                  </td>
                </tr>
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
