"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useCustomers, useSuppliers, useBankAccounts } from "@/features/erp/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { CreditCard, RefreshCw, Plus, X, Check, AlertTriangle } from "lucide-react";

const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Received",  cls: "bg-blue-100 text-blue-700" },
  2: { label: "Deposited", cls: "bg-emerald-100 text-emerald-700" },
  3: { label: "Cleared",   cls: "bg-emerald-200 text-emerald-800" },
  4: { label: "Bounced",   cls: "bg-red-100 text-red-700" },
  5: { label: "Cancelled", cls: "bg-muted text-muted-foreground" },
};

const INIT_REC = { customerId:"", chequeNumber:"", bankName:"", currencyId:"", amount:"", issueDate:"", dueDate:"", receivedDate:new Date().toISOString().split("T")[0], notes:"" };
const INIT_PAY = { supplierId:"", chequeNumber:"", bankName:"", currencyId:"", amount:"", issueDate:"", dueDate:"", notes:"" };

export default function ChequesPage() {
  const { t } = useT();
  const { toast } = useToast();
  const qc = useQueryClient();

  const [tab, setTab]       = useState<"receivable" | "payable">("receivable");
  const [showForm, setShowForm] = useState(false);
  const [recForm, setRecForm]   = useState(INIT_REC);
  const [payForm, setPayForm]   = useState(INIT_PAY);
  const [formError, setFormError] = useState<string | null>(null);

  /* ── queries ── */
  const recQ = useQuery({ queryKey: ["cheques", "receivable"], queryFn: () => apiFetch<any[]>("/cheques?direction=1") });
  const payQ = useQuery({ queryKey: ["cheques", "payable"],    queryFn: () => apiFetch<any[]>("/cheques?direction=2") });

  const { data: customers  } = useCustomers({});
  const { data: suppliers  } = useSuppliers({});
  const { data: currencies } = useCurrencies();
  const { data: bankAccts  } = useBankAccounts({ isActive: true });

  /* ── mutations ── */
  const createRec = useMutation({
    mutationFn: (data: any) => apiFetch<{ id: string }>("/cheques", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["cheques"] }); toast("Cheque receivable recorded.", "success"); setShowForm(false); setRecForm(INIT_REC); },
    onError: (e: any) => { toast(e?.message ?? "Failed.", "error"); },
  });

  const createPay = useMutation({
    mutationFn: (data: any) => apiFetch<{ id: string }>("/cheques", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["cheques"] }); toast("Cheque payable recorded.", "success"); setShowForm(false); setPayForm(INIT_PAY); },
    onError: (e: any) => { toast(e?.message ?? "Failed.", "error"); },
  });

  const depositMut = useMutation({
    mutationFn: ({ id, bankAccountId, depositDate }: { id: string; bankAccountId: string; depositDate: string }) =>
      apiFetch<void>(`/cheques/${id}/deposit`, { method: "POST", body: JSON.stringify({ bankAccountId, depositDate }) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["cheques"] }); toast("Cheque deposited.", "success"); },
    onError: (e: any) => toast(e?.message ?? "Failed.", "error"),
  });

  const bounceMut = useMutation({
    mutationFn: ({ id, date }: { id: string; date: string }) =>
      apiFetch<void>(`/cheques/${id}/bounce`, { method: "POST", body: JSON.stringify(date) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["cheques"] }); toast("Cheque marked bounced.", "info"); },
    onError: (e: any) => toast(e?.message ?? "Failed.", "error"),
  });

  const [depositingId, setDepositingId] = useState<string | null>(null);
  const [depositBankId, setDepositBankId] = useState("");
  const [depositDate, setDepositDate] = useState(new Date().toISOString().split("T")[0]);

  const handleSubmitRec = async (e: React.FormEvent) => {
    e.preventDefault(); setFormError(null);
    if (!recForm.customerId) { setFormError("Select a customer."); return; }
    if (!recForm.currencyId) { setFormError("Select a currency."); return; }
    if (!recForm.amount || Number(recForm.amount) <= 0) { setFormError("Amount must be > 0."); return; }
    createRec.mutate({ ...recForm, amount: Number(recForm.amount), direction: 1 });
  };

  const handleSubmitPay = async (e: React.FormEvent) => {
    e.preventDefault(); setFormError(null);
    if (!payForm.supplierId) { setFormError("Select a supplier."); return; }
    if (!payForm.currencyId) { setFormError("Select a currency."); return; }
    if (!payForm.amount || Number(payForm.amount) <= 0) { setFormError("Amount must be > 0."); return; }
    createPay.mutate({ ...payForm, amount: Number(payForm.amount), direction: 2 });
  };

  const data    = tab === "receivable" ? recQ.data  : payQ.data;
  const loading = tab === "receivable" ? recQ.isLoading : payQ.isLoading;
  const refetch = tab === "receivable" ? recQ.refetch : payQ.refetch;

  return (
    <div className="p-6 space-y-6">

      {/* Deposit modal */}
      {depositingId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <h2 className="font-semibold">Deposit Cheque</h2>
            <div className="space-y-3">
              <div><label className="text-xs text-muted-foreground block mb-1">Deposit to Bank Account *</label>
                <select value={depositBankId} onChange={e => setDepositBankId(e.target.value)} className="input w-full">
                  <option value="">Select…</option>
                  {(bankAccts ?? []).map((b: any) => <option key={b.id} value={b.id}>{b.name} — {b.bankName}</option>)}
                </select>
              </div>
              <div><label className="text-xs text-muted-foreground block mb-1">Deposit Date *</label>
                <input type="date" value={depositDate} onChange={e => setDepositDate(e.target.value)} className="input w-full" />
              </div>
            </div>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setDepositingId(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={() => { depositMut.mutate({ id: depositingId, bankAccountId: depositBankId, depositDate }); setDepositingId(null); }}
                disabled={!depositBankId || depositMut.isPending}
                className="btn-primary px-4 py-2 rounded-lg text-sm disabled:opacity-50">Deposit</button>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <CreditCard className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">{t("cheques")}</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => { setShowForm(v => !v); setFormError(null); }}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : (tab === "receivable" ? "Receive Cheque" : "Record Payable Cheque")}
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-border">
        {([["receivable","Cheques Receivable"],["payable","Cheques Payable"]] as const).map(([k,lbl]) => (
          <button key={k} onClick={() => { setTab(k); setShowForm(false); }}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${tab === k ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}>
            {lbl}
          </button>
        ))}
      </div>

      {/* Forms */}
      {showForm && tab === "receivable" && (
        <form onSubmit={handleSubmitRec} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">Record Cheque Receivable</h2>
          {formError && <div className="flex items-center gap-2 rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700"><AlertTriangle className="h-4 w-4 shrink-0" />{formError}</div>}
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">Customer *</label>
              <select required value={recForm.customerId} onChange={e => setRecForm(f => ({ ...f, customerId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {(customers ?? []).filter((c: any) => c.isActive).map((c: any) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Cheque # *</label>
              <input required value={recForm.chequeNumber} onChange={e => setRecForm(f => ({ ...f, chequeNumber: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Bank Name *</label>
              <input required value={recForm.bankName} onChange={e => setRecForm(f => ({ ...f, bankName: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Amount *</label>
              <input required type="number" min="0.01" step="0.01" value={recForm.amount} onChange={e => setRecForm(f => ({ ...f, amount: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Currency *</label>
              <select required value={recForm.currencyId} onChange={e => setRecForm(f => ({ ...f, currencyId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {currencies?.filter(c => c.isActive).map(c => <option key={c.id} value={c.id}>{c.code}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Issue Date</label>
              <input type="date" value={recForm.issueDate} onChange={e => setRecForm(f => ({ ...f, issueDate: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Due Date *</label>
              <input required type="date" value={recForm.dueDate} onChange={e => setRecForm(f => ({ ...f, dueDate: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Received Date</label>
              <input type="date" value={recForm.receivedDate} onChange={e => setRecForm(f => ({ ...f, receivedDate: e.target.value }))} className="input w-full" />
            </div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Notes</label>
              <input value={recForm.notes} onChange={e => setRecForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" />
            </div>
          </div>
          <button type="submit" disabled={createRec.isPending} className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
            <Check className="h-4 w-4" />{createRec.isPending ? t("saving") : t("save")}
          </button>
        </form>
      )}

      {showForm && tab === "payable" && (
        <form onSubmit={handleSubmitPay} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">Record Cheque Payable</h2>
          {formError && <div className="flex items-center gap-2 rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700"><AlertTriangle className="h-4 w-4 shrink-0" />{formError}</div>}
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">Supplier *</label>
              <select required value={payForm.supplierId} onChange={e => setPayForm(f => ({ ...f, supplierId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {(suppliers ?? []).filter((s: any) => s.isActive).map((s: any) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Cheque # *</label>
              <input required value={payForm.chequeNumber} onChange={e => setPayForm(f => ({ ...f, chequeNumber: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Bank Name *</label>
              <input required value={payForm.bankName} onChange={e => setPayForm(f => ({ ...f, bankName: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Amount *</label>
              <input required type="number" min="0.01" step="0.01" value={payForm.amount} onChange={e => setPayForm(f => ({ ...f, amount: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Currency *</label>
              <select required value={payForm.currencyId} onChange={e => setPayForm(f => ({ ...f, currencyId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {currencies?.filter(c => c.isActive).map(c => <option key={c.id} value={c.id}>{c.code}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Issue Date *</label>
              <input required type="date" value={payForm.issueDate} onChange={e => setPayForm(f => ({ ...f, issueDate: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Due Date *</label>
              <input required type="date" value={payForm.dueDate} onChange={e => setPayForm(f => ({ ...f, dueDate: e.target.value }))} className="input w-full" />
            </div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Notes</label>
              <input value={payForm.notes} onChange={e => setPayForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" />
            </div>
          </div>
          <button type="submit" disabled={createPay.isPending} className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
            <Check className="h-4 w-4" />{createPay.isPending ? t("saving") : t("save")}
          </button>
        </form>
      )}

      {/* Table */}
      {loading ? (
        <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Cheque #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{tab === "receivable" ? "Customer" : "Supplier"}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Bank</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Due Date</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Amount</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {(data ?? []).map((c: any) => {
                const st = STATUS[c.status as keyof typeof STATUS] ?? { label: "?", cls: "bg-muted" };
                return (
                  <tr key={c.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{c.chequeNumber}</td>
                    <td className="p-3 font-medium">{c.customerName ?? c.supplierName ?? "—"}</td>
                    <td className="p-3 text-muted-foreground">{c.bankName}</td>
                    <td className="p-3 text-muted-foreground">{c.dueDate}</td>
                    <td className="p-3 text-right tabular-nums">{c.amount?.toLocaleString()} {c.currencyCode}</td>
                    <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full font-medium ${st.cls}`}>{st.label}</span></td>
                    <td className="p-3 text-right">
                      <div className="flex items-center justify-end gap-1">
                        {c.status === 1 && tab === "receivable" && (
                          <button onClick={() => { setDepositingId(c.id); setDepositDate(new Date().toISOString().split("T")[0]); setDepositBankId(""); }}
                            className="text-xs px-2 py-1 rounded bg-blue-600 text-white hover:bg-blue-700">Deposit</button>
                        )}
                        {c.status === 1 && (
                          <button onClick={() => bounceMut.mutate({ id: c.id, date: new Date().toISOString().split("T")[0] })}
                            className="text-xs px-2 py-1 rounded bg-red-100 text-red-700 hover:bg-red-200">Bounce</button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
              {(data ?? []).length === 0 && (
                <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">No {tab} cheques found.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
