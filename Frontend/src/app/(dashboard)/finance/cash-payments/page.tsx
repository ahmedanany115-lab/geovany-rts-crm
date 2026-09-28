"use client";

import { useState } from "react";
import {
  useCashPayments,
  useCashPayment,
  useCreateCashPayment,
  useUpdateCashPayment,
  usePostCashPayment,
  useVoidCashPayment,
  useDeleteCashPayment,
  useAccounts,
  useCurrencies,
} from "@/features/finance/hooks";
import { usePrint } from "@/hooks/usePrint";
import { useCompanySettings } from "@/hooks/useCompanySettings";
import { useToast } from "@/components/ui/toast";
import type { AccountDto } from "@/features/finance/types";
import type { CashPaymentDetailDto, CreateCashPaymentRequest } from "@/features/finance/api/financeApi";
import {
  ArrowUpCircle, Plus, X, Check, Trash2, Pencil,
  AlertTriangle, Printer, RefreshCw,
} from "lucide-react";

/* ── Status config ─────────────────────────────────────────────────────────── */
const STATUS = {
  1: { label: "Draft",   cls: "bg-amber-100 text-amber-700" },
  2: { label: "Posted",  cls: "bg-emerald-100 text-emerald-700" },
  3: { label: "Voided",  cls: "bg-red-100 text-red-600" },
};

type FormData = {
  paymentDate: string;
  paidTo: string;
  amount: string;
  currencyId: string;
  exchangeRate: string;
  cashAccountId: string;
  expenseAccountId: string;
  description: string;
  referenceNumber: string;
  notes: string;
  preparedBy: string;
  approvedBy: string;
};

const EMPTY_FORM: FormData = {
  paymentDate: new Date().toISOString().split("T")[0],
  paidTo: "",
  amount: "",
  currencyId: "",
  exchangeRate: "1.00",
  cashAccountId: "",
  expenseAccountId: "",
  description: "",
  referenceNumber: "",
  notes: "",
  preparedBy: "",
  approvedBy: "",
};

