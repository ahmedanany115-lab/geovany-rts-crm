"use client";

import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useRoles } from "@/hooks/useRoles";
import { useRouter } from "next/navigation";
import { Activity, RefreshCw, ChevronLeft, ChevronRight, Filter, Search, X } from "lucide-react";

interface AuditRow {
  id: string; userId?: string; userName: string; userEmail: string;
  action: string; module: string; entityType?: string; entityId?: string;
  entityName?: string; reference?: string; status: string; details?: string;
  ipAddress?: string; occurredAt: string;
}

const STATUS_CLS: Record<string, string> = {
  Success: "bg-emerald-100 text-emerald-700",
  Failed:  "bg-red-100 text-red-700",
  Warning: "bg-amber-100 text-amber-700",
};

const ACTION_ICONS: Record<string, string> = {
  Login: "🔑", Logout: "🚪", "Login Failed": "🚫",
  Created: "✨", Updated: "✏️", Deleted: "🗑️",
  Submitted: "📤", Approved: "✅", Rejected: "❌",
  Uploaded: "📂", Downloaded: "⬇️", Posted: "📮",
  Completed: "🏁", Cancelled: "🚫", Converted: "🔄",
};

function timeAgo(iso: string) {
  const ms = Date.now() - new Date(iso).getTime();
  const m = Math.floor(ms / 60000), h = Math.floor(m / 60), d = Math.floor(h / 24);
  if (d > 0) return `${d}d ago`; if (h > 0) return `${h}h ago`;
  if (m > 0) return `${m}m ago`; return "just now";
}

