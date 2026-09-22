"use client";
import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { useRoles } from "@/hooks/useRoles";
import { useAuthStore } from "@/stores/auth-store";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { Users, RefreshCw, PowerOff, Plus, X, Check, AlertTriangle, Search } from "lucide-react";
import { useToast } from "@/components/ui/toast";

interface SystemUser {
  id: string; email: string; firstName: string; lastName: string;
  fullName: string; isActive: boolean; roles: string[]; createdAt: string;
}

const ALL_ROLES = [
  "Admin","Manager","SalesManager","Accountant","Sales",
  "Purchasing","SupportAgent","Delivery","Marketing","Office","ReadOnly",
];

const ROLE_COLORS: Record<string, string> = {
  Admin: "bg-red-100 text-red-700",       Manager: "bg-purple-100 text-purple-700",
  SalesManager: "bg-indigo-100 text-indigo-700", Accountant: "bg-blue-100 text-blue-700",
  Sales: "bg-emerald-100 text-emerald-700", Purchasing: "bg-amber-100 text-amber-700",
  SupportAgent: "bg-cyan-100 text-cyan-700", Delivery: "bg-orange-100 text-orange-700",
  Marketing: "bg-pink-100 text-pink-700",  Office: "bg-yellow-100 text-yellow-700",
  ReadOnly: "bg-gray-100 text-gray-600",
};

const INIT_FORM = { email: "", password: "", role: "Sales", firstName: "", lastName: "", jobTitle: "", department: "" };

export default function UsersPage() {
  const { t } = useT();
  const { toast } = useToast();
  const { isAdmin } = useRoles();
  const { isInitializing } = useAuthStore();
  const router = useRouter();
  const qc = useQueryClient();

  const [search, setSearch]     = useState("");
  const [showForm, setShowForm] = useState(false);
  const [form, setForm]         = useState(INIT_FORM);
  const [formErr, setFormErr]   = useState<string | null>(null);

  useEffect(() => {
    if (!isInitializing && !isAdmin) router.replace("/dashboard");
  }, [isInitializing, isAdmin, router]);

  if (isInitializing) return <div className="flex items-center justify-center min-h-[60vh] text-muted-foreground">{t("loading")}</div>;
  if (!isAdmin) return null;

  const { data: users, isLoading, refetch } = useQuery({
    queryKey: ["users"],
    queryFn: () => apiFetch<SystemUser[]>("/users"),
    enabled: isAdmin,
  });

  const toggleMut = useMutation({
    mutationFn: (id: string) => apiFetch<{ id: string; isActive: boolean }>(`/users/${id}/toggle-active`, { method: "PATCH" }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["users"] }); toast("User status updated.", "success"); },
    onError: (e: any) => toast(e?.message ?? "Failed.", "error"),
  });

  const createMut = useMutation({
    mutationFn: (data: typeof INIT_FORM) => apiFetch<any>("/users", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: (r) => {
      qc.invalidateQueries({ queryKey: ["users"] });
      toast(`User ${r.email} created with role ${r.role}.`, "success");
      setShowForm(false); setForm(INIT_FORM); setFormErr(null);
    },
    onError: (e: any) => { const msg = e?.message ?? "Failed."; setFormErr(msg); toast(msg, "error"); },
  });

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault(); setFormErr(null);
    if (!form.email.trim())    { setFormErr("Email is required."); return; }
    if (!form.password.trim()) { setFormErr("Password is required (min 6 chars)."); return; }
    if (form.password.length < 6) { setFormErr("Password must be at least 6 characters."); return; }
    createMut.mutate(form);
  };

  const filtered = users?.filter(u =>
    !search ||
    u.fullName.toLowerCase().includes(search.toLowerCase()) ||
    u.email.toLowerCase().includes(search.toLowerCase()) ||
    u.roles.some(r => r.toLowerCase().includes(search.toLowerCase()))
  );

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex items-center gap-3">
          <Users className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">{t("users")}</h1>
          <span className="text-xs text-muted-foreground bg-muted px-2 py-0.5 rounded-full">{users?.length ?? 0} accounts</span>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => { setShowForm(v => !v); setFormErr(null); }}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : "Create User"}
          </button>
        </div>
      </div>

      {/* Create form */}
      {showForm && (
        <form onSubmit={handleCreate} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">New System User</h2>
          {formErr && (
            <div className="flex items-center gap-2 bg-red-50 border border-red-200 rounded-md px-4 py-2 text-sm text-red-700">
              <AlertTriangle className="h-4 w-4 shrink-0" />{formErr}
            </div>
          )}
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Email *</label>
              <input type="email" required value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Role *</label>
              <select required value={form.role} onChange={e => setForm(f => ({ ...f, role: e.target.value }))} className="input w-full">
                {ALL_ROLES.map(r => <option key={r} value={r}>{r}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">First Name</label>
              <input value={form.firstName} onChange={e => setForm(f => ({ ...f, firstName: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Last Name</label>
              <input value={form.lastName} onChange={e => setForm(f => ({ ...f, lastName: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Password *</label>
              <input type="password" required value={form.password} onChange={e => setForm(f => ({ ...f, password: e.target.value }))} className="input w-full" autoComplete="new-password" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Job Title</label>
              <input value={form.jobTitle} onChange={e => setForm(f => ({ ...f, jobTitle: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Department</label>
              <input value={form.department} onChange={e => setForm(f => ({ ...f, department: e.target.value }))} className="input w-full" /></div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={createMut.isPending}
              className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
              <Check className="h-4 w-4" />{createMut.isPending ? "Creating…" : "Create User"}
            </button>
            <button type="button" onClick={() => { setShowForm(false); setFormErr(null); setForm(INIT_FORM); }}
              className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}

      {/* Search */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <input value={search} onChange={e => setSearch(e.target.value)}
          placeholder="Search users…" className="input pl-10 w-full" />
      </div>

      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Name</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Email</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Role(s)</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {filtered?.map(u => (
                <tr key={u.id} className={`hover:bg-muted/20 ${!u.isActive ? "opacity-60" : ""}`}>
                  <td className="p-3 font-semibold">{u.fullName || "—"}</td>
                  <td className="p-3 text-muted-foreground text-xs">{u.email}</td>
                  <td className="p-3">
                    <div className="flex flex-wrap gap-1">
                      {u.roles.map(r => (
                        <span key={r} className={`text-xs px-2 py-0.5 rounded-full font-medium ${ROLE_COLORS[r] ?? "bg-muted"}`}>{r}</span>
                      ))}
                    </div>
                  </td>
                  <td className="p-3 text-center">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${u.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>
                      {u.isActive ? t("active") : t("inactive")}
                    </span>
                  </td>
                  <td className="p-3 text-right">
                    <button onClick={() => toggleMut.mutate(u.id)} disabled={toggleMut.isPending}
                      title={u.isActive ? t("deactivate") : t("activate")}
                      className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground disabled:opacity-50">
                      <PowerOff className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              ))}
              {!filtered?.length && <tr><td colSpan={5} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
