"use client";
import { Users, Plus, Search, RefreshCw, PowerOff } from "lucide-react";
import { useState } from "react";
import { useCustomers, useCreateCustomer, useToggleCustomerStatus } from "@/features/erp/hooks";

export default function CrmCustomersPage() {
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ code: "", name: "", taxNumber: "", phone: "", email: "", address: "", notes: "" });
  const { data: customers, isLoading, refetch } = useCustomers({ search: search || undefined });
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
        <div className="flex items-center gap-3"><Users className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">CRM — Customers</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            <Plus className="h-4 w-4" />{showForm ? "Cancel" : "Add Customer"}
          </button>
        </div>
      </div>
      {showForm && (
        <form onSubmit={handleCreate} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold">New Customer</h2>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">Code *</label><input required value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Name *</label><input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Tax Number</label><input value={form.taxNumber} onChange={e => setForm(f => ({ ...f, taxNumber: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Phone</label><input value={form.phone} onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Email</label><input type="email" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Address</label><input value={form.address} onChange={e => setForm(f => ({ ...f, address: e.target.value }))} className="input w-full" /></div>
          </div>
          <button type="submit" disabled={create.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">{create.isPending ? "Saving..." : "Save Customer"}</button>
        </form>
      )}
      <div className="relative max-w-md"><Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" /><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search customers..." className="input pl-10 w-full" /></div>
      {isLoading ? <div className="text-center py-10 text-muted-foreground">Loading...</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Code</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Name</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Phone</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Email</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {customers?.map(c => (
                <tr key={c.id} className="hover:bg-muted/20">
                  <td className="p-3 font-mono text-xs text-muted-foreground">{c.code}</td>
                  <td className="p-3 font-medium">{c.name}</td>
                  <td className="p-3 text-muted-foreground">{c.phone ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">{c.email ?? "—"}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${c.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>{c.isActive ? "Active" : "Inactive"}</span></td>
                  <td className="p-3"><button onClick={() => toggle.mutate(c.id)} className="text-muted-foreground hover:text-foreground"><PowerOff className="h-4 w-4" /></button></td>
                </tr>
              ))}
              {!customers?.length && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">No customers yet.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
