"use client";
import { useState } from "react";
import {
  useCustomerInvoices, useCreateCustomerInvoice,
  usePostCustomerInvoice, useCreateCustomerPayment,
  useCustomers, useProducts,
} from "@/features/erp/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { usePrint } from "@/hooks/usePrint";
import { useRoles } from "@/hooks/useRoles";
import {
  Receipt, RefreshCw, CheckCircle, DollarSign, X, Plus, Printer,
  Trash2, Check, AlertTriangle, ChevronDown, ChevronUp,
} from "lucide-react";

const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Draft",     cls: "bg-muted text-muted-foreground" },
  2: { label: "Posted",    cls: "bg-blue-100 text-blue-700" },
  3: { label: "Partial",   cls: "bg-amber-100 text-amber-700" },
  4: { label: "Paid",      cls: "bg-emerald-100 text-emerald-700" },
  5: { label: "Cancelled", cls: "bg-red-100 text-red-700" },
};
const VAT_RATE = 0.14;
const EMPTY_LINE = { productId: "", description: "", quantity: 1, unitPrice: 0, discountPercent: 0, taxRateOverride: VAT_RATE * 100 };
const EGP = (v: number) => v.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 2 });

export default function CustomerInvoicesPage() {
  const { t } = useT();
  const { toast } = useToast();
  const qc = useQueryClient();
  const { isAdmin, hasRole } = useRoles();
  const canCreate = isAdmin || hasRole("Manager", "Accountant");

  const [filterStatus, setFilterStatus] = useState<number | undefined>();
  const [showForm,  setShowForm]  = useState(false);
  const [expanded,  setExpanded]  = useState<string | null>(null);
  const [payingId,  setPayingId]  = useState<string | null>(null);
  const [payAmt,    setPayAmt]    = useState("");
  const [voidTarget,setVoidTarget]= useState<any>(null);
  const [delTarget, setDelTarget] = useState<any>(null);
  const [formError, setFormError] = useState<string | null>(null);

  // Form state
  const today = new Date().toISOString().split("T")[0];
  const [form, setForm] = useState({
    customerId: "", invoiceDate: today, dueDate: "",
    currencyId: "", notes: "",
  });
  const [lines, setLines] = useState([{ ...EMPTY_LINE }]);

  const { data, isLoading, refetch } = useCustomerInvoices({ status: filterStatus });
  const { data: customers  } = useCustomers({});
  const { data: products   } = useProducts({});
  const { data: currencies } = useCurrencies();

  const create = useCreateCustomerInvoice();
  const post   = usePostCustomerInvoice();
  const record = useCreateCustomerPayment();

  const voidMut = useMutation({
    mutationFn: (id: string) => apiFetch<void>(`/customerinvoices/${id}/void`, { method: "POST" }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["customer-invoices"] }); toast("Invoice voided.", "info"); setVoidTarget(null); },
    onError: (e: any) => toast(e?.message ?? "Cannot void.", "error"),
  });
  const deleteMut = useMutation({
    mutationFn: (id: string) => apiFetch<void>(`/customerinvoices/${id}`, { method: "DELETE" }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["customer-invoices"] }); toast("Invoice deleted.", "info"); setDelTarget(null); },
    onError: (e: any) => toast(e?.message ?? "Cannot delete.", "error"),
  });

  const { printRef, handlePrint } = usePrint("Customer Invoice");

  // Line helpers
  const setLine = (i: number, k: string, v: string | number) =>
    setLines(p => p.map((l, idx) => idx === i ? { ...l, [k]: v } : l));

  const pickProduct = (i: number, pid: string) => {
    const p = products?.find((x: any) => x.id === pid);
    setLines(prev => prev.map((l, idx) => idx === i
      ? { ...l, productId: pid, description: (p as any)?.name ?? "", unitPrice: (p as any)?.salesPrice ?? 0, taxRateOverride: (p as any)?.taxRatePercent ?? VAT_RATE * 100 }
      : l));
  };

  // Totals
  const calcLine = (l: typeof lines[0]) => {
    const gross    = Number(l.quantity) * Number(l.unitPrice);
    const discount = gross * (Number(l.discountPercent) / 100);
    const taxable  = gross - discount;
    const vat      = taxable * (Number(l.taxRateOverride) / 100);
    return { gross, discount, taxable, vat, net: taxable + vat };
  };
  const totals = lines.reduce((s, l) => {
    const c = calcLine(l);
    return { subTotal: s.subTotal + c.gross, discount: s.discount + c.discount, vat: s.vat + c.vat, total: s.total + c.net };
  }, { subTotal: 0, discount: 0, vat: 0, total: 0 });

  const resetForm = () => {
    setShowForm(false); setFormError(null);
    setForm({ customerId: "", invoiceDate: today, dueDate: "", currencyId: "", notes: "" });
    setLines([{ ...EMPTY_LINE }]);
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault(); setFormError(null);
    if (!form.customerId)  { setFormError("Select a customer."); return; }
    if (!form.currencyId)  { setFormError("Select a currency."); return; }
    if (!form.dueDate)     { setFormError("Due date is required."); return; }
    if (lines.some(l => !l.productId)) { setFormError("All lines need a product."); return; }
    if (lines.some(l => Number(l.quantity) <= 0)) { setFormError("Quantity must be > 0."); return; }
    try {
      const result = await create.mutateAsync({
        customerId:   form.customerId,
        invoiceDate:  form.invoiceDate,
        dueDate:      form.dueDate,
        currencyId:   form.currencyId,
        exchangeRate: 1,
        lines: lines.map(l => ({
          productId:       l.productId,
          description:     l.description || undefined,
          quantity:        Number(l.quantity),
          unitPrice:       Number(l.unitPrice),
          discountPercent: Number(l.discountPercent),
          taxRateOverride: Number(l.taxRateOverride),
        })),
      });
      toast("Invoice created. Post it when ready.", "success");
      resetForm();
    } catch (err: any) { const msg = err?.message ?? "Failed."; setFormError(msg); toast(msg, "error"); }
  };

  const handlePost = async (id: string, num: string) => {
    try { await post.mutateAsync(id); toast(`${num} posted to accounting.`, "success"); }
    catch (err: any) { toast(err?.message ?? "Post failed.", "error"); }
  };

  const handlePay = async (inv: any) => {
    if (!payAmt || Number(payAmt) <= 0) { toast("Enter a valid amount.", "error"); return; }
    try {
      await record.mutateAsync({
        customerId: inv.customerId, paymentDate: today,
        amount: Number(payAmt), paymentMethod: 1,
        currencyId: inv.currencyId ?? currencies?.[0]?.id,
        exchangeRate: 1, invoiceIds: [inv.id],
      });
      toast("Payment recorded.", "success"); setPayingId(null); setPayAmt("");
    } catch (err: any) { toast(err?.message ?? "Payment failed.", "error"); }
  };

  const totalsBar = {
    invoiced: data?.reduce((s, i) => s + i.totalAmount, 0) ?? 0,
    paid:     data?.reduce((s, i) => s + i.paidAmount, 0)  ?? 0,
    balance:  data?.reduce((s, i) => s + i.balanceDue, 0)  ?? 0,
  };

  return (
    <div className="p-6 space-y-6">
      {/* Modals */}
      {voidTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <div className="flex items-center gap-3 text-amber-600"><AlertTriangle className="h-5 w-5" /><h2 className="font-semibold">Void Invoice?</h2></div>
            <p className="text-sm text-muted-foreground">Void <strong>{voidTarget.invoiceNumber}</strong>? Draft invoices will be cancelled. Posted invoices cannot be voided here.</p>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setVoidTarget(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={() => voidMut.mutate(voidTarget.id)} disabled={voidMut.isPending}
                className="px-4 py-2 rounded-lg text-sm bg-amber-600 text-white hover:bg-amber-700 disabled:opacity-50">Void</button>
            </div>
          </div>
        </div>
      )}
      {delTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <div className="flex items-center gap-3 text-red-600"><Trash2 className="h-5 w-5" /><h2 className="font-semibold">Delete Invoice?</h2></div>
            <p className="text-sm text-muted-foreground">Delete <strong>{delTarget.invoiceNumber}</strong>? Only Draft invoices with no payments can be deleted.</p>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setDelTarget(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={() => deleteMut.mutate(delTarget.id)} disabled={deleteMut.isPending}
                className="px-4 py-2 rounded-lg text-sm bg-red-600 text-white hover:bg-red-700 disabled:opacity-50">Delete</button>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex items-center gap-3"><Receipt className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("customer_invoices")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={handlePrint} className="btn-ghost flex items-center gap-2 px-3 py-2 rounded-lg text-sm"><Printer className="h-4 w-4" /></button>
          {canCreate && (
            <button onClick={showForm ? resetForm : () => setShowForm(true)}
              className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
              {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
              {showForm ? t("cancel") : "New Invoice"}
            </button>
          )}
        </div>
      </div>

      {/* KPI strip */}
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Total Invoiced", value: EGP(totalsBar.invoiced), cls: "text-blue-600" },
          { label: "Paid",           value: EGP(totalsBar.paid),     cls: "text-emerald-600" },
          { label: "Outstanding",    value: EGP(totalsBar.balance),   cls: "text-red-600" },
        ].map(k => (
          <div key={k.label} className="card p-4">
            <p className="text-xs text-muted-foreground">{k.label}</p>
            <p className={`text-xl font-bold tabular-nums ${k.cls}`}>{k.value}</p>
          </div>
        ))}
      </div>

      {/* Create Invoice form */}
      {showForm && canCreate && (
        <form onSubmit={handleCreate} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">New Customer Invoice</h2>
          {formError && <div className="flex items-center gap-2 bg-red-50 border border-red-200 rounded-md px-4 py-2 text-sm text-red-700"><AlertTriangle className="h-4 w-4 shrink-0"/>{formError}</div>}

          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Customer *</label>
              <select required value={form.customerId} onChange={e => setForm(f => ({ ...f, customerId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {customers?.filter((c: any) => c.isActive).map((c: any) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Invoice Date *</label>
              <input type="date" required value={form.invoiceDate} onChange={e => setForm(f => ({ ...f, invoiceDate: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Due Date *</label>
              <input type="date" required value={form.dueDate} min={form.invoiceDate} onChange={e => setForm(f => ({ ...f, dueDate: e.target.value }))} className="input w-full" />
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Currency *</label>
              <select required value={form.currencyId} onChange={e => setForm(f => ({ ...f, currencyId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {currencies?.filter(c => c.isActive).map(c => <option key={c.id} value={c.id}>{c.code} — {c.name}</option>)}
              </select>
            </div>
          </div>

          {/* Line items */}
          <div>
            <div className="grid grid-cols-12 gap-2 text-xs font-medium text-muted-foreground pb-1 border-b">
              <span className="col-span-4">Product</span>
              <span className="col-span-2">Description</span>
              <span className="col-span-1">Qty</span>
              <span className="col-span-1 text-right">Price</span>
              <span className="col-span-1 text-right">Disc%</span>
              <span className="col-span-1 text-right">VAT%</span>
              <span className="col-span-1 text-right">Total</span>
              <span className="col-span-1"></span>
            </div>
            <div className="space-y-1.5 mt-2">
              {lines.map((l, i) => {
                const c = calcLine(l);
                return (
                  <div key={i} className="grid grid-cols-12 gap-2 items-center">
                    <div className="col-span-4">
                      <select value={l.productId} onChange={e => pickProduct(i, e.target.value)} className="input w-full text-sm py-1.5">
                        <option value="">Select product…</option>
                        {products?.filter((p: any) => p.isActive).map((p: any) => <option key={p.id} value={p.id}>{p.sku} — {p.name}</option>)}
                      </select>
                    </div>
                    <div className="col-span-2">
                      <input value={l.description} onChange={e => setLine(i, "description", e.target.value)} className="input w-full text-sm py-1.5" placeholder="Note…" />
                    </div>
                    <div className="col-span-1">
                      <input type="number" min="0.01" step="0.01" value={l.quantity} onChange={e => setLine(i, "quantity", e.target.value)} className="input w-full text-sm py-1.5" />
                    </div>
                    <div className="col-span-1">
                      <input type="number" min="0" step="0.01" value={l.unitPrice} onChange={e => setLine(i, "unitPrice", e.target.value)} className="input w-full text-sm py-1.5 text-right" />
                    </div>
                    <div className="col-span-1">
                      <input type="number" min="0" max="100" step="0.1" value={l.discountPercent} onChange={e => setLine(i, "discountPercent", e.target.value)} className="input w-full text-sm py-1.5 text-right" />
                    </div>
                    <div className="col-span-1">
                      <input type="number" min="0" max="100" step="0.1" value={l.taxRateOverride} onChange={e => setLine(i, "taxRateOverride", e.target.value)} className="input w-full text-sm py-1.5 text-right" />
                    </div>
                    <div className="col-span-1 text-right text-sm tabular-nums font-medium">{EGP(c.net)}</div>
                    <div className="col-span-1 flex justify-end">
                      <button type="button" onClick={() => setLines(p => p.length > 1 ? p.filter((_,idx) => idx !== i) : p)} disabled={lines.length <= 1}
                        className="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 disabled:opacity-20">
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
            <button type="button" onClick={() => setLines(l => [...l, { ...EMPTY_LINE }])}
              className="mt-2 text-xs flex items-center gap-1 text-primary hover:underline">
              <Plus className="h-3.5 w-3.5" /> Add Line
            </button>

            {/* Totals */}
            <div className="mt-3 space-y-0.5 text-sm border-t pt-2">
              {[
                ["Subtotal",  EGP(totals.subTotal)],
                ["Discount",  totals.discount > 0 ? `(${EGP(totals.discount)})` : "—"],
                ["VAT",       EGP(totals.vat)],
              ].map(([l, v]) => (
                <div key={l} className="flex justify-between text-muted-foreground">
                  <span>{l}</span><span className="tabular-nums">{v}</span>
                </div>
              ))}
              <div className="flex justify-between font-bold text-base border-t pt-1">
                <span>Total</span><span className="tabular-nums">{EGP(totals.total)}</span>
              </div>
            </div>
          </div>

          <div className="flex gap-2">
            <button type="submit" disabled={create.isPending}
              className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
              <Check className="h-4 w-4" />{create.isPending ? t("saving") : "Save Invoice"}
            </button>
            <button type="button" onClick={resetForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}

      {/* Status filter */}
      <div className="flex gap-3">
        <select value={filterStatus ?? ""} onChange={e => setFilterStatus(e.target.value ? Number(e.target.value) : undefined)} className="input text-sm">
          <option value="">All Statuses</option>
          {Object.entries(STATUS).map(([v, s]) => <option key={v} value={v}>{s.label}</option>)}
        </select>
      </div>

      {/* Invoice table */}
      <div ref={printRef}>
        <div className="print-header hidden">
          <div className="print-header-text"><h1>Royal Technology System</h1><p>Customer Invoices</p></div>
        </div>
        {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30"><tr>
                <th className="p-3 w-8"></th>
                <th className="text-left p-3 font-medium text-muted-foreground">Invoice #</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Due</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Total</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Balance</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
                <th className="p-3"></th>
              </tr></thead>
              <tbody className="divide-y divide-border">
                {(data ?? []).map(inv => {
                  const st = STATUS[inv.status as keyof typeof STATUS] ?? { label: "?", cls: "bg-muted" };
                  const isExpanded = expanded === inv.id;
                  return (
                    <>
                      <tr key={inv.id} className="hover:bg-muted/20">
                        <td className="p-3">
                          <button onClick={() => setExpanded(v => v === inv.id ? null : inv.id)} className="p-1 rounded hover:bg-accent text-muted-foreground">
                            {isExpanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                          </button>
                        </td>
                        <td className="p-3 font-mono text-xs">{inv.invoiceNumber}</td>
                        <td className="p-3 font-medium">{inv.customerName}</td>
                        <td className="p-3 text-muted-foreground text-xs">{inv.invoiceDate?.toString()}</td>
                        <td className="p-3 text-muted-foreground text-xs">{inv.dueDate?.toString()}</td>
                        <td className="p-3 text-right tabular-nums">{EGP(inv.totalAmount)}</td>
                        <td className={`p-3 text-right tabular-nums font-semibold ${inv.balanceDue > 0 ? "text-red-600" : "text-emerald-600"}`}>
                          {inv.balanceDue > 0 ? EGP(inv.balanceDue) : "Paid"}
                        </td>
                        <td className="p-3 text-center">
                          <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${st.cls}`}>{st.label}</span>
                        </td>
                        <td className="p-3 text-right no-print">
                          <div className="flex items-center justify-end gap-1">
                            {inv.status === 1 && canCreate && (
                              <button onClick={() => handlePost(inv.id, inv.invoiceNumber)} disabled={post.isPending}
                                className="text-xs px-2 py-1.5 rounded bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50 flex items-center gap-1">
                                <CheckCircle className="h-3.5 w-3.5" /> Post
                              </button>
                            )}
                            {inv.status === 2 && inv.balanceDue > 0 && (
                              <button onClick={() => { setPayingId(inv.id); setPayAmt(String(inv.balanceDue)); }}
                                className="text-xs px-2 py-1.5 rounded bg-blue-600 text-white hover:bg-blue-700 flex items-center gap-1">
                                <DollarSign className="h-3.5 w-3.5" /> Pay
                              </button>
                            )}
                            {inv.status === 1 && (
                              <button onClick={() => setVoidTarget(inv)}
                                className="text-xs px-2 py-1 rounded bg-amber-100 text-amber-700 hover:bg-amber-200">Void</button>
                            )}
                            {inv.status === 1 && canCreate && (
                              <button onClick={() => setDelTarget(inv)}
                                className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600">
                                <Trash2 className="h-3.5 w-3.5" />
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                      {isExpanded && (
                        <tr key={`${inv.id}-details`}>
                          <td colSpan={9} className="bg-muted/10 px-6 py-3 border-b">
                            <table className="w-full text-xs">
                              <thead><tr className="text-muted-foreground">
                                <th className="text-left pb-1">Product</th>
                                <th className="text-right pb-1">Qty</th>
                                <th className="text-right pb-1">Unit Price</th>
                                <th className="text-right pb-1">Disc%</th>
                                <th className="text-right pb-1">VAT</th>
                                <th className="text-right pb-1">Line Total</th>
                              </tr></thead>
                              <tbody>
                                {(inv as any).lines?.map((l: any, i: number) => (
                                  <tr key={i} className="border-t border-border/30">
                                    <td className="py-1">{l.productName ?? l.description ?? "—"}</td>
                                    <td className="py-1 text-right">{l.quantity}</td>
                                    <td className="py-1 text-right tabular-nums">{EGP(l.unitPrice)}</td>
                                    <td className="py-1 text-right">{l.discountPercent > 0 ? `${l.discountPercent}%` : "—"}</td>
                                    <td className="py-1 text-right tabular-nums">{EGP(l.taxAmount ?? 0)}</td>
                                    <td className="py-1 text-right tabular-nums font-semibold">{EGP(l.netAmount ?? l.lineTotal)}</td>
                                  </tr>
                                ))}
                              </tbody>
                              <tfoot className="border-t-2 border-border">
                                <tr>
                                  <td colSpan={5} className="pt-1 text-right font-semibold">Total</td>
                                  <td className="pt-1 text-right tabular-nums font-bold">{EGP(inv.totalAmount)}</td>
                                </tr>
                              </tfoot>
                            </table>
                          </td>
                        </tr>
                      )}
                    </>
                  );
                })}
                {!(data?.length) && <tr><td colSpan={9} className="p-8 text-center text-muted-foreground">No invoices found.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Pay modal */}
      {payingId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <h2 className="font-semibold">Record Payment</h2>
            <div><label className="text-xs text-muted-foreground block mb-1">Amount *</label>
              <input type="number" min="0.01" step="0.01" value={payAmt} onChange={e => setPayAmt(e.target.value)} className="input w-full" autoFocus />
            </div>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setPayingId(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
              <button onClick={() => handlePay(data?.find(i => i.id === payingId))} disabled={record.isPending}
                className="btn-primary px-4 py-2 rounded-lg text-sm disabled:opacity-50">
                {record.isPending ? "Saving…" : "Record"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
