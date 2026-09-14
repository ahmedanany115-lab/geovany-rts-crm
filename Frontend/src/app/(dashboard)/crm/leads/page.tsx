"use client";

import { useState, useEffect } from "react";
import { useToast } from "@/components/ui/toast";
import { useT } from "@/hooks/useT";
import { useAuthStore } from "@/stores/auth-store";
import {
  TrendingUp, Plus, X, Trash2, Pencil, Check,
  AlertTriangle, RefreshCw,
} from "lucide-react";

/* ── Types ────────────────────────────────────────────────────────────────── */

type Stage = "new" | "qualified" | "proposal" | "negotiation" | "won" | "lost";

interface Lead {
  id: string;
  name: string;            // contact / company name
  company: string;
  phone: string;
  email: string;
  source: string;          // Where did this lead come from
  estimatedValue: number;
  stage: Stage;
  notes: string;
  owner: string;
  createdAt: string;
}

/* ── Pipeline stages config ─────────────────────────────────────────────── */

const STAGES: { key: Stage; label_en: string; label_ar: string; cls: string; border: string }[] = [
  { key: "new",         label_en: "New",         label_ar: "جديد",         cls: "bg-slate-50",    border: "border-t-slate-400" },
  { key: "qualified",   label_en: "Qualified",   label_ar: "مؤهل",         cls: "bg-blue-50",     border: "border-t-blue-500" },
  { key: "proposal",    label_en: "Proposal",    label_ar: "عرض سعر",      cls: "bg-indigo-50",   border: "border-t-indigo-500" },
  { key: "negotiation", label_en: "Negotiation", label_ar: "تفاوض",        cls: "bg-amber-50",    border: "border-t-amber-500" },
  { key: "won",         label_en: "Won",         label_ar: "فاز",          cls: "bg-emerald-50",  border: "border-t-emerald-500" },
  { key: "lost",        label_en: "Lost",        label_ar: "خُسر",         cls: "bg-red-50",      border: "border-t-red-400" },
];

const SOURCES = ["Website", "Referral", "Cold Call", "Email Campaign", "Social Media", "Exhibition", "Other"];

const STORAGE_KEY = "rts_leads_v2";
const INIT_FORM   = { name: "", company: "", phone: "", email: "", source: "Website", estimatedValue: 0, stage: "new" as Stage, notes: "" };

/* ── Component ───────────────────────────────────────────────────────────── */

