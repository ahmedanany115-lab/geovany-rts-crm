"use client";

import { useState } from "react";
import { Plus, RefreshCw, PowerOff, Search, Users } from "lucide-react";
import { useCustomers, useCreateCustomer, useToggleCustomerStatus } from "@/features/erp/hooks";

function StatusBadge({ active }: { active: boolean }) {
  return (
    <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${
      active ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400"
             : "bg-muted text-muted-foreground"
    }`}>
      {active ? "Active" : "Inactive"}
    </span>
  );
}

export default function CustomersPage() {
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ code: "", name: "", taxNumber: "", phone: "", email: "", creditLimit: "" });

  const { data: customers, isLoading, refetch } = useCustomers({ search: search || undefined });
  const create = useCreateCustomer();
  const toggle = useToggleCustomerStatus();

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    await create.mutateAsync({
      code: form.code,
      name: form.name,
      taxNumber: form.taxNumber || undefined,
      phone: form.phone || undefined,
      email: form.email || undefined,
      creditLimit: form.creditLimit ? Number(form.creditLimit) : undefined,
      partnerType: 1,
    });
    setForm({ code: "", name: "", taxNumber: "", phone: "", email: "", creditLimit: "" });
    setShowForm(false);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Users className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">Customers</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg">
            <RefreshCw className="h-4 w-4" />
          </button>
          <button onClick={() => setShowForm(true)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            <Plus className="h-4 w-4" /> Add Customer
          </button>
        </div>
      </div>

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <input
          value={search}
          onChange={e => setSearch(e.target.value)}
          placeholder="Search customers..."
          className="input pl-10 w-full"
        />
      </div>

      {/* Create Form */}
      {showForm && (
        <form onSubmit={handleCreate} className="card p-4 space-y-3 border border-primary/20">
          <h2 className="font-medium">New Customer</h2>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-sm text-muted-foreground">Code *</label>
              <input required value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} className="input w-full mt-1" placeholder="CUST001" />
            </div>
            <div>
              <label className="text-sm text-muted-foreground">Name *</label>
              <input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full mt-1" placeholder="Company Name" />
            </div>
            <div>
              <label className="text-sm text-muted-foreground">Tax/VAT Number</label>
              <input value={form.taxNumber} onChange={e => setForm(f => ({ ...f, taxNumber: e.target.value }))} className="input w-full mt-1" placeholder="123456789" />
            </div>
            <div>
              <label className="text-sm text-muted-foreground">Phone</label>
              <input value={form.phone} onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} className="input w-full mt-1" placeholder="+20 100 000 0000" />
            </div>
            <div>
              <label className="text-sm text-muted-foreground">Email</label>
              <input type="email" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} className="input w-full mt-1" />
            </div>
            <div>
              <label className="text-sm text-muted-foreground">Credit Limit (EGP)</label>
              <input type="number" value={form.creditLimit} onChange={e => setForm(f => ({ ...f, creditLimit: e.target.value }))} className="input w-full mt-1" placeholder="50000" />
            </div>
          </div>
          <div className="flex gap-2 pt-2">
            <button type="submit" disabled={create.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">
              {create.isPending ? "Saving..." : "Save Customer"}
            </button>
            <button type="button" onClick={() => setShowForm(false)} className="btn-ghost px-4 py-2 rounded-lg text-sm">
              Cancel
            </button>
          </div>
        </form>
      )}

      {/* Table */}
      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">Loading customers...</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30">
              <tr>
                <th className="text-left p-3 font-medium text-muted-foreground">Code</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Name</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Phone</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Tax Number</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Credit Limit</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
                <th className="p-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {customers?.map(c => (
                <tr key={c.id} className="hover:bg-muted/20 transition-colors">
                  <td className="p-3 font-mono text-xs text-muted-foreground">{c.code}</td>
                  <td className="p-3 font-medium">{c.name}</td>
                  <td className="p-3 text-muted-foreground">{c.phone ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">{c.taxNumber ?? "—"}</td>
                  <td className="p-3 text-right tabular-nums">
                    {c.creditLimit != null ? c.creditLimit.toLocaleString("en-EG", { style: "currency", currency: "EGP" }) : "—"}
                  </td>
                  <td className="p-3 text-center"><StatusBadge active={c.isActive} /></td>
                  <td className="p-3">
                    <button
                      onClick={() => toggle.mutate(c.id)}
                      className="text-muted-foreground hover:text-foreground transition-colors"
                      title={c.isActive ? "Deactivate" : "Activate"}
                    >
                      <PowerOff className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ))}
              {!customers?.length && (
                <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">No customers found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
