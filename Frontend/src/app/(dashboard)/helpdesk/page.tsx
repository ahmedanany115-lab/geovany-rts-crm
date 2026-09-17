"use client";
import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useToast } from "@/components/ui/toast";
import { useRoles } from "@/hooks/useRoles";
import { useT } from "@/hooks/useT";
import { LifeBuoy, Plus, X, Check, RefreshCw, MessageSquare, AlertTriangle } from "lucide-react";

const PRIORITY = { 1:"Low",2:"Medium",3:"High",4:"Critical" };
const STATUS   = { 1:"Open",2:"In Progress",3:"Pending",4:"Resolved",5:"Closed" };
const CATEGORY = { 1:"General",2:"Technical",3:"Billing",4:"Delivery",5:"Maintenance",6:"Other" };
const PRIO_CLS: Record<number,string> = { 1:"bg-muted text-muted-foreground",2:"bg-blue-100 text-blue-700",3:"bg-amber-100 text-amber-700",4:"bg-red-100 text-red-700" };
const ST_CLS:   Record<number,string> = { 1:"bg-emerald-100 text-emerald-700",2:"bg-blue-100 text-blue-700",3:"bg-amber-100 text-amber-700",4:"bg-purple-100 text-purple-700",5:"bg-muted text-muted-foreground" };

const INIT = { title:"", description:"", priority:2, category:1, relatedEntityRef:"" };

