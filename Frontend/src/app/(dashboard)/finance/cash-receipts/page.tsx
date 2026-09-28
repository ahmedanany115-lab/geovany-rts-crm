"use client";

import { useState } from "react";
import {
  useCashReceipts,
  useCashReceipt,
  useCreateCashReceipt,
  useUpdateCashReceipt,
  usePostCashReceipt,
  useVoidCashReceipt,
  useDeleteCashReceipt,
  useAccounts,
  useCurrencies,
} from "@/features/finance/hooks";
import { usePrint } from "@/hooks/usePrint";
import { useToast } from "@/components/ui/toast";
import type { AccountDto } from "@/features/finance/types";
import type { CashReceiptDetailDto, CreateCashReceiptRequest } from "@/features/finance/api/financeApi";
import {
  ArrowDownCircle, Plus, X, Check, Trash2, Pencil,
  AlertTriangle, Printer, RefreshCw,
} from "lucide-react";

/* ── Status config ─────────────────────────────────────────────────────────── */
const STATUS = {
  1: { label: "Draft",   cls: "bg-amber-100 text-amber-700" },
  2: { label: "Posted",  cls: "bg-emerald-100 text-emerald-700" },
  3: { label: "Voided",  cls: "bg-red-100 text-red-600" },
};

type FormData = {
  receiptDate: string;
  receivedFrom: string;
  amount: string;
  currencyId: string;
  exchangeRate: string;
  cashAccountId: string;
  contraAccountId: string;
  description: string;
  referenceNumber: string;
  notes: string;
  preparedBy: string;
  approvedBy: string;
};

const EMPTY_FORM: FormData = {
  receiptDate: new Date().toISOString().split("T")[0],
  receivedFrom: "",
  amount: "",
  currencyId: "",
  exchangeRate: "1.00",
  cashAccountId: "",
  contraAccountId: "",
  description: "",
  referenceNumber: "",
  notes: "",
  preparedBy: "",
  approvedBy: "",
};