function fmt(n: number) {
  return n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

/* ── Form modal ────────────────────────────────────────────────────────────── */
function PaymentFormModal({
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
  const { data: existing, isLoading: loadingExisting } = useCashPayment(editId ?? "");

  const createPayment = useCreateCashPayment();
  const updatePayment = useUpdateCashPayment();

  const postingAccounts = (accounts ?? []).filter((a: AccountDto) => !a.isGroup && a.isActive);
  const activeCurrencies = (currencies ?? []).filter(c => c.isActive);

  const [initialised, setInitialised] = useState(false);
  const [form, setForm] = useState<FormData>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);

  if (editId && existing && !initialised) {
    setForm({
      paymentDate: existing.paymentDate,
      paidTo: existing.paidTo ?? "",
      amount: String(existing.amount),
      currencyId: existing.currencyId,
      exchangeRate: String(existing.exchangeRate),
      cashAccountId: existing.cashAccountId,
      expenseAccountId: existing.expenseAccountId,
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

  const buildRequest = (): CreateCashPaymentRequest => ({
    paymentDate: form.paymentDate,
    paidTo: form.paidTo || undefined,
    amount: parseFloat(form.amount) || 0,
    currencyId: form.currencyId,
    exchangeRate: parseFloat(form.exchangeRate) || 1,
    cashAccountId: form.cashAccountId,
    expenseAccountId: form.expenseAccountId,
    description: form.description || undefined,
    referenceNumber: form.referenceNumber || undefined,
    notes: form.notes || undefined,
    preparedBy: form.preparedBy || undefined,
    approvedBy: form.approvedBy || undefined,
  });

  const validate = (): string | null => {
    if (!form.paymentDate) return "Payment date is required.";
    const amt = parseFloat(form.amount);
    if (!amt || amt <= 0) return "Amount must be greater than 0.";
    if (!form.currencyId) return "Select a currency.";
    if (!form.cashAccountId) return "Select a cash account.";
    if (!form.expenseAccountId) return "Select an expense account.";
    return null;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    const err = validate();
    if (err) { setFormError(err); return; }
    try {
      if (editId) {
        await updatePayment.mutateAsync({ id: editId, ...buildRequest() });
        toast("Cash payment updated.", "success");
      } else {
        const res = await createPayment.mutateAsync(buildRequest());
        toast(`Payment ${(res as any).paymentNumber ?? ""} created.`, "success");
      }
      onSaved();
    } catch (err: any) {
      const msg = err?.message ?? "Failed to save payment.";
      setFormError(msg);
      toast(msg, "error");
    }
  };

  const isPending = createPayment.isPending || updatePayment.isPending;
  const isLoading = editId ? (loadingExisting || (!!editId && !initialised)) : false;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4 overflow-y-auto">
      <div className="bg-background rounded-xl border shadow-xl w-full max-w-2xl my-auto">
        <div className="flex items-center justify-between p-5 border-b">
          <div className="flex items-center gap-3 text-primary">
            <ArrowUpCircle className="h-5 w-5" />
            <h2 className="font-semibold">{editId ? "Edit Cash Payment" : "New Cash Payment"}</h2>
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
                <label className="text-xs text-muted-foreground block mb-1">Payment Date *</label>
                <input type="date" required value={form.paymentDate} onChange={set("paymentDate")} className="input w-full" />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Paid To</label>
                <input value={form.paidTo} onChange={set("paidTo")} className="input w-full" placeholder="Vendor / person name…" />
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
                <label className="text-xs text-muted-foreground block mb-1">Expense Account *</label>
                <select required value={form.expenseAccountId} onChange={set("expenseAccountId")} className="input w-full">
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

/* ── Print view (Cash Payment Voucher / إذن صرف نقدية) ────────────────────── */
function PrintView({ payment }: { payment: CashPaymentDetailDto }) {
  const co = useCompanySettings();

  // Convert number to words (simple EGP format)
  function toWords(n: number): string {
    if (n === 0) return "Zero";
    const ones = ["","One","Two","Three","Four","Five","Six","Seven","Eight","Nine",
      "Ten","Eleven","Twelve","Thirteen","Fourteen","Fifteen","Sixteen","Seventeen",
      "Eighteen","Nineteen"];
    const tens = ["","","Twenty","Thirty","Forty","Fifty","Sixty","Seventy","Eighty","Ninety"];
    function below1000(x: number): string {
      if (x < 20) return ones[x];
      if (x < 100) return tens[Math.floor(x/10)] + (x%10 ? " " + ones[x%10] : "");
      return ones[Math.floor(x/100)] + " Hundred" + (x%100 ? " " + below1000(x%100) : "");
    }
    const intPart = Math.floor(n);
    const decPart = Math.round((n - intPart) * 100);
    let result = "";
    if (intPart >= 1000) result += below1000(Math.floor(intPart/1000)) + " Thousand ";
    result += below1000(intPart % 1000);
    result += ` ${payment.currencyCode ?? "EGP"}`;
    if (decPart > 0) result += ` and ${decPart}/100 Piasters`;
    return result.trim() + " Only";
  }

  return (
    <div className="bg-white text-black text-sm" style={{ fontFamily: "Arial, sans-serif", padding: "32px 40px", minHeight: "297mm", width: "210mm", margin: "0 auto" }}>
      {/* Header */}
      <div className="print-header" style={{ display: "flex", alignItems: "center", gap: 16, paddingBottom: 16, borderBottom: "2px solid #1e3a8a", marginBottom: 24 }}>
        <div style={{ flex: 1 }}>
          <div style={{ fontSize: 20, fontWeight: "bold", color: "#1e3a8a" }}>{co.name}</div>
          <div style={{ fontSize: 12, color: "#555" }}>{co.nameAr}</div>
          {co.phone && <div style={{ fontSize: 11, color: "#555" }}>Tel: {co.phone}</div>}
          {co.email && <div style={{ fontSize: 11, color: "#555" }}>Email: {co.email}</div>}
        </div>
        <div style={{ textAlign: "right" }}>
          <div style={{ fontSize: 22, fontWeight: "bold", color: "#1e3a8a" }}>Cash Payment Voucher</div>
          <div style={{ fontSize: 16, color: "#1e3a8a", marginTop: 4 }}>إذن صرف نقدية</div>
        </div>
      </div>

      {/* Voucher meta */}
      <table style={{ width: "100%", borderCollapse: "collapse", marginBottom: 24 }}>
        <tbody>
          <tr>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb", fontWeight: "bold", background: "#f0f4ff", width: "20%" }}>Voucher No.</td>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb", width: "30%" }}><strong>{payment.paymentNumber}</strong></td>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb", fontWeight: "bold", background: "#f0f4ff", width: "20%" }}>Date</td>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb" }}>
              {new Date(payment.paymentDate + "T00:00:00").toLocaleDateString("en-GB", { day: "2-digit", month: "long", year: "numeric" })}
            </td>
          </tr>
          <tr>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb", fontWeight: "bold", background: "#f0f4ff" }}>Paid To</td>
            <td colSpan={3} style={{ padding: "6px 12px", border: "1px solid #bbb" }}>{payment.paidTo ?? "—"}</td>
          </tr>
          <tr>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb", fontWeight: "bold", background: "#f0f4ff" }}>Amount</td>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb", fontSize: 16, fontWeight: "bold" }}>
              {payment.amount.toLocaleString("en-US", { minimumFractionDigits: 2 })} {payment.currencyCode}
            </td>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb", fontWeight: "bold", background: "#f0f4ff" }}>Cash Account</td>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb" }}>{payment.cashAccountName}</td>
          </tr>
          <tr>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb", fontWeight: "bold", background: "#f0f4ff" }}>Amount in Words</td>
            <td colSpan={3} style={{ padding: "6px 12px", border: "1px solid #bbb", fontStyle: "italic" }}>{toWords(payment.amount)}</td>
          </tr>
          <tr>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb", fontWeight: "bold", background: "#f0f4ff" }}>Description</td>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb" }}>{payment.description ?? "—"}</td>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb", fontWeight: "bold", background: "#f0f4ff" }}>Expense Account</td>
            <td style={{ padding: "6px 12px", border: "1px solid #bbb" }}>{payment.expenseAccountName}</td>
          </tr>
          {payment.referenceNumber && (
            <tr>
              <td style={{ padding: "6px 12px", border: "1px solid #bbb", fontWeight: "bold", background: "#f0f4ff" }}>Reference</td>
              <td colSpan={3} style={{ padding: "6px 12px", border: "1px solid #bbb" }}>{payment.referenceNumber}</td>
            </tr>
          )}
          {payment.notes && (
            <tr>
              <td style={{ padding: "6px 12px", border: "1px solid #bbb", fontWeight: "bold", background: "#f0f4ff" }}>Notes</td>
              <td colSpan={3} style={{ padding: "6px 12px", border: "1px solid #bbb" }}>{payment.notes}</td>
            </tr>
          )}
        </tbody>
      </table>

      {/* Signatures */}
      <div style={{ display: "flex", justifyContent: "space-between", marginTop: 60, gap: 24 }}>
        <div style={{ textAlign: "center", flex: 1 }}>
          <div style={{ borderTop: "1px solid #555", paddingTop: 8 }}>Prepared By</div>
          <div style={{ marginTop: 4, color: "#555", fontSize: 12 }}>{payment.preparedBy ?? "___________________"}</div>
        </div>
        <div style={{ textAlign: "center", flex: 1 }}>
          <div style={{ borderTop: "1px solid #555", paddingTop: 8 }}>Paid To (Recipient)</div>
          <div style={{ marginTop: 4, color: "#555", fontSize: 12 }}>___________________</div>
        </div>
        <div style={{ textAlign: "center", flex: 1 }}>
          <div style={{ borderTop: "1px solid #555", paddingTop: 8 }}>Approved By</div>
          <div style={{ marginTop: 4, color: "#555", fontSize: 12 }}>{payment.approvedBy ?? "___________________"}</div>
        </div>
      </div>
    </div>
  );
}

/* ── Print modal ───────────────────────────────────────────────────────────── */
function PrintModal({ id, onClose }: { id: string; onClose: () => void }) {
  const { data: payment, isLoading } = useCashPayment(id);
  const { printRef, handlePrint } = usePrint(`Cash Payment ${payment?.paymentNumber ?? ""}`);

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
          {isLoading || !payment ? (
            <div className="p-10 text-center text-muted-foreground">Loading…</div>
          ) : (
            <PrintView payment={payment} />
          )}
        </div>
      </div>
    </div>
  );
}

/* ── Main page ─────────────────────────────────────────────────────────────── */
export default function CashPaymentsPage() {
  const { toast } = useToast();

  const [params, setParams] = useState<{ status?: number; fromDate?: string; toDate?: string }>({});
  const { data, isLoading, refetch } = useCashPayments(params);

  const postPayment   = usePostCashPayment();
  const voidPayment   = useVoidCashPayment();
  const deletePayment = useDeleteCashPayment();

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
      await postPayment.mutateAsync(id);
      toast(`${num} posted. Journal entry created.`, "success");
      refetch();
    } catch (err: any) { toast(err?.message ?? "Failed to post.", "error"); }
  };

  const handleVoid = async (id: string, num: string) => {
    if (!confirm(`Void payment ${num}? This action cannot be undone.`)) return;
    try {
      await voidPayment.mutateAsync(id);
      toast(`${num} voided.`, "info");
      refetch();
    } catch (err: any) { toast(err?.message ?? "Failed to void.", "error"); }
  };

  const handleDelete = async (id: string, num: string) => {
    if (!confirm(`Delete draft payment ${num}? This cannot be undone.`)) return;
    try {
      await deletePayment.mutateAsync(id);
      toast(`${num} deleted.`, "info");
      refetch();
    } catch (err: any) { toast(err?.message ?? "Failed to delete.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      {/* Form modal */}
      {showForm && (
        <PaymentFormModal
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
          <ArrowUpCircle className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">Cash Payments</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={openCreate}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            <Plus className="h-4 w-4" /> New Payment
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
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Payment #</th>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Date</th>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Paid To</th>
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
                    No cash payments found.
                  </td>
                </tr>
              ) : (data ?? []).map((p: any) => {
                const st = STATUS[p.status as keyof typeof STATUS] ?? { label: "?", cls: "bg-muted" };
                const isDraft  = p.status === 1;
                const isPosted = p.status === 2;
                return (
                  <tr key={p.id} className="hover:bg-muted/20">
                    <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{p.paymentNumber}</td>
                    <td className="px-4 py-3">{new Date(p.paymentDate).toLocaleDateString("en-GB")}</td>
                    <td className="px-4 py-3 text-muted-foreground">{p.paidTo ?? "—"}</td>
                    <td className="px-4 py-3 text-right tabular-nums font-medium">{fmt(p.amount)}</td>
                    <td className="px-4 py-3 font-mono text-xs">{p.currencyCode}</td>
                    <td className="px-4 py-3 text-muted-foreground">{p.cashAccountName}</td>
                    <td className="px-4 py-3">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${st.cls}`}>{st.label}</span>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-1">
                        <button onClick={() => setPrintId(p.id)} title="Print"
                          className="p-1.5 rounded hover:bg-accent text-muted-foreground">
                          <Printer className="h-3.5 w-3.5" />
                        </button>
                        {isDraft && (
                          <>
                            <button onClick={() => openEdit(p.id)} title="Edit"
                              className="p-1.5 rounded hover:bg-blue-50 text-muted-foreground hover:text-blue-600">
                              <Pencil className="h-3.5 w-3.5" />
                            </button>
                            <button
                              onClick={() => handlePost(p.id, p.paymentNumber)}
                              disabled={postPayment.isPending}
                              className="text-xs px-2.5 py-1.5 rounded bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50">
                              Post
                            </button>
                            <button onClick={() => handleDelete(p.id, p.paymentNumber)} title="Delete"
                              disabled={deletePayment.isPending}
                              className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600">
                              <Trash2 className="h-3.5 w-3.5" />
                            </button>
                          </>
                        )}
                        {isPosted && (
                          <button
                            onClick={() => handleVoid(p.id, p.paymentNumber)}
                            disabled={voidPayment.isPending}
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