export default function HelpdeskPage() {
  const { t } = useT();
  const { toast } = useToast();
  const { isAdmin, hasRole } = useRoles();
  const qc = useQueryClient();
  const isAgent = isAdmin || hasRole("Manager","SupportAgent");

  const [showForm, setShowForm] = useState(false);
  const [form, setForm]         = useState(INIT);
  const [selected, setSelected] = useState<any>(null);
  const [comment, setComment]   = useState("");
  const [statusFilter, setStatus] = useState("");

  const { data, isLoading, refetch } = useQuery({
    queryKey: ["helpdesk-tickets", statusFilter],
    queryFn: () => apiFetch<any[]>(`/helpdesk${statusFilter ? `?status=${statusFilter}` : ""}`),
  });

  const createMut = useMutation({
    mutationFn: (d: any) => apiFetch<any>("/helpdesk", { method:"POST", body:JSON.stringify(d) }),
    onSuccess: (r) => { qc.invalidateQueries({ queryKey:["helpdesk-tickets"] }); toast(`Ticket ${r.ticketNumber} created.`,"success"); setShowForm(false); setForm(INIT); },
    onError: (e: any) => toast(e?.message ?? "Failed.","error"),
  });

  const statusMut = useMutation({
    mutationFn: ({ id, status, resolution }: any) => apiFetch<void>(`/helpdesk/${id}/status`,{ method:"PATCH",body:JSON.stringify({ status, resolution }) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey:["helpdesk-tickets"] }); if (selected) refetch(); toast("Status updated.","success"); },
    onError: (e: any) => toast(e?.message ?? "Failed.","error"),
  });

  const commentMut = useMutation({
    mutationFn: ({ id, body }: any) => apiFetch<any>(`/helpdesk/${id}/comments`,{ method:"POST",body:JSON.stringify({ body, isInternal:false }) }),
    onSuccess: () => { setComment(""); toast("Comment added.","success"); },
    onError: (e: any) => toast(e?.message ?? "Failed.","error"),
  });

  return (
    <div className="p-6 space-y-6">
      {/* Detail drawer */}
      {selected && (
        <div className="fixed inset-0 z-50 flex items-center justify-end bg-black/30" onClick={() => setSelected(null)}>
          <div className="bg-background w-full max-w-lg h-screen overflow-y-auto shadow-2xl border-l p-6 space-y-4" onClick={e => e.stopPropagation()}>
            <div className="flex items-center justify-between">
              <h2 className="font-bold">Ticket {selected.ticketNumber}</h2>
              <button onClick={() => setSelected(null)} className="p-1.5 rounded hover:bg-accent"><X className="h-4 w-4" /></button>
            </div>
            <div className="space-y-2 text-sm">
              <p className="font-semibold text-base">{selected.title}</p>
              {selected.description && <p className="text-muted-foreground">{selected.description}</p>}
              <div className="flex gap-2 flex-wrap">
                <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${ST_CLS[selected.status] ?? "bg-muted"}`}>{STATUS[selected.status as keyof typeof STATUS]}</span>
                <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${PRIO_CLS[selected.priority] ?? "bg-muted"}`}>{PRIORITY[selected.priority as keyof typeof PRIORITY]}</span>
                <span className="text-xs px-2 py-0.5 rounded-full bg-muted text-muted-foreground">{CATEGORY[selected.category as keyof typeof CATEGORY]}</span>
              </div>
              <div className="text-xs text-muted-foreground space-y-0.5">
                <p>Reported by: {selected.reportedByName}</p>
                {selected.assignedToName && <p>Assigned to: {selected.assignedToName}</p>}
                {selected.relatedEntityRef && <p>Ref: {selected.relatedEntityRef}</p>}
                <p>Created: {new Date(selected.createdAt).toLocaleString()}</p>
              </div>
            </div>
            {isAgent && selected.status < 4 && (
              <div className="border-t pt-3 space-y-2">
                <p className="text-xs font-semibold text-muted-foreground">Update Status</p>
                <div className="flex gap-2 flex-wrap">
                  {[2,3,4,5].map(s => (
                    <button key={s} onClick={() => statusMut.mutate({ id: selected.id, status: s })}
                      disabled={selected.status === s}
                      className={`text-xs px-3 py-1.5 rounded-lg font-medium disabled:opacity-30 ${ST_CLS[s]}`}>
                      {STATUS[s as keyof typeof STATUS]}
                    </button>
                  ))}
                </div>
              </div>
            )}
            {/* Comment */}
            <div className="border-t pt-3 space-y-2">
              <p className="text-xs font-semibold text-muted-foreground">Add Comment</p>
              <textarea rows={3} value={comment} onChange={e => setComment(e.target.value)}
                className="input w-full resize-none text-sm" placeholder="Type your comment…" />
              <button onClick={() => { if (comment.trim()) commentMut.mutate({ id: selected.id, body: comment.trim() }); }}
                disabled={!comment.trim() || commentMut.isPending}
                className="btn-primary text-xs px-3 py-1.5 rounded-lg disabled:opacity-50">
                {commentMut.isPending ? "Posting…" : "Post Comment"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><LifeBuoy className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Help Desk</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : "New Ticket"}
          </button>
        </div>
      </div>

      {/* Create form */}
      {showForm && (
        <form onSubmit={e => { e.preventDefault(); createMut.mutate(form); }} className="card p-5 space-y-3 border border-primary/20">
          <h2 className="font-semibold text-sm">New Support Ticket</h2>
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Title *</label>
              <input required value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} className="input w-full" placeholder="Briefly describe the issue…" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Priority</label>
              <select value={form.priority} onChange={e => setForm(f => ({ ...f, priority: Number(e.target.value) }))} className="input w-full">
                {Object.entries(PRIORITY).map(([v,l]) => <option key={v} value={v}>{l}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Category</label>
              <select value={form.category} onChange={e => setForm(f => ({ ...f, category: Number(e.target.value) }))} className="input w-full">
                {Object.entries(CATEGORY).map(([v,l]) => <option key={v} value={v}>{l}</option>)}
              </select>
            </div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Description</label>
              <textarea rows={3} value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} className="input w-full resize-none" /></div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Related Record (optional)</label>
              <input value={form.relatedEntityRef} onChange={e => setForm(f => ({ ...f, relatedEntityRef: e.target.value }))} className="input w-full" placeholder="e.g. SO-2025-001, INV-2025-001" /></div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={createMut.isPending} className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
              <Check className="h-4 w-4" />{createMut.isPending ? t("saving") : "Submit Ticket"}
            </button>
            <button type="button" onClick={() => setShowForm(false)} className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}

      {/* Filter */}
      <div className="flex gap-3">
        <select value={statusFilter} onChange={e => setStatus(e.target.value)} className="input text-sm">
          <option value="">All Statuses</option>
          {Object.entries(STATUS).map(([v,l]) => <option key={v} value={v}>{l}</option>)}
        </select>
      </div>

      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Ticket #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Title</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Reporter</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Priority</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Created</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(tk => (
                <tr key={tk.id} className="hover:bg-muted/20 cursor-pointer" onClick={() => setSelected(tk)}>
                  <td className="p-3 font-mono text-xs">{tk.ticketNumber}</td>
                  <td className="p-3 font-medium max-w-xs truncate">{tk.title}</td>
                  <td className="p-3 text-muted-foreground">{tk.reportedByName}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full font-medium ${PRIO_CLS[tk.priority] ?? "bg-muted"}`}>{PRIORITY[tk.priority as keyof typeof PRIORITY]}</span></td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full font-medium ${ST_CLS[tk.status] ?? "bg-muted"}`}>{STATUS[tk.status as keyof typeof STATUS]}</span></td>
                  <td className="p-3 text-muted-foreground text-xs">{new Date(tk.createdAt).toLocaleDateString()}</td>
                  <td className="p-3 text-right">
                    {tk.commentCount > 0 && <span className="text-xs text-muted-foreground flex items-center gap-1 justify-end"><MessageSquare className="h-3.5 w-3.5" />{tk.commentCount}</span>}
                  </td>
                </tr>
              ))}
              {!data?.length && <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">No tickets. Click "New Ticket" to raise one.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