export default function LeadsPage() {
  const { t, lang } = useT();
  const { toast }   = useToast();
  const user        = useAuthStore(s => s.user);

  const [leads,      setLeads]      = useState<Lead[]>([]);
  const [showForm,   setShowForm]   = useState(false);
  const [editTarget, setEditTarget] = useState<Lead | null>(null);
  const [delTarget,  setDelTarget]  = useState<Lead | null>(null);
  const [form,       setForm]       = useState(INIT_FORM);
  const [view,       setView]       = useState<"kanban" | "list">("kanban");

  /* persist */
  useEffect(() => {
    try { const s = localStorage.getItem(STORAGE_KEY); if (s) setLeads(JSON.parse(s)); } catch {}
  }, []);

  const save = (updated: Lead[]) => {
    setLeads(updated);
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(updated)); } catch {}
  };

  /* ── open edit ── */
  const openEdit = (lead: Lead) => {
    setEditTarget(lead);
    setForm({ name: lead.name, company: lead.company, phone: lead.phone, email: lead.email, source: lead.source, estimatedValue: lead.estimatedValue, stage: lead.stage, notes: lead.notes });
    setShowForm(true);
  };

  const closeForm = () => { setShowForm(false); setEditTarget(null); setForm(INIT_FORM); };

  /* ── submit ── */
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name.trim()) { toast("Name is required.", "error"); return; }

    if (editTarget) {
      save(leads.map(l => l.id === editTarget.id ? { ...l, ...form, estimatedValue: Number(form.estimatedValue) } : l));
      toast(`"${form.name}" updated.`, "success");
    } else {
      const lead: Lead = {
        id:             crypto.randomUUID(),
        ...form,
        estimatedValue: Number(form.estimatedValue),
        owner:          `${user?.firstName ?? ""} ${user?.lastName ?? ""}`.trim() || user?.email || "Me",
        createdAt:      new Date().toISOString(),
      };
      save([lead, ...leads]);
      toast(`Lead "${form.name}" created.`, "success");
    }
    closeForm();
  };

  /* ── move stage (drag-like via buttons) ── */
  const moveStage = (id: string, stage: Stage) => {
    save(leads.map(l => l.id === id ? { ...l, stage } : l));
  };

  /* ── delete ── */
  const handleDelete = () => {
    if (!delTarget) return;
    save(leads.filter(l => l.id !== delTarget.id));
    toast(`"${delTarget.name}" removed.`, "info");
    setDelTarget(null);
  };

  /* ── computed ── */
  const stageName = (s: Stage) => {
    const cfg = STAGES.find(x => x.key === s);
    return lang === "ar" ? cfg?.label_ar : cfg?.label_en;
  };

  const totalValue  = leads.reduce((s, l) => s + l.estimatedValue, 0);
  const wonValue    = leads.filter(l => l.stage === "won").reduce((s, l) => s + l.estimatedValue, 0);

  return (
    <div className="p-6 space-y-6">

      {/* Delete confirm */}
      {delTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <div className="flex items-center gap-3 text-red-600">
              <AlertTriangle className="h-5 w-5 shrink-0" />
              <h2 className="font-semibold">Delete Lead?</h2>
            </div>
            <p className="text-sm text-muted-foreground">Remove <strong>{delTarget.name}</strong>? This cannot be undone.</p>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setDelTarget(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={handleDelete} className="px-4 py-2 rounded-lg text-sm bg-red-600 text-white hover:bg-red-700">Delete</button>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <TrendingUp className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">{lang === "ar" ? "العملاء المحتملون" : "Leads & Pipeline"}</h1>
            <p className="text-sm text-muted-foreground">
              {leads.length} leads · Pipeline: {totalValue.toLocaleString()} · Won: {wonValue.toLocaleString()}
            </p>
          </div>
        </div>
        <div className="flex gap-2">
          <div className="flex rounded-lg border overflow-hidden text-sm">
            {(["kanban", "list"] as const).map(v => (
              <button key={v} onClick={() => setView(v)}
                className={`px-3 py-1.5 transition-colors ${view === v ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-accent"}`}>
                {v === "kanban" ? "Board" : "List"}
              </button>
            ))}
          </div>
          <button onClick={showForm ? closeForm : () => setShowForm(true)}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : "New Lead"}
          </button>
        </div>
      </div>

      {/* Form */}
      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">{editTarget ? `Edit: ${editTarget.name}` : "New Lead"}</h2>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">Name *</label>
              <input required value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} className="input w-full" placeholder="Contact / Company" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Company</label>
              <input value={form.company} onChange={e => setForm(f => ({ ...f, company: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Phone</label>
              <input value={form.phone} onChange={e => setForm(f => ({ ...f, phone: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Email</label>
              <input type="email" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Source</label>
              <select value={form.source} onChange={e => setForm(f => ({ ...f, source: e.target.value }))} className="input w-full">
                {SOURCES.map(s => <option key={s}>{s}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Est. Value (EGP)</label>
              <input type="number" min="0" step="100" value={form.estimatedValue} onChange={e => setForm(f => ({ ...f, estimatedValue: Number(e.target.value) }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Stage</label>
              <select value={form.stage} onChange={e => setForm(f => ({ ...f, stage: e.target.value as Stage }))} className="input w-full">
                {STAGES.map(s => <option key={s.key} value={s.key}>{s.label_en}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Notes</label>
              <input value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" /></div>
          </div>
          <div className="flex gap-2">
            <button type="submit" className="btn-primary px-5 py-2 rounded-lg text-sm flex items-center gap-2">
              <Check className="h-4 w-4" />{t("save")}
            </button>
            <button type="button" onClick={closeForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}

      {/* Kanban board */}
      {view === "kanban" && (
        <div className="flex gap-3 overflow-x-auto pb-4">
          {STAGES.map(stage => {
            const stageLeads = leads.filter(l => l.stage === stage.key);
            const stageTotal = stageLeads.reduce((s, l) => s + l.estimatedValue, 0);
            return (
              <div key={stage.key} className={`flex flex-col rounded-lg border border-t-4 ${stage.border} ${stage.cls} min-w-[220px] w-[220px] overflow-hidden shrink-0`}>
                <div className="px-3 py-2 border-b bg-background/80 flex items-center justify-between">
                  <span className="font-semibold text-sm">{lang === "ar" ? stage.label_ar : stage.label_en}</span>
                  <div className="flex items-center gap-1.5">
                    <span className="text-xs text-muted-foreground bg-muted px-1.5 py-0.5 rounded-full">{stageLeads.length}</span>
                  </div>
                </div>
                {stageTotal > 0 && (
                  <div className="px-3 py-1 text-xs text-muted-foreground border-b bg-background/60">
                    {stageTotal.toLocaleString()} EGP
                  </div>
                )}
                <div className="flex-1 overflow-y-auto p-2 space-y-2 max-h-[600px]">
                  {stageLeads.map(lead => (
                    <div key={lead.id} className="bg-background rounded-md border p-3 shadow-sm group space-y-1.5">
                      <div className="flex items-start justify-between gap-1">
                        <div className="min-w-0">
                          <p className="font-semibold text-sm leading-tight truncate">{lead.name}</p>
                          {lead.company && <p className="text-xs text-muted-foreground truncate">{lead.company}</p>}
                        </div>
                        <div className="flex gap-0.5 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
                          <button onClick={() => openEdit(lead)} title="Edit" className="p-1 rounded hover:bg-accent text-muted-foreground hover:text-foreground"><Pencil className="h-3 w-3" /></button>
                          <button onClick={() => setDelTarget(lead)} title="Delete" className="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600"><Trash2 className="h-3 w-3" /></button>
                        </div>
                      </div>
                      {lead.estimatedValue > 0 && (
                        <p className="text-xs font-semibold text-emerald-600">{lead.estimatedValue.toLocaleString()} EGP</p>
                      )}
                      {lead.phone && <p className="text-xs text-muted-foreground">{lead.phone}</p>}
                      {lead.notes && <p className="text-xs text-muted-foreground line-clamp-2 italic">{lead.notes}</p>}
                      {/* Move buttons */}
                      <div className="flex gap-1 flex-wrap pt-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        {STAGES.filter(s => s.key !== stage.key).map(s => (
                          <button key={s.key} onClick={() => moveStage(lead.id, s.key)}
                            className="text-xs px-1.5 py-0.5 rounded bg-muted hover:bg-accent text-muted-foreground hover:text-foreground truncate max-w-[70px]">
                            → {s.label_en}
                          </button>
                        ))}
                      </div>
                    </div>
                  ))}
                  {stageLeads.length === 0 && (
                    <div className="text-center py-6 text-xs text-muted-foreground/40">Drop leads here</div>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* List view */}
      {view === "list" && (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Name</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Company</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Source</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Est. Value</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Stage</th>
              <th className="p-3 text-right text-muted-foreground">{t("actions")}</th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {leads.length === 0 && (
                <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">No leads yet. Click "New Lead" to start.</td></tr>
              )}
              {leads.map(lead => {
                const stageCfg = STAGES.find(s => s.key === lead.stage);
                return (
                  <tr key={lead.id} className="hover:bg-muted/20">
                    <td className="p-3 font-semibold">{lead.name}</td>
                    <td className="p-3 text-muted-foreground">{lead.company || "—"}</td>
                    <td className="p-3 text-muted-foreground">{lead.source}</td>
                    <td className="p-3 text-right tabular-nums">{lead.estimatedValue > 0 ? lead.estimatedValue.toLocaleString() : "—"}</td>
                    <td className="p-3 text-center">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium border ${stageCfg?.border.replace("border-t-", "border-")}`}>
                        {lang === "ar" ? stageCfg?.label_ar : stageCfg?.label_en}
                      </span>
                    </td>
                    <td className="p-3 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button onClick={() => openEdit(lead)} title={t("edit")} className="p-1.5 rounded hover:bg-accent text-muted-foreground hover:text-foreground"><Pencil className="h-3.5 w-3.5" /></button>
                        <button onClick={() => setDelTarget(lead)} title={t("delete")} className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600"><Trash2 className="h-3.5 w-3.5" /></button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
