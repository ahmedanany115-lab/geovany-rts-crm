"use client";
import { useState } from "react";
import { Truck, RefreshCw, PowerOff, Search } from "lucide-react";
import { useSuppliers, useToggleSupplierStatus } from "@/features/erp/hooks";

export default function SuppliersPage() {
  const [search, setSearch] = useState("");
  const { data: suppliers, isLoading, refetch } = useSuppliers({ search: search || undefined });
  const toggle = useToggleSupplierStatus();
  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Truck className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Suppliers</h1></div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      <div className="relative max-w-md"><Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" /><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search suppliers..." className="input pl-10 w-full" /></div>
      {isLoading ? <div className="text-center py-10 text-muted-foreground">Loading suppliers...</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr><th className="text-left p-3 font-medium text-muted-foreground">Code</th><th className="text-left p-3 font-medium text-muted-foreground">Name</th><th className="text-left p-3 font-medium text-muted-foreground">Phone</th><th className="text-left p-3 font-medium text-muted-foreground">Tax Number</th><th className="text-center p-3 font-medium text-muted-foreground">Status</th><th className="p-3"></th></tr></thead>
            <tbody className="divide-y divide-border">
              {suppliers?.map(s => (
                <tr key={s.id} className="hover:bg-muted/20 transition-colors">
                  <td className="p-3 font-mono text-xs text-muted-foreground">{s.code}</td>
                  <td className="p-3 font-medium">{s.name}</td>
                  <td className="p-3 text-muted-foreground">{s.phone ?? "—"}</td>
                  <td className="p-3 text-muted-foreground">{s.taxNumber ?? "—"}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${s.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>{s.isActive ? "Active" : "Inactive"}</span></td>
                  <td className="p-3"><button onClick={() => toggle.mutate(s.id)} className="text-muted-foreground hover:text-foreground"><PowerOff className="h-4 w-4" /></button></td>
                </tr>
              ))}
              {!suppliers?.length && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">No suppliers found</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
