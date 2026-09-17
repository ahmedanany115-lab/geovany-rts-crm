"use client";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import {
  useCustomers, useCreateCustomer, useUpdateCustomer,
  useToggleCustomerStatus, useDeleteCustomer, useAssignCustomerSalesRep,
} from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { useRoles } from "@/hooks/useRoles";
import { Users, Plus, RefreshCw, PowerOff, Search, Pencil, Trash2, X, Check, AlertTriangle, UserCheck } from "lucide-react";

const INIT = { code: "", name: "", phone: "", email: "", address: "", notes: "" };

interface SalesUser { id: string; firstName: string; lastName: string; email: string; }

export default function ErpCustomersPage() {
  const { t } = useT();
  const { toast } = useToast();
  const { isAdmin, hasRole } = useRoles();
  const isSalesOnly = !isAdmin && !hasRole("Manager","Accountant","SalesManager") && hasRole("Sales");
  const canCreate   = isAdmin || hasRole("Manager","Accountant");

  const [search,    setSearch]   = useState("");
  const [showForm,  setShowForm]  = useState(false);
  const [editing,   setEditing]  = useState<any>(null);
  const [form,      setForm]     = useState(INIT);
  const [salesRep,  setSalesRep] = useState<{ id: string; name: string } | null>(null);
  const [delTarget, setDel]      = useState<any>(null);
  const [assignTarget, setAssignTarget] = useState<any>(null);
  const [assignRepId, setAssignRepId]   = useState("");

  const { data, isLoading, refetch } = useCustomers({ search: search || undefined });
  const create  = useCreateCustomer();
  const update  = useUpdateCustomer();
  const toggle  = useToggleCustomerStatus();
  const del     = useDeleteCustomer();
  const assign  = useAssignCustomerSalesRep();

  // Fetch sales users for assignment dropdown
  const { data: salesUsers } = useQuery<SalesUser[]>({
    queryKey: ["users-sales"],
    queryFn: () => apiFetch("/users?role=Sales"),
    enabled: canCreate,
  });

  const openEdit = (c: any) => {
    setEditing(c); setSalesRep(c.assignedSalesRepId ? { id: c.assignedSalesRepId, name: c.assignedSalesRepName ?? "" } : null);
    setForm({ code: c.code, name: c.name, phone: c.phone ?? "", email: c.email ?? "", address: c.address ?? "", notes: c.notes ?? "" });
    setShowForm(true);
  };
  const closeForm = () => { setShowForm(false); setEditing(null); setForm(INIT); setSalesRep(null); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const payload = {
        ...form, partnerType: 1,
        assignedSalesRepId: salesRep?.id || null,
        assignedSalesRepName: salesRep?.name || null,
      };
      if (editing) {
        await update.mutateAsync({ id: editing.id, data: payload });
        toast(`"${form.name}" updated.`, "success");
      } else {
        await create.mutateAsync(payload);
        toast(`Customer "${form.name}" created and assigned.`, "success");
      }
      closeForm();
    } catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  const handleToggle = async (c: any) => {
    try { await toggle.mutateAsync(c.id); toast(`"${c.name}" ${c.isActive ? "deactivated" : "activated"}.`, "success"); }
    catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  const handleDelete = async () => {
    if (!delTarget) return;
    try { await del.mutateAsync(delTarget.id); toast(`"${delTarget.name}" deleted.`, "info"); setDel(null); }
    catch (err: any) { toast(err?.message ?? "Failed.", "error"); setDel(null); }
  };

  const handleAssign = async () => {
    if (!assignTarget) return;
    const rep = salesUsers?.find(u => u.id === assignRepId);
    try {
      await assign.mutateAsync({ id: assignTarget.id, salesRepId: assignRepId || null, salesRepName: rep ? `${rep.firstName} ${rep.lastName}` : null });
      toast("Sales representative updated.", "success");
      setAssignTarget(null); setAssignRepId("");
    } catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      {/* Delete confirm */}
      {delTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <div className="flex items-center gap-3 text-red-600"><AlertTriangle className="h-5 w-5" /><h2 className="font-semibold">{t("confirm_delete")}</h2></div>
            <p className="text-sm text-muted-foreground">Delete <strong>{delTarget.name}</strong>?</p>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setDel(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={handleDelete} disabled={del.isPending} className="px-4 py-2 rounded-lg text-sm bg-red-600 text-white hover:bg-red-700 disabled:opacity-50">{del.isPending ? "Deleting…" : "Delete"}</button>
            </div>
          </div>
        </div>
      )}

      {/* Assign Sales Rep modal */}
      {assignTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <div className="flex items-center gap-3 text-blue-600"><UserCheck className="h-5 w-5" /><h2 className="font-semibold">Assign Sales Rep</h2></div>
            <p className="text-sm text-muted-foreground">Customer: <strong>{assignTarget.name}</strong></p>
            <select value={assignRepId} onChange={e => setAssignRepId(e.target.value)} className="input w-full">
              <option value="">— Unassigned —</option>
              {salesUsers?.map(u => <option key={u.id} value={u.id}>{u.firstName} {u.lastName} ({u.email})</option>)}
            </select>
            <div className="flex gap-2 justify-end">
              <button onClick={() => { setAssignTarget(null); setAssignRepId(""); }} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={handleAssign} disabled={assign.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm disabled:opacity-50">{assign.isPending ? "Saving…" : "Save"}</button>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Users className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("customers")}</h1>
          {isSalesOnly && <span className="text-xs text-muted-foreground bg-muted px-2 py-0.5 rounded-full">Showing your assigned customers</span>}
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          {canCreate && (
            <button onClick={showForm ? closeForm : () => setShowForm(true)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
              {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
              {showForm ? t("cancel") : "Add Customer"}
            </button>
          )}
        </div>
      </div>

      {/* Create/Edit form — only for Admin/Accountant/Manager */}
      {showForm && canCreate && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">{editing ? `Edit: ${editing.name}` : "New Customer"}</h2>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">{t("code")} *</label>
              <input required disabled={!!editing} value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} className="input w-full disabled:opacity-50" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("name")} *</label>
              <input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("phone")} *</label>
              <input required value={form.phone} onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("email")} *</label>
              <input required type="email" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} className="input w-full" /></div>
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">Assign to Sales Representative</label>
              <select value={salesRep?.id ?? ""} onChange={e => {
                const rep = salesUsers?.find(u => u.id === e.target.value);
                setSalesRep(rep ? { id: rep.id, name: `${rep.firstName} ${rep.lastName}` } : null);
              }} className="input w-full">
                <option value="">— Unassigned —</option>
                {salesUsers?.map(u => <option key={u.id} value={u.id}>{u.firstName} {u.lastName} ({u.email})</option>)}
              </select>
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
        <input value={search} onChange={e => setSearch(e.target.value)} placeholder={`${t("search")} ${t("customers")}…`} className="input pl-10 w-full" />
      </div>

      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("code")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("name")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("phone")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Sales Rep</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3 text-right"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(c => (
                <tr key={c.id} className={`hover:bg-muted/20 ${!c.isActive ? "opacity-60" : ""}`}>
                  <td className="p-3 font-mono text-xs">{c.code}</td>
                  <td className="p-3 font-semibold">{c.name}</td>
                  <td className="p-3 text-muted-foreground">{c.phone ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">
                    {(c as any).assignedSalesRepName
                      ? <span className="flex items-center gap-1"><UserCheck className="h-3.5 w-3.5 text-emerald-500" />{(c as any).assignedSalesRepName}</span>
                      : <span className="text-muted-foreground/50">Unassigned</span>}
                  </td>
                  <td className="p-3 text-center">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${c.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>
                      {c.isActive ? t("active") : t("inactive")}
                    </span>
                  </td>
                  <td className="p-3 text-right">
                    <div className="flex items-center justify-end gap-1">
                      {canCreate && (
                        <>
                          <button onClick={() => openEdit(c)} className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground" title={t("edit")}><Pencil className="h-3.5 w-3.5" /></button>
                          <button onClick={() => { setAssignTarget(c); setAssignRepId((c as any).assignedSalesRepId ?? ""); }} className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-blue-600" title="Assign Sales Rep"><UserCheck className="h-3.5 w-3.5" /></button>
                          <button onClick={() => handleToggle(c)} className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground" title={c.isActive ? t("deactivate") : t("activate")}><PowerOff className="h-3.5 w-3.5" /></button>
                          {(isAdmin || hasRole("Manager")) && <button onClick={() => setDel(c)} className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600" title={t("delete")}><Trash2 className="h-3.5 w-3.5" /></button>}
                        </>
                      )}
                    </div>
                  </td>
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
