"use client";

import { useState } from "react";
import {
  useJournalEntries,
  useCreateJournalEntry,
  usePostJournalEntry,
  useReverseJournalEntry,
  useAccounts,
} from "@/features/finance/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import {
  FileText, RefreshCw, Plus, X, Check, Trash2,
  AlertTriangle, ChevronDown, ChevronUp, RotateCcw,
} from "lucide-react";
import type { AccountDto } from "@/features/finance/types";

/* ── Status config ─────────────────────────────────────────────────────────── */
const STATUS = {
  1: { label: "Draft",    cls: "bg-amber-100 text-amber-700" },
  2: { label: "Posted",   cls: "bg-emerald-100 text-emerald-700" },
  3: { label: "Reversed", cls: "bg-red-100 text-red-700" },
};

/* ── Line type ─────────────────────────────────────────────────────────────── */
interface JeLine { accountId: string; debit: number; credit: number; description: string; }
const EMPTY_LINE: JeLine = { accountId: "", debit: 0, credit: 0, description: "" };

export default function JournalEntriesPage() {
  const { t } = useT();
  const { toast } = useToast();

  /* ── filters ── */
  const [params, setParams] = useState<{ status?: number; fromDate?: string; toDate?: string }>({});
  const { data, isLoading, refetch } = useJournalEntries(params);
  const { data: accounts } = useAccounts();
  const { data: currencies } = useCurrencies();

  /* ── mutations ── */
  const createJE  = useCreateJournalEntry();
  const postJE    = usePostJournalEntry();
  const reverseJE = useReverseJournalEntry();

  /* ── form state ── */
  const [showForm,  setShowForm]  = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [entryDate, setEntryDate] = useState(new Date().toISOString().split("T")[0]);
  const [description, setDescription] = useState("");
  const [currencyId, setCurrencyId]   = useState("");
  const [postNow, setPostNow]         = useState(false);
  const [lines, setLines]             = useState<JeLine[]>([{ ...EMPTY_LINE }, { ...EMPTY_LINE }]);

  /* ── reverse modal ── */
  const [reversingId, setReversingId]     = useState<string | null>(null);
  const [reversalReason, setReversalReason] = useState("");
  const [reversalDate, setReversalDate]   = useState(new Date().toISOString().split("T")[0]);

  /* ── expanded row ── */
  const [expandedId, setExpandedId] = useState<string | null>(null);

  /* ── derived totals ── */
  const totalDebit  = lines.reduce((s, l) => s + (Number(l.debit)  || 0), 0);
  const totalCredit = lines.reduce((s, l) => s + (Number(l.credit) || 0), 0);
  const diff        = Math.abs(totalDebit - totalCredit);
  const balanced    = diff < 0.001;

  const setLine = (i: number, field: keyof JeLine, val: string | number) =>
    setLines(prev => prev.map((l, idx) => idx === i ? { ...l, [field]: val } : l));

  const removeLine = (i: number) =>
    setLines(prev => prev.length > 2 ? prev.filter((_, idx) => idx !== i) : prev);

  const resetForm = () => {
    setShowForm(false); setFormError(null);
    setEntryDate(new Date().toISOString().split("T")[0]);
    setDescription(""); setCurrencyId(""); setPostNow(false);
    setLines([{ ...EMPTY_LINE }, { ...EMPTY_LINE }]);
  };

  /* ── submit create ── */
  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!description.trim())  { setFormError("Description is required."); return; }
    if (!currencyId)           { setFormError("Select a currency."); return; }
    if (!balanced)             { setFormError(`Entry is not balanced. Difference: ${diff.toFixed(2)}`); return; }
    if (lines.some(l => !l.accountId)) { setFormError("All lines need an account."); return; }
    if (lines.some(l => l.debit === 0 && l.credit === 0)) { setFormError("Each line must have a debit or credit amount."); return; }

    try {
      const result = await createJE.mutateAsync({
        entryDate, description: description.trim(),
        currencyId, exchangeRate: 1, postImmediately: postNow,
        lines: lines.map((l, i) => ({
          accountId:   l.accountId,
          debit:       Number(l.debit)  || 0,
          credit:      Number(l.credit) || 0,
          description: l.description || undefined,
          sortOrder:   i + 1,
        })),
      });
      toast(`Journal Entry ${(result as any).entryNumber ?? ""} created${postNow ? " and posted." : "."}`, "success");
      resetForm();
    } catch (err: any) {
      const msg = err?.message ?? "Failed to create entry.";
      setFormError(msg); toast(msg, "error");
    }
  };

  /* ── post ── */
  const handlePost = async (id: string, num: string) => {
    try {
      await postJE.mutateAsync(id);
      toast(`${num} posted to ledger.`, "success");
    } catch (err: any) { toast(err?.message ?? "Failed to post.", "error"); }
  };

  /* ── reverse ── */
  const handleReverse = async () => {
    if (!reversingId) return;
    try {
      await reverseJE.mutateAsync({ id: reversingId, reason: reversalReason, reversalDate });
      toast("Reversal entry created.", "success");
      setReversingId(null); setReversalReason(""); setReversalDate(new Date().toISOString().split("T")[0]);
    } catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  /* ── posting accounts only ── */
  const postingAccounts = (accounts ?? []).filter((a: AccountDto) => !a.isGroup && a.isActive);

  return (
    <div className="p-6 space-y-6">

      {/* Reverse modal */}
      {reversingId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <div className="flex items-center gap-3 text-amber-600">
              <RotateCcw className="h-5 w-5" /><h2 className="font-semibold">Reverse Entry</h2>
            </div>
            <div className="space-y-3">
              <div><label className="text-xs text-muted-foreground block mb-1">Reversal Date *</label>
                <input type="date" value={reversalDate} onChange={e => setReversalDate(e.target.value)} className="input w-full" /></div>
              <div><label className="text-xs text-muted-foreground block mb-1">Reason *</label>
                <input value={reversalReason} onChange={e => setReversalReason(e.target.value)} className="input w-full" placeholder="Reason for reversal" /></div>
            </div>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setReversingId(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={handleReverse} disabled={!reversalReason.trim() || reverseJE.isPending}
                className="px-4 py-2 rounded-lg text-sm bg-amber-600 text-white hover:bg-amber-700 disabled:opacity-50">
                {reverseJE.isPending ? "Reversing…" : "Create Reversal"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <FileText className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">{t("journal_entries")}</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={showForm ? resetForm : () => setShowForm(true)}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : "Add Journal Entry"}
          </button>
        </div>
      </div>

      {/* ── Create form ── */}
      {showForm && (
        <form onSubmit={handleCreate} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">New Manual Journal Entry</h2>
          {formError && (
            <div className="flex items-center gap-2 rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700">
              <AlertTriangle className="h-4 w-4 shrink-0" />{formError}
            </div>
          )}

          {/* Header row */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Date *</label>
              <input type="date" required value={entryDate} onChange={e => setEntryDate(e.target.value)} className="input w-full" />
            </div>
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">Description *</label>
              <input required value={description} onChange={e => setDescription(e.target.value)}
                className="input w-full" placeholder="e.g. Monthly adjustment…" />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Currency *</label>
              <select required value={currencyId} onChange={e => setCurrencyId(e.target.value)} className="input w-full">
                <option value="">Select…</option>
                {currencies?.filter(c => c.isActive).map(c => (
                  <option key={c.id} value={c.id}>{c.code} — {c.name}</option>
                ))}
              </select>
            </div>
          </div>

          {/* Line items */}
          <div>
            <div className="grid grid-cols-12 gap-2 text-xs font-medium text-muted-foreground pb-1 border-b">
              <span className="col-span-5">Account</span>
              <span className="col-span-3">Description</span>
              <span className="col-span-1 text-right">Debit</span>
              <span className="col-span-1 text-right">Credit</span>
              <span className="col-span-2"></span>
            </div>
            <div className="space-y-1.5 mt-2">
              {lines.map((line, i) => (
                <div key={i} className="grid grid-cols-12 gap-2 items-center">
                  <div className="col-span-5">
                    <select value={line.accountId}
                      onChange={e => setLine(i, "accountId", e.target.value)}
                      className="input w-full text-sm py-1.5">
                      <option value="">Select account…</option>
                      {postingAccounts.map((a: AccountDto) => (
                        <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
                      ))}
                    </select>
                  </div>
                  <div className="col-span-3">
                    <input value={line.description}
                      onChange={e => setLine(i, "description", e.target.value)}
                      className="input w-full text-sm py-1.5" placeholder="Note…" />
                  </div>
                  <div className="col-span-1">
                    <input type="number" min="0" step="0.01" value={line.debit || ""}
                      onChange={e => setLine(i, "debit", e.target.value)}
                      className="input w-full text-sm py-1.5 text-right" placeholder="0.00" />
                  </div>
                  <div className="col-span-1">
                    <input type="number" min="0" step="0.01" value={line.credit || ""}
                      onChange={e => setLine(i, "credit", e.target.value)}
                      className="input w-full text-sm py-1.5 text-right" placeholder="0.00" />
                  </div>
                  <div className="col-span-2 flex items-center justify-end gap-1">
                    <button type="button" onClick={() => removeLine(i)} disabled={lines.length <= 2}
                      className="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 disabled:opacity-20">
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </div>
                </div>
              ))}
            </div>

            <button type="button" onClick={() => setLines(l => [...l, { ...EMPTY_LINE }])}
              className="mt-2 text-xs flex items-center gap-1 text-primary hover:underline">
              <Plus className="h-3.5 w-3.5" /> Add Line
            </button>

            {/* Totals */}
            <div className="mt-3 grid grid-cols-12 gap-2 text-sm font-semibold border-t pt-2">
              <span className="col-span-8 text-right text-muted-foreground">Totals</span>
              <span className="col-span-1 text-right tabular-nums">{totalDebit.toLocaleString()}</span>
              <span className="col-span-1 text-right tabular-nums">{totalCredit.toLocaleString()}</span>
              <span className="col-span-2 text-right text-xs">
                {balanced
                  ? <span className="text-emerald-600">✓ Balanced</span>
                  : <span className="text-red-600">Diff: {diff.toLocaleString()}</span>}
              </span>
            </div>
          </div>

          {/* Options */}
          <div className="flex items-center gap-2">
            <input type="checkbox" id="postNow" checked={postNow} onChange={e => setPostNow(e.target.checked)} className="h-4 w-4" />
            <label htmlFor="postNow" className="text-sm cursor-pointer">Post immediately after saving</label>
          </div>

          <div className="flex gap-2">
            <button type="submit" disabled={!balanced || createJE.isPending}
              className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
              <Check className="h-4 w-4" />
              {createJE.isPending ? t("saving") : (postNow ? "Save & Post" : "Save Draft")}
            </button>
            <button type="button" onClick={resetForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}

      {/* Filters */}
      <div className="flex gap-3 flex-wrap">
        <select value={params.status ?? ""} onChange={e => setParams(p => ({ ...p, status: e.target.value ? Number(e.target.value) : undefined }))} className="input text-sm">
          <option value="">All Statuses</option>
          <option value="1">Draft</option>
          <option value="2">Posted</option>
          <option value="3">Reversed</option>
        </select>
        <input type="date" value={params.fromDate ?? ""} onChange={e => setParams(p => ({ ...p, fromDate: e.target.value || undefined }))} className="input text-sm" placeholder="From" />
        <input type="date" value={params.toDate ?? ""} onChange={e => setParams(p => ({ ...p, toDate: e.target.value || undefined }))} className="input text-sm" placeholder="To" />
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>
      ) : (
        <div className="space-y-2">
          {(data ?? []).map((je: any) => {
            const st = STATUS[je.status as keyof typeof STATUS] ?? { label: "?", cls: "bg-muted" };
            const expanded = expandedId === je.id;
            return (
              <div key={je.id} className="card overflow-hidden">
                <div className="flex items-center gap-3 p-3">
                  <button onClick={() => setExpandedId(expanded ? null : je.id)}
                    className="p-1 rounded hover:bg-accent text-muted-foreground shrink-0">
                    {expanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                  </button>
                  <div className="flex-1 grid grid-cols-5 gap-2 items-center text-sm min-w-0">
                    <span className="font-mono text-xs text-muted-foreground">{je.entryNumber}</span>
                    <span className="text-muted-foreground">{je.entryDate}</span>
                    <span className="col-span-2 truncate font-medium">{je.description}</span>
                    <span className="text-right tabular-nums">{je.totalDebit?.toLocaleString()}</span>
                  </div>
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium shrink-0 ${st.cls}`}>{st.label}</span>
                  <div className="flex items-center gap-1 shrink-0">
                    {je.status === 1 && (
                      <button onClick={() => handlePost(je.id, je.entryNumber)} disabled={postJE.isPending}
                        className="text-xs px-2.5 py-1.5 rounded bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50">
                        Post
                      </button>
                    )}
                    {je.status === 2 && (
                      <button onClick={() => { setReversingId(je.id); }}
                        title="Reverse"
                        className="p-1.5 rounded hover:bg-amber-50 text-muted-foreground hover:text-amber-600">
                        <RotateCcw className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </div>
                {expanded && (
                  <div className="border-t bg-muted/10 p-3">
                    <table className="w-full text-xs">
                      <thead><tr className="text-muted-foreground">
                        <th className="text-left pb-1">Account</th>
                        <th className="text-left pb-1">Note</th>
                        <th className="text-right pb-1">Debit</th>
                        <th className="text-right pb-1">Credit</th>
                      </tr></thead>
                      <tbody className="divide-y divide-border/50">
                        {(je.lines ?? []).map((l: any, i: number) => (
                          <tr key={i}>
                            <td className="py-1 font-mono">{l.accountCode} — {l.accountName}</td>
                            <td className="py-1 text-muted-foreground">{l.description ?? "—"}</td>
                            <td className="py-1 text-right tabular-nums">{l.debit > 0 ? l.debit.toLocaleString() : "—"}</td>
                            <td className="py-1 text-right tabular-nums">{l.credit > 0 ? l.credit.toLocaleString() : "—"}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            );
          })}
          {(data ?? []).length === 0 && (
            <div className="card p-8 text-center text-muted-foreground">No journal entries found.</div>
          )}
        </div>
      )}
    </div>
  );
}
