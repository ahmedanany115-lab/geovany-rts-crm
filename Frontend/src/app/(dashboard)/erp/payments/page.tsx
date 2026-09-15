"use client";

import { useState } from "react";
import {
  useCustomerPayments,
  useSupplierPayments,
  useCreateCustomerPayment,
  useCreateSupplierPayment,
  useCustomers,
  useSuppliers,
  useBankAccounts,
} from "@/features/erp/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { DollarSign, RefreshCw, Plus, X, Check, AlertTriangle } from "lucide-react";

/* ── Payment method config ─────────────────────────────────────────────────── */
const METHODS = [
  { value: 1, label: "Bank Transfer" },
  { value: 3, label: "Cash" },
  { value: 4, label: "InstaPay" },
];

const STATUS = {
  1: { label: "Draft",  cls: "bg-amber-100 text-amber-700" },
  2: { label: "Posted", cls: "bg-emerald-100 text-emerald-700" },
  3: { label: "Cancelled", cls: "bg-red-100 text-red-700" },
};

const INIT_FORM = {
  paymentType: "customer" as "customer" | "supplier",
  partnerId:    "",
  paymentDate:  new Date().toISOString().split("T")[0],
  currencyId:   "",
  exchangeRate: 1,
  amount:       "",
  paymentMethod: 1,   // 1=Bank, 3=Cash, 4=InstaPay
  bankAccountId: "",
  notes:         "",
};