function fmt(n: number) {
  return n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

/* ── Edit / Create form modal ──────────────────────────────────────────────── */
function ReceiptFormModal({
  editId,
  onClose,
  onSaved,
}: {
  editId: string | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { toast } = useToast();
  const { data: accounts } = useAccounts();
  const { data: currencies } = useCurrencies();
  const { data: existing, isLoading: loadingExisting } = useCashReceipt(editId ?? "");

  const createReceipt = useCreateCashReceipt();
  const updateReceipt = useUpdateCashReceipt();

  const postingAccounts = (accounts ?? []).filter((a: AccountDto) => !a.isGroup && a.isActive);
  const activeCurrencies = (currencies ?? []).filter(c => c.isActive);

  const [initialised, setInitialised] = useState(false);
  const [form, setForm] = useState<FormData>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);

  // Populate form once existing data loads for edit
  if (editId && existing && !initialised) {
    setForm({
      receiptDate: existing.receiptDate,
      receivedFrom: existing.receivedFrom ?? "",
      amount: String(existing.amount),
      currencyId: existing.currencyId,
      exchangeRate: String(existing.exchangeRate),
      cashAccountId: existing.cashAccountId,
      contraAccountId: existing.contraAccountId,
      description: existing.description ?? "",
      referenceNumber: existing.referenceNumber ?? "",
      notes: existing.notes ?? "",
      preparedBy: existing.preparedBy ?? "",
      approvedBy: existing.approvedBy ?? "",
    });
    setInitialised(true);
  }

  const set = (k: keyof FormData) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
    setForm(f => ({ ...f, [k]: e.target.value }));

  const buildRequest = (): CreateCashReceiptRequest => ({
    receiptDate: form.receiptDate,
    receivedFrom: form.receivedFrom || undefined,
    amount: parseFloat(form.amount) || 0,
    currencyId: form.currencyId,
    exchangeRate: parseFloat(form.exchangeRate) || 1,
    cashAccountId: form.cashAccountId,
    contraAccountId: form.contraAccountId,
    description: form.description || undefined,
    referenceNumber: form.referenceNumber || undefined,
    notes: form.notes || undefined,
    preparedBy: form.preparedBy || undefined,
    approvedBy: form.approvedBy || undefined,
  });

  const validate = (): string | null => {
    if (!form.receiptDate) return "Receipt date is required.";
    const amt = parseFloat(form.amount);
    if (!amt || amt <= 0) return "Amount must be greater than 0.";
    if (!form.currencyId) return "Select a currency.";
    if (!form.cashAccountId) return "Select a cash account.";
    if (!form.contraAccountId) return "Select a contra account.";
    return null;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    const err = validate();
    if (err) { setFormError(err); return; }
    try {
      if (editId) {
        await updateReceipt.mutateAsync({ id: editId, ...buildRequest() });
        toast("Cash receipt updated.", "success");
      } else {
        const res = await createReceipt.mutateAsync(buildRequest());
        toast(`Receipt ${(res as any).receiptNumber ?? ""} created.`, "success");
      }
      onSaved();
    } catch (err: any) {
      const msg = err?.message ?? "Failed to save receipt.";
      setFormError(msg);
      toast(msg, "error");
    }
  };

  const isPending = createReceipt.isPending || updateReceipt.isPending;
  const isLoading = editId ? (loadingExisting || (!!editId && !initialised)) : false;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4 overflow-y-auto">
      <div className="bg-background rounded-xl border shadow-xl w-full max-w-2xl my-auto">
        <div className="flex items-center justify-between p-5 border-b">
          <div className="flex items-center gap-3 text-primary">
            <ArrowDownCircle className="h-5 w-5" />
            <h2 className="font-semibold">{editId ? "Edit Cash Receipt" : "New Cash Receipt"}</h2>
          </div>
          <button onClick={onClose} className="p-1 rounded hover:bg-accent text-muted-foreground">
            <X className="h-5 w-5" />
          </button>
        </div>

        {isLoading ? (
          <div className="p-10 text-center text-muted-foreground">Loading…</div>
        ) : (
          <form onSubmit={handleSubmit} className="p-5 space-y-4">
            {formError && (
              <div className="flex items-center gap-2 rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700">
                <AlertTriangle className="h-4 w-4 shrink-0" />{formError}
              </div>
            )}

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Receipt Date *</label>
                <input type="date" required value={form.receiptDate} onChange={set("receiptDate")} className="input w-full" />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Received From</label>
                <input value={form.receivedFrom} onChange={set("receivedFrom")} className="input w-full" placeholder="Customer / person name…" />
              </div>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Amount *</label>
                <input type="number" min="0.01" step="0.01" required value={form.amount}
                  onChange={set("amount")} className="input w-full text-right" placeholder="0.00" />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Currency *</label>
                <select required value={form.currencyId} onChange={set("currencyId")} className="input w-full">
                  <option value="">Select…</option>
                  {activeCurrencies.map(c => (
                    <option key={c.id} value={c.id}>{c.code} — {c.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Exchange Rate</label>
                <input type="number" min="0.0001" step="0.0001" value={form.exchangeRate}
                  onChange={set("exchangeRate")} className="input w-full text-right" />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Cash Account *</label>
                <select required value={form.cashAccountId} onChange={set("cashAccountId")} className="input w-full">
                  <option value="">Select…</option>
                  {postingAccounts.map((a: AccountDto) => (
                    <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Account Affected (Contra) *</label>
                <select required value={form.contraAccountId} onChange={set("contraAccountId")} className="input w-full">
                  <option value="">Select…</option>
                  {postingAccounts.map((a: AccountDto) => (
                    <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Description</label>
                <input value={form.description} onChange={set("description")} className="input w-full" placeholder="Description…" />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Reference Number</label>
                <input value={form.referenceNumber} onChange={set("referenceNumber")} className="input w-full" placeholder="Ref #…" />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Prepared By</label>
                <input value={form.preparedBy} onChange={set("preparedBy")} className="input w-full" placeholder="Name…" />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Approved By</label>
                <input value={form.approvedBy} onChange={set("approvedBy")} className="input w-full" placeholder="Name…" />
              </div>
            </div>

            <div>
              <label className="text-xs text-muted-foreground block mb-1">Notes</label>
              <textarea value={form.notes} onChange={set("notes")} rows={2}
                className="input w-full resize-none" placeholder="Additional notes…" />
            </div>

            <div className="flex gap-2 pt-2">
              <button type="submit" disabled={isPending}
                className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
                <Check className="h-4 w-4" />
                {isPending ? "Saving…" : "Save Draft"}
              </button>
              <button type="button" onClick={onClose} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}

/* ── Print view ────────────────────────────────────────────────────────────── */
function PrintView({ receipt }: { receipt: CashReceiptDetailDto }) {
  return (
    <div className="p-8 font-mono text-sm space-y-4 text-black">
      <div className="text-center text-xl font-bold tracking-widest border-b-2 border-black pb-2">
        CASH RECEIPT
      </div>
      <div className="flex justify-between text-xs">
        <span>Receipt #: <strong>{receipt.receiptNumber}</strong></span>
        <span>Date: <strong>{new Date(receipt.receiptDate).toLocaleDateString("en-GB")}</strong></span>
      </div>
      {receipt.receivedFrom && (
        <div><span className="font-semibold">Received From:</span> {receipt.receivedFrom}</div>
      )}
      <div>
        <span className="font-semibold">Amount:</span>{" "}
        {fmt(receipt.amount)} {receipt.currencyCode}
      </div>
      {receipt.description && (
        <div><span className="font-semibold">Description:</span> {receipt.description}</div>
      )}
      {receipt.referenceNumber && (
        <div><span className="font-semibold">Reference:</span> {receipt.referenceNumber}</div>
      )}
      <div className="border-t border-black pt-2 mt-2">
        <div className="font-semibold mb-1">Accounting:</div>
        <div className="ml-4 space-y-0.5">
          <div>Cash Account: {receipt.cashAccountName}</div>
          <div>Contra Account: {receipt.contraAccountName}</div>
        </div>
      </div>
      <div className="flex justify-between border-t border-black pt-2 mt-4">
        <span>Prepared By: {receipt.preparedBy ?? "—"}</span>
        <span>Approved By: {receipt.approvedBy ?? "—"}</span>
      </div>
      {receipt.notes && (
        <div><span className="font-semibold">Notes:</span> {receipt.notes}</div>
      )}
    </div>
  );
}

/* ── Print modal ───────────────────────────────────────────────────────────── */
function PrintModal({ id, onClose }: { id: string; onClose: () => void }) {
  const { data: receipt, isLoading } = useCashReceipt(id);
  const { printRef, handlePrint } = usePrint(`Cash Receipt ${receipt?.receiptNumber ?? ""}`);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="bg-background rounded-xl border shadow-xl w-full max-w-xl">
        <div className="flex items-center justify-between p-4 border-b">
          <span className="font-semibold text-sm">Print Preview</span>
          <div className="flex gap-2">
            <button onClick={handlePrint}
              className="flex items-center gap-2 btn-primary px-4 py-1.5 rounded-lg text-sm">
              <Printer className="h-4 w-4" /> Print
            </button>
            <button onClick={onClose} className="p-1 rounded hover:bg-accent text-muted-foreground"><X className="h-4 w-4" /></button>
          </div>
        </div>
        <div ref={printRef} className="overflow-y-auto max-h-[70vh]">
          {isLoading || !receipt ? (
            <div className="p-10 text-center text-muted-foreground">Loading…</div>
          ) : (
            <PrintView receipt={receipt} />
          )}
        </div>
      </div>
    </div>
  );
}

/* ── Main page ─────────────────────────────────────────────────────────────── */
export default function CashReceiptsPage() {
  const { toast } = useToast();

  const [params, setParams] = useState<{ status?: number; fromDate?: string; toDate?: string }>({});
  const { data, isLoading, refetch } = useCashReceipts(params);

  const postReceipt   = usePostCashReceipt();
  const voidReceipt   = useVoidCashReceipt();
  const deleteReceipt = useDeleteCashReceipt();

  const [showForm, setShowForm]   = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [printId, setPrintId]     = useState<string | null>(null);

  const openCreate = () => { setEditingId(null); setShowForm(true); };
  const openEdit   = (id: string) => { setEditingId(id); setShowForm(true); };
  const closeForm  = () => { setShowForm(false); setEditingId(null); };
  const onSaved    = () => { closeForm(); refetch(); };

  const handlePost = async (id: string, num: string) => {
    if (!confirm(`This will create a journal entry for ${num}. Continue?`)) return;
    try {
      await postReceipt.mutateAsync(id);
      toast(`${num} posted. Journal entry created.`, "success");
      refetch();
    } catch (err: any) { toast(err?.message ?? "Failed to post.", "error"); }
  };

  const handleVoid = async (id: string, num: string) => {
    if (!confirm(`Void receipt ${num}? This action cannot be undone.`)) return;
    try {
      await voidReceipt.mutateAsync(id);
      toast(`${num} voided.`, "info");
      refetch();
    } catch (err: any) { toast(err?.message ?? "Failed to void.", "error"); }
  };

  const handleDelete = async (id: string, num: string) => {
    if (!confirm(`Delete draft receipt ${num}? This cannot be undone.`)) return;
    try {
      await deleteReceipt.mutateAsync(id);
      toast(`${num} deleted.`, "info");
      refetch();
    } catch (err: any) { toast(err?.message ?? "Failed to delete.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      {/* Form modal */}
      {showForm && (
        <ReceiptFormModal
          editId={editingId}
          onClose={closeForm}
          onSaved={onSaved}
        />
      )}

      {/* Print modal */}
      {printId && <PrintModal id={printId} onClose={() => setPrintId(null)} />}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <ArrowDownCircle className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">Cash Receipts</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={openCreate}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            <Plus className="h-4 w-4" /> New Receipt
          </button>
        </div>
      </div>

      {/* Filters */}
      <div className="flex gap-3 flex-wrap">
        <select
          value={params.status ?? ""}
          onChange={e => setParams(p => ({ ...p, status: e.target.value ? Number(e.target.value) : undefined }))}
          className="input text-sm">
          <option value="">All Statuses</option>
          <option value="1">Draft</option>
          <option value="2">Posted</option>
          <option value="3">Voided</option>
        </select>
        <input type="date" value={params.fromDate ?? ""}
          onChange={e => setParams(p => ({ ...p, fromDate: e.target.value || undefined }))}
          className="input text-sm" placeholder="From" />
        <input type="date" value={params.toDate ?? ""}
          onChange={e => setParams(p => ({ ...p, toDate: e.target.value || undefined }))}
          className="input text-sm" placeholder="To" />
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">Loading…</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/30">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Receipt #</th>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Date</th>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Received From</th>
                <th className="text-right px-4 py-3 font-medium text-muted-foreground">Amount</th>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Currency</th>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Cash Account</th>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Status</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {(data ?? []).length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-4 py-12 text-center text-muted-foreground">
                    No cash receipts found.
                  </td>
                </tr>
              ) : (data ?? []).map((r: any) => {
                const st = STATUS[r.status as keyof typeof STATUS] ?? { label: "?", cls: "bg-muted" };
                const isDraft  = r.status === 1;
                const isPosted = r.status === 2;
                return (
                  <tr key={r.id} className="hover:bg-muted/20">
                    <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{r.receiptNumber}</td>
                    <td className="px-4 py-3">{new Date(r.receiptDate).toLocaleDateString("en-GB")}</td>
                    <td className="px-4 py-3 text-muted-foreground">{r.receivedFrom ?? "—"}</td>
                    <td className="px-4 py-3 text-right tabular-nums font-medium">{fmt(r.amount)}</td>
                    <td className="px-4 py-3 font-mono text-xs">{r.currencyCode}</td>
                    <td className="px-4 py-3 text-muted-foreground">{r.cashAccountName}</td>
                    <td className="px-4 py-3">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${st.cls}`}>{st.label}</span>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-1">
                        <button onClick={() => setPrintId(r.id)} title="Print"
                          className="p-1.5 rounded hover:bg-accent text-muted-foreground">
                          <Printer className="h-3.5 w-3.5" />
                        </button>
                        {isDraft && (
                          <>
                            <button onClick={() => openEdit(r.id)} title="Edit"
                              className="p-1.5 rounded hover:bg-blue-50 text-muted-foreground hover:text-blue-600">
                              <Pencil className="h-3.5 w-3.5" />
                            </button>
                            <button
                              onClick={() => handlePost(r.id, r.receiptNumber)}
                              disabled={postReceipt.isPending}
                              className="text-xs px-2.5 py-1.5 rounded bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50">
                              Post
                            </button>
                            <button onClick={() => handleDelete(r.id, r.receiptNumber)} title="Delete"
                              disabled={deleteReceipt.isPending}
                              className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600">
                              <Trash2 className="h-3.5 w-3.5" />
                            </button>
                          </>
                        )}
                        {isPosted && (
                          <button
                            onClick={() => handleVoid(r.id, r.receiptNumber)}
                            disabled={voidReceipt.isPending}
                            className="text-xs px-2.5 py-1.5 rounded bg-red-600 text-white hover:bg-red-700 disabled:opacity-50">
                            Void
                          </button>
                        )}
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
