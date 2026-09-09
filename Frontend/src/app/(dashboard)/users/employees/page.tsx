"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { Users, RefreshCw, PowerOff, Shield, Search } from "lucide-react";
import { useState } from "react";

interface SystemUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
  roles: string[];
  createdAt: string;
}

const ROLE_COLORS: Record<string, string> = {
  Admin:        "bg-red-100 text-red-700",
  Manager:      "bg-purple-100 text-purple-700",
  SalesManager: "bg-indigo-100 text-indigo-700",
  Accountant:   "bg-blue-100 text-blue-700",
  Sales:        "bg-emerald-100 text-emerald-700",
  Purchasing:   "bg-amber-100 text-amber-700",
  SupportAgent: "bg-cyan-100 text-cyan-700",
  Delivery:     "bg-orange-100 text-orange-700",
  ReadOnly:     "bg-gray-100 text-gray-600",
};

export default function UsersPage() {
  const qc = useQueryClient();
  const [search, setSearch] = useState("");

  const { data: users, isLoading, refetch } = useQuery({
    queryKey: ["system-users"],
    queryFn: () => apiFetch<SystemUser[]>("/users"),
  });

  const toggle = useMutation({
    mutationFn: (id: string) =>
      apiFetch<{ id: string; isActive: boolean }>(`/users/${id}/toggle-active`, { method: "PATCH" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["system-users"] }),
  });

  const filtered = (users ?? []).filter(u =>
    !search ||
    u.fullName.toLowerCase().includes(search.toLowerCase()) ||
    u.email.toLowerCase().includes(search.toLowerCase()) ||
    u.roles.some(r => r.toLowerCase().includes(search.toLowerCase()))
  );

  const roleGroups = [
    "Admin", "Manager", "SalesManager", "Accountant",
    "Sales", "Purchasing", "SupportAgent", "Delivery", "ReadOnly",
  ];

  const groupCounts = roleGroups.reduce<Record<string, number>>((acc, role) => {
    acc[role] = (users ?? []).filter(u => u.roles.includes(role)).length;
    return acc;
  }, {});

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Users className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">System Users</h1>
            <p className="text-sm text-muted-foreground">{users?.length ?? 0} registered accounts</p>
          </div>
        </div>
        <button onClick={() => refetch()} className="flex items-center gap-2 px-3 py-2 rounded-lg text-sm text-muted-foreground hover:bg-accent">
          <RefreshCw className="h-4 w-4" /> Refresh
        </button>
      </div>

      {/* Role summary chips */}
      <div className="flex flex-wrap gap-2">
        {roleGroups.filter(r => groupCounts[r] > 0).map(role => (
          <span key={role} className={`text-xs font-medium px-2.5 py-1 rounded-full ${ROLE_COLORS[role] ?? "bg-muted text-muted-foreground"}`}>
            {role} ({groupCounts[role]})
          </span>
        ))}
      </div>

      {/* Search */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <input
          value={search}
          onChange={e => setSearch(e.target.value)}
          placeholder="Filter by name, email or role…"
          className="input pl-10 w-full"
        />
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="text-center py-12 text-muted-foreground">Loading users…</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30">
              <tr>
                <th className="text-left p-3 font-medium text-muted-foreground">Name</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Email</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Roles</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {filtered.map(u => (
                <tr key={u.id} className={`hover:bg-muted/20 ${!u.isActive ? "opacity-60" : ""}`}>
                  <td className="p-3">
                    <div className="flex items-center gap-2.5">
                      <div className="h-8 w-8 rounded-full bg-accent flex items-center justify-center text-xs font-semibold shrink-0">
                        {u.firstName?.[0]}{u.lastName?.[0]}
                      </div>
                      <span className="font-medium">{u.fullName || u.email.split("@")[0]}</span>
                    </div>
                  </td>
                  <td className="p-3 text-muted-foreground font-mono text-xs">{u.email}</td>
                  <td className="p-3">
                    <div className="flex flex-wrap gap-1">
                      {u.roles.map(r => (
                        <span key={r} className={`text-xs px-2 py-0.5 rounded-full font-medium flex items-center gap-1 ${ROLE_COLORS[r] ?? "bg-muted text-muted-foreground"}`}>
                          <Shield className="h-2.5 w-2.5" />{r}
                        </span>
                      ))}
                      {u.roles.length === 0 && <span className="text-xs text-muted-foreground">No role</span>}
                    </div>
                  </td>
                  <td className="p-3 text-center">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${u.isActive ? "bg-emerald-100 text-emerald-700" : "bg-red-100 text-red-700"}`}>
                      {u.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className="p-3 text-right">
                    <button
                      onClick={() => toggle.mutate(u.id)}
                      disabled={toggle.isPending}
                      title={u.isActive ? "Deactivate user" : "Activate user"}
                      className="inline-flex items-center gap-1 text-xs px-2 py-1 rounded hover:bg-accent text-muted-foreground hover:text-foreground disabled:opacity-50"
                    >
                      <PowerOff className="h-3.5 w-3.5" />
                      {u.isActive ? "Deactivate" : "Activate"}
                    </button>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr><td colSpan={5} className="p-8 text-center text-muted-foreground">
                  {search ? "No users match your search." : "No users found."}
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