export default function PaymentsPage() {
  const { t } = useT();
  const { toast } = useToast();
  const [tab, setTab]       = useState<"customer" | "supplier">("customer");
  const [showForm, setShowForm] = useState(false);
  const [form, setForm]     = useState(INIT_FORM);
  const [formError, setFormError] = useState<string | null>(null);

  /* ── data hooks ── */
  const { data: custPayments, isLoading: cL, refetch: cR } = useCustomerPayments({});
  const { data: suppPayments, isLoading: sL, refetch: sR } = useSupplierPayments({});
  const { data: customers  } = useCustomers({});
  const { data: suppliers  } = useSuppliers({});
  const { data: bankAccounts } = useBankAccounts({ isActive: true });
  const { data: currencies } = useCurrencies();

  const createCust = useCreateCustomerPayment();
  const createSupp = useCreateSupplierPayment();

  const data    = tab === "customer" ? custPayments : suppPayments;
  const loading = tab === "customer" ? cL : sL;
  const refetch = tab === "customer" ? cR : sR;

  const isBankMethod = form.paymentMethod === 1;

  const resetForm = () => { setForm(INIT_FORM); setFormError(null); setShowForm(false); };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!form.partnerId)            { setFormError("Select a customer or supplier."); return; }
    if (!form.currencyId)           { setFormError("Select a currency."); return; }
    if (!form.amount || Number(form.amount) <= 0) { setFormError("Amount must be > 0."); return; }
    if (isBankMethod && !form.bankAccountId) { setFormError("Select a bank account for bank payments."); return; }

    const payload = {
      paymentDate:   form.paymentDate,
      currencyId:    form.currencyId,
      exchangeRate:  form.exchangeRate,
      amount:        Number(form.amount),
      paymentMethod: form.paymentMethod,
      bankAccountId: isBankMethod ? form.bankAccountId : undefined,
      notes:         form.notes || undefined,
      invoiceIds:    [],
      ...(form.paymentType === "customer"
        ? { customerId: form.partnerId }
        : { supplierId: form.partnerId }),
    };

    try {
      if (form.paymentType === "customer") {
        await createCust.mutateAsync(payload);
      } else {
        await createSupp.mutateAsync(payload);
      }
      toast("Payment recorded successfully. Accounting entry created.", "success");
      resetForm(); refetch();
    } catch (err: any) {
      const msg = err?.message ?? "Failed to record payment.";
      setFormError(msg); toast(msg, "error");
    }
  };

  const partners = form.paymentType === "customer"
    ? (customers ?? []).filter((c: any) => c.isActive)
    : (suppliers ?? []).filter((s: any) => s.isActive);

  return (
    <div className="p-6 space-y-6">

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <DollarSign className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">{t("payments")}</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={showForm ? resetForm : () => setShowForm(true)}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : "Add Payment"}
          </button>
        </div>
      </div>

      {/* ── Add Payment form ── */}
      {showForm && (
        <form onSubmit={handleSubmit} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">New Payment</h2>
          {formError && (
            <div className="flex items-center gap-2 rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700">
              <AlertTriangle className="h-4 w-4 shrink-0" />{formError}
            </div>
          )}

          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            {/* Payment type toggle */}
            <div className="col-span-2 md:col-span-3">
              <label className="text-xs text-muted-foreground block mb-1">Payment Type *</label>
              <div className="flex rounded-lg border overflow-hidden w-fit">
                {([["customer", "Customer Receipt"], ["supplier", "Supplier Payment"]] as const).map(([k, lbl]) => (
                  <button key={k} type="button" onClick={() => setForm(f => ({ ...f, paymentType: k, partnerId: "" }))}
                    className={`px-4 py-2 text-sm font-medium transition-colors ${form.paymentType === k ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-accent"}`}>
                    {lbl}
                  </button>
                ))}
              </div>
            </div>

            {/* Partner selector */}
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">
                {form.paymentType === "customer" ? "Customer" : "Supplier"} *
              </label>
              <select required value={form.partnerId}
                onChange={e => setForm(f => ({ ...f, partnerId: e.target.value }))}
                className="input w-full">
                <option value="">Select…</option>
                {partners.map((p: any) => <option key={p.id} value={p.id}>{p.name}</option>)}
              </select>
            </div>

            {/* Date */}
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Payment Date *</label>
              <input type="date" required value={form.paymentDate}
                onChange={e => setForm(f => ({ ...f, paymentDate: e.target.value }))}
                className="input w-full" />
            </div>

            {/* Amount */}
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Amount *</label>
              <input type="number" required min="0.01" step="0.01" value={form.amount}
                onChange={e => setForm(f => ({ ...f, amount: e.target.value }))}
                className="input w-full" placeholder="0.00" />
            </div>

            {/* Currency */}
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Currency *</label>
              <select required value={form.currencyId}
                onChange={e => setForm(f => ({ ...f, currencyId: e.target.value }))}
                className="input w-full">
                <option value="">Select…</option>
                {currencies?.filter(c => c.isActive).map(c => (
                  <option key={c.id} value={c.id}>{c.code} — {c.name}</option>
                ))}
              </select>
            </div>

            {/* Payment Method */}
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Payment Method *</label>
              <select value={form.paymentMethod}
                onChange={e => setForm(f => ({ ...f, paymentMethod: Number(e.target.value), bankAccountId: "" }))}
                className="input w-full">
                {METHODS.map(m => <option key={m.value} value={m.value}>{m.label}</option>)}
              </select>
            </div>

            {/* Bank Account — shown only for Bank method */}
            {isBankMethod && (
              <div className="col-span-2">
                <label className="text-xs text-muted-foreground block mb-1">Bank Account *</label>
                <select value={form.bankAccountId}
                  onChange={e => setForm(f => ({ ...f, bankAccountId: e.target.value }))}
                  className="input w-full">
                  <option value="">Select bank account…</option>
                  {(bankAccounts ?? []).map((ba: any) => (
                    <option key={ba.id} value={ba.id}>
                      {ba.name} — {ba.bankName} ({ba.currencyCode})
                    </option>
                  ))}
                </select>
                {(bankAccounts ?? []).length === 0 && (
                  <p className="text-xs text-amber-600 mt-1">
                    No bank accounts found. Add one in Finance → Bank Accounts first.
                  </p>
                )}
              </div>
            )}

            {/* Cash/InstaPay info */}
            {!isBankMethod && (
              <div className="col-span-2 rounded-md bg-blue-50 border border-blue-100 px-3 py-2 text-xs text-blue-700">
                {form.paymentMethod === 4 ? "InstaPay" : "Cash"} payment will be recorded against the petty cash / cash-on-hand account (1101/1102).
              </div>
            )}

            {/* Notes */}
            <div className="col-span-2 md:col-span-3">
              <label className="text-xs text-muted-foreground block mb-1">Reference / Notes</label>
              <input value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
                className="input w-full" placeholder="Reference number, note…" />
            </div>
          </div>

          <div className="flex gap-2">
            <button type="submit" disabled={createCust.isPending || createSupp.isPending}
              className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
              <Check className="h-4 w-4" />
              {(createCust.isPending || createSupp.isPending) ? t("saving") : "Record Payment"}
            </button>
            <button type="button" onClick={resetForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}

      {/* Tabs */}
      <div className="flex gap-1 border-b border-border">
        {([["customer", "Customer Receipts"], ["supplier", "Supplier Payments"]] as const).map(([k, lbl]) => (
          <button key={k} onClick={() => setTab(k)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${tab === k ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}>
            {lbl}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Payment #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{tab === "customer" ? "Customer" : "Supplier"}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Method</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Amount</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {(data ?? []).map((p: any) => {
                const st = STATUS[p.status as keyof typeof STATUS] ?? { label: "?", cls: "bg-muted" };
                return (
                  <tr key={p.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{p.paymentNumber}</td>
                    <td className="p-3 font-medium">{p.customerName ?? p.supplierName}</td>
                    <td className="p-3 text-muted-foreground">{p.paymentDate}</td>
                    <td className="p-3 text-muted-foreground">
                      {METHODS.find(m => m.value === p.paymentMethod)?.label ?? p.paymentMethodName}
                    </td>
                    <td className="p-3 text-right tabular-nums font-medium">
                      {p.amount.toLocaleString()} {p.currencyCode}
                    </td>
                    <td className="p-3 text-center">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${st.cls}`}>{st.label}</span>
                    </td>
                  </tr>
                );
              })}
              {(data ?? []).length === 0 && (
                <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">No payments found.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