export default function UserActivityPage() {
  const { isAdmin } = useRoles();
  const router = useRouter();
  useEffect(() => { if (!isAdmin) router.replace("/erp/dashboard"); }, [isAdmin, router]);

  const [page, setPage] = useState(1);
  const PAGE_SIZE = 50;
  const [userId, setUserId] = useState(""); const [module, setModule] = useState("");
  const [action, setAction] = useState(""); const [status, setStatus] = useState("");
  const [fromDate, setFromDate] = useState(""); const [toDate, setToDate] = useState("");
  const [search, setSearch] = useState(""); const [selected, setSelected] = useState<AuditRow | null>(null);

  const { data: users }   = useQuery({ queryKey: ["audit-users"],   queryFn: () => apiFetch<any[]>("/audit/users"),   enabled: isAdmin });
  const { data: modules } = useQuery({ queryKey: ["audit-modules"], queryFn: () => apiFetch<string[]>("/audit/modules"), enabled: isAdmin });

  const qs = new URLSearchParams({ ...(userId?{userId}:{}), ...(module?{module}:{}), ...(action?{action}:{}),
    ...(status?{status}:{}), ...(fromDate?{fromDate}:{}), ...(toDate?{toDate}:{}), ...(search?{search}:{}),
    page: String(page), pageSize: String(PAGE_SIZE) });

  const { data, isLoading, refetch } = useQuery({
    queryKey: ["audit-logs", qs.toString()],
    queryFn: () => apiFetch<{ total: number; rows: AuditRow[] }>(`/audit?${qs}`),
    enabled: isAdmin, staleTime: 10000,
  });

  const totalPages = data ? Math.ceil(data.total / PAGE_SIZE) : 0;
  if (!isAdmin) return null;

  const clearFilters = () => { setUserId(""); setModule(""); setAction(""); setStatus(""); setFromDate(""); setToDate(""); setSearch(""); setPage(1); };

  return (
    <div className="p-6 space-y-6">
      {/* Detail drawer */}
      {selected && (
        <div className="fixed inset-0 z-50 flex items-center justify-end bg-black/30" onClick={() => setSelected(null)}>
          <div className="bg-background w-full max-w-md h-screen overflow-y-auto shadow-2xl border-l p-6 space-y-4" onClick={e => e.stopPropagation()}>
            <div className="flex items-center justify-between">
              <h2 className="font-bold text-lg">Activity Details</h2>
              <button onClick={() => setSelected(null)} className="p-1.5 rounded hover:bg-accent"><X className="h-4 w-4" /></button>
            </div>
            <div className="space-y-3 text-sm">
              {[["User",`${selected.userName} (${selected.userEmail})`],["Action",`${ACTION_ICONS[selected.action]??"📋"} ${selected.action}`],
                ["Module",selected.module],["Record",selected.entityName??"—"],["Reference",selected.reference??"—"],
                ["Status",selected.status],["Date/Time",new Date(selected.occurredAt).toLocaleString()],["IP",selected.ipAddress??"—"]
              ].map(([l,v])=>(
                <div key={l} className="flex gap-3"><span className="text-muted-foreground w-24 shrink-0">{l}</span><span className="font-medium break-all">{v}</span></div>
              ))}
              {selected.details && (<div><p className="text-muted-foreground mb-1">Details</p><pre className="rounded bg-muted px-3 py-2 text-xs overflow-x-auto whitespace-pre-wrap">{selected.details}</pre></div>)}
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Activity className="h-6 w-6 text-primary" />
          <div><h1 className="text-2xl font-semibold">User Activity</h1>
            <p className="text-sm text-muted-foreground">{data ? `${data.total.toLocaleString()} total events` : "Loading…"}</p>
          </div>
        </div>
        <button onClick={() => refetch()} disabled={isLoading} className="btn-ghost p-2 rounded-lg disabled:opacity-50">
          <RefreshCw className={`h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
        </button>
      </div>

      {/* Filters */}
      <div className="card p-4 space-y-3">
        <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground"><Filter className="h-4 w-4" /> Filters</div>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          <select value={userId} onChange={e=>{setUserId(e.target.value);setPage(1)}} className="input text-sm">
            <option value="">All Users</option>
            {users?.map((u:any)=><option key={u.userId} value={u.userId}>{u.userName||u.userEmail}</option>)}
          </select>
          <select value={module} onChange={e=>{setModule(e.target.value);setPage(1)}} className="input text-sm">
            <option value="">All Modules</option>
            {modules?.map((m:string)=><option key={m}>{m}</option>)}
          </select>
          <select value={action} onChange={e=>{setAction(e.target.value);setPage(1)}} className="input text-sm">
            <option value="">All Actions</option>
            {["Login","Logout","Created","Updated","Deleted","Submitted","Approved","Rejected","Uploaded","Downloaded","Posted","Completed"].map(a=><option key={a}>{a}</option>)}
          </select>
          <select value={status} onChange={e=>{setStatus(e.target.value);setPage(1)}} className="input text-sm">
            <option value="">All Statuses</option>
            <option>Success</option><option>Failed</option><option>Warning</option>
          </select>
          <input type="date" value={fromDate} onChange={e=>{setFromDate(e.target.value);setPage(1)}} className="input text-sm" />
          <input type="date" value={toDate}   onChange={e=>{setToDate(e.target.value);setPage(1)}}   className="input text-sm" />
          <div className="relative col-span-2">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <input value={search} onChange={e=>{setSearch(e.target.value);setPage(1)}} placeholder="Search…" className="input pl-9 w-full text-sm" />
          </div>
        </div>
        {(userId||module||action||status||fromDate||toDate||search) && (
          <button onClick={clearFilters} className="text-xs text-primary hover:underline flex items-center gap-1"><X className="h-3 w-3"/>Clear filters</button>
        )}
      </div>

      {/* Table */}
      {isLoading ? <div className="text-center py-12 text-muted-foreground">Loading activity…</div> : (
        <>
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30"><tr>
                <th className="text-left p-3 font-medium text-muted-foreground">User</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Action</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Module</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Record</th>
                <th className="text-left p-3 font-medium text-muted-foreground">When</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
              </tr></thead>
              <tbody className="divide-y divide-border">
                {data?.rows.length === 0 && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">No activity yet. Actions across the ERP will appear here.</td></tr>}
                {data?.rows.map(row=>(
                  <tr key={row.id} onClick={()=>setSelected(row)} className="hover:bg-muted/20 cursor-pointer">
                    <td className="p-3"><p className="font-medium">{row.userName||"—"}</p><p className="text-xs text-muted-foreground">{row.userEmail}</p></td>
                    <td className="p-3"><span className="flex items-center gap-1.5"><span>{ACTION_ICONS[row.action]??"📋"}</span><span className="font-medium">{row.action}</span></span></td>
                    <td className="p-3 text-muted-foreground">{row.module}</td>
                    <td className="p-3"><p className="truncate max-w-[180px]" title={row.entityName}>{row.entityName??"—"}</p>{row.reference&&<p className="text-xs text-muted-foreground font-mono">{row.reference}</p>}</td>
                    <td className="p-3"><p>{timeAgo(row.occurredAt)}</p><p className="text-xs text-muted-foreground">{new Date(row.occurredAt).toLocaleString()}</p></td>
                    <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full font-medium ${STATUS_CLS[row.status]??"bg-muted text-muted-foreground"}`}>{row.status}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {totalPages > 1 && (
            <div className="flex items-center justify-between text-sm">
              <p className="text-muted-foreground">Page {page} of {totalPages} · {data?.total.toLocaleString()} events</p>
              <div className="flex gap-1">
                <button onClick={()=>setPage(p=>Math.max(1,p-1))} disabled={page<=1} className="p-2 rounded hover:bg-accent disabled:opacity-30"><ChevronLeft className="h-4 w-4"/></button>
                <button onClick={()=>setPage(p=>Math.min(totalPages,p+1))} disabled={page>=totalPages} className="p-2 rounded hover:bg-accent disabled:opacity-30"><ChevronRight className="h-4 w-4"/></button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
