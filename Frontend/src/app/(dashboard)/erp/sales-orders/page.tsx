"use client";
import { useState } from "react";
import {
  useSalesOrders, useApproveSalesOrder, useCreateSalesOrder,
  useCustomers, useProducts, useWarehouses,
} from "@/features/erp/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { usePrint } from "@/hooks/usePrint";
import { useRoles } from "@/hooks/useRoles";
import {
  ShoppingCart, RefreshCw, CheckCircle, FileText, Printer,
  Plus, X, Trash2,
} from "lucide-react";

const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Draft",     cls: "bg-muted text-muted-foreground" },
  2: { label: "Confirmed", cls: "bg-blue-100 text-blue-700" },
  3: { label: "Delivered", cls: "bg-emerald-100 text-emerald-700" },
  4: { label: "Invoiced",  cls: "bg-purple-100 text-purple-700" },
  5: { label: "Cancelled", cls: "bg-red-100 text-red-700" },
};

const EMPTY_LINE = { productId: "", quantity: 1, unitPrice: 0, discountPercent: 0 };

export default function SalesOrdersPage() {
  const { t } = useT();
  const { toast } = useToast();
  const qc = useQueryClient();
  const { isAdmin, hasRole } = useRoles();
  const canCreate = isAdmin || hasRole("Manager", "Sales", "SalesManager");

  const { data, isLoading, refetch } = useSalesOrders({});
  const approve  = useApproveSalesOrder();
  const create   = useCreateSalesOrder();
  const { printRef, handlePrint } = usePrint("RTS ERP — Sales Orders");

  const [showForm,   setShowForm]   = useState(false);
  const [formError,  setFormError]  = useState<string | null>(null);

  const today = new Date().toISOString().split("T")[0];
  const [form, setForm] = useState({
    customerId: "", orderDate: today, currencyId: "",
    exchangeRate: "1", warehouseId: "", notes: "",
  });
  const [lines, setLines] = useState([{ ...EMPTY_LINE }]);

  const { data: customers  } = useCustomers({});
  const { data: products   } = useProducts({});
  const { data: warehouses } = useWarehouses({});
  const { data: currencies } = useCurrencies();

  const generateInvoice = useMutation({
    mutationFn: (id: string) =>
      apiFetch<{ invoiceId: string; invoiceNumber: string }>(
        `/salesorders/${id}/generate-invoice`, { method: "POST" }
      ),
    onSuccess: (r) => {
      toast(`Invoice ${r.invoiceNumber} created. Go to Customer Invoices to post it.`, "success");
      qc.invalidateQueries({ queryKey: ["customer-invoices"] });
    },
    onError: (e: any) => toast(e?.message ?? "Failed.", "error"),
  });

  const handleApprove = async (id: string) => {
    try { await approve.mutateAsync(id); toast("Order confirmed.", "success"); }
    catch (e: any) { toast(e?.message ?? "Failed.", "error"); }
  };

  // ── Line helpers ─────────────────────────────────────────────────────────────
  const setLine = (i: number, k: string, v: string | number) =>
    setLines(p => p.map((l, idx) => idx === i ? { ...l, [k]: v } : l));

  const pickProduct = (i: number, pid: string) => {
    const p = products?.find((x: any) => x.id === pid);
    setLines(prev => prev.map((l, idx) => idx === i
      ? { ...l, productId: pid, unitPrice: (p as any)?.salesPrice ?? 0 }
      : l));
  };

  // ── Totals ───────────────────────────────────────────────────────────────────
  const calcLine = (l: typeof lines[0]) => {
    const gross    = Number(l.quantity) * Number(l.unitPrice);
    const discount = gross * (Number(l.discountPercent) / 100);
    return gross - discount;
  };
  const grandTotal = lines.reduce((s, l) => s + calcLine(l), 0);

  const resetForm = () => {
    setShowForm(false); setFormError(null);
    setForm({ customerId: "", orderDate: today, currencyId: "", exchangeRate: "1", warehouseId: "", notes: "" });
    setLines([{ ...EMPTY_LINE }]);
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault(); setFormError(null);
    if (!form.customerId)  { setFormError("Select a customer.");  return; }
    if (!form.currencyId)  { setFormError("Select a currency.");  return; }
    if (!form.warehouseId) { setFormError("Select a warehouse."); return; }
    if (lines.some(l => !l.productId)) { setFormError("All lines need a product."); return; }
    if (lines.some(l => Number(l.quantity) <= 0)) { setFormError("Quantity must be > 0."); return; }
    try {
      await create.mutateAsync({
        customerId:   form.customerId,
        orderDate:    form.orderDate,
        currencyId:   form.currencyId,
        exchangeRate: Number(form.exchangeRate) || 1,
        warehouseId:  form.warehouseId,
        notes:        form.notes || undefined,
        lines: lines.map(l => ({
          productId:       l.productId,
          quantity:        Number(l.quantity),
          unitPrice:       Number(l.unitPrice),
          discountPercent: Number(l.discountPercent),
        })),
      });
      toast("Sales order created as Draft.", "success");
      resetForm();
    } catch (err: any) {
      const msg = err?.message ?? "Failed to create order.";
      setFormError(msg); toast(msg, "error");
    }
  };

  return (
    <div className="p-6 space-y-6">
      {/* ── Header ─────────────────────────────────────────────────────────── */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <ShoppingCart className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">{t("sales_orders")}</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={handlePrint} className="btn-ghost flex items-center gap-2 px-3 py-2 rounded-lg text-sm">
            <Printer className="h-4 w-4" />
          </button>
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg">
            <RefreshCw className="h-4 w-4" />
          </button>
          {canCreate && !showForm && (
            <button
              onClick={() => setShowForm(true)}
              className="flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90"
            >
              <Plus className="h-4 w-4" /> New Sales Order
            </button>
          )}
        </div>
      </div>

      {/* ── Create Form ─────────────────────────────────────────────────────── */}
      {showForm && (
        <div className="card p-6 space-y-5">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold">New Sales Order</h2>
            <button onClick={resetForm} className="p-1 rounded hover:bg-accent">
              <X className="h-5 w-5" />
            </button>
          </div>

          <form onSubmit={handleCreate} className="space-y-5">
            {/* ── Header fields ── */}
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {/* Customer */}
              <div className="space-y-1">
                <label className="text-sm font-medium text-foreground">Customer *</label>
                <select
                  value={form.customerId}
                  onChange={e => setForm(p => ({ ...p, customerId: e.target.value }))}
                  className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                  required
                >
                  <option value="">Select customer…</option>
                  {customers?.map((c: any) => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </div>

              {/* Order Date */}
              <div className="space-y-1">
                <label className="text-sm font-medium text-foreground">Order Date *</label>
                <input
                  type="date"
                  value={form.orderDate}
                  onChange={e => setForm(p => ({ ...p, orderDate: e.target.value }))}
                  className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                  required
                />
              </div>

              {/* Currency */}
              <div className="space-y-1">
                <label className="text-sm font-medium text-foreground">Currency *</label>
                <select
                  value={form.currencyId}
                  onChange={e => setForm(p => ({ ...p, currencyId: e.target.value }))}
                  className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                  required
                >
                  <option value="">Select currency…</option>
                  {currencies?.map((c: any) => (
                    <option key={c.id} value={c.id}>{c.code} — {c.name}</option>
                  ))}
                </select>
              </div>

              {/* Exchange Rate */}
              <div className="space-y-1">
                <label className="text-sm font-medium text-foreground">Exchange Rate</label>
                <input
                  type="number"
                  min="0.0001"
                  step="0.0001"
                  value={form.exchangeRate}
                  onChange={e => setForm(p => ({ ...p, exchangeRate: e.target.value }))}
                  className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                />
              </div>

              {/* Warehouse */}
              <div className="space-y-1">
                <label className="text-sm font-medium text-foreground">Warehouse *</label>
                <select
                  value={form.warehouseId}
                  onChange={e => setForm(p => ({ ...p, warehouseId: e.target.value }))}
                  className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                  required
                >
                  <option value="">Select warehouse…</option>
                  {warehouses?.map((w: any) => (
                    <option key={w.id} value={w.id}>{w.name}</option>
                  ))}
                </select>
              </div>

              {/* Notes */}
              <div className="space-y-1">
                <label className="text-sm font-medium text-foreground">Notes</label>
                <input
                  type="text"
                  value={form.notes}
                  onChange={e => setForm(p => ({ ...p, notes: e.target.value }))}
                  placeholder="Optional reference / notes"
                  className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm"
                />
              </div>
            </div>

            {/* ── Line Items ── */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-foreground">Line Items</h3>
                <button
                  type="button"
                  onClick={() => setLines(p => [...p, { ...EMPTY_LINE }])}
                  className="flex items-center gap-1 text-xs px-2 py-1 rounded hover:bg-accent text-primary"
                >
                  <Plus className="h-3.5 w-3.5" /> Add Line
                </button>
              </div>

              {/* Header row */}
              <div className="hidden sm:grid grid-cols-12 gap-2 text-xs font-medium text-muted-foreground px-1">
                <div className="col-span-4">Product</div>
                <div className="col-span-2 text-right">Qty</div>
                <div className="col-span-3 text-right">Unit Price</div>
                <div className="col-span-2 text-right">Disc %</div>
                <div className="col-span-1"></div>
              </div>

              {lines.map((line, i) => (
                <div key={i} className="grid grid-cols-12 gap-2 items-center">
                  {/* Product */}
                  <div className="col-span-12 sm:col-span-4">
                    <select
                      value={line.productId}
                      onChange={e => pickProduct(i, e.target.value)}
                      className="w-full rounded border border-border bg-background px-2 py-1.5 text-sm"
                      required
                    >
                      <option value="">Select product…</option>
                      {products?.map((p: any) => (
                        <option key={p.id} value={p.id}>{p.name}</option>
                      ))}
                    </select>
                  </div>
                  {/* Qty */}
                  <div className="col-span-4 sm:col-span-2">
                    <input
                      type="number" min="0.001" step="0.001"
                      value={line.quantity}
                      onChange={e => setLine(i, "quantity", e.target.value)}
                      className="w-full rounded border border-border bg-background px-2 py-1.5 text-sm text-right"
                      required
                    />
                  </div>
                  {/* Unit Price */}
                  <div className="col-span-4 sm:col-span-3">
                    <input
                      type="number" min="0" step="0.01"
                      value={line.unitPrice}
                      onChange={e => setLine(i, "unitPrice", e.target.value)}
                      className="w-full rounded border border-border bg-background px-2 py-1.5 text-sm text-right"
                      required
                    />
                  </div>
                  {/* Discount % */}
                  <div className="col-span-3 sm:col-span-2">
                    <input
                      type="number" min="0" max="100" step="0.01"
                      value={line.discountPercent}
                      onChange={e => setLine(i, "discountPercent", e.target.value)}
                      className="w-full rounded border border-border bg-background px-2 py-1.5 text-sm text-right"
                    />
                  </div>
                  {/* Remove */}
                  <div className="col-span-1 flex justify-end">
                    {lines.length > 1 && (
                      <button
                        type="button"
                        onClick={() => setLines(p => p.filter((_, idx) => idx !== i))}
                        className="p-1 rounded hover:bg-red-50 text-red-500"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    )}
                  </div>
                </div>
              ))}

              {/* Totals row */}
              <div className="flex justify-end pt-2 border-t border-border">
                <div className="text-sm space-y-1 min-w-[200px]">
                  <div className="flex justify-between font-semibold text-foreground">
                    <span>Estimated Total</span>
                    <span className="tabular-nums">{grandTotal.toLocaleString("en-EG", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                  </div>
                  <p className="text-xs text-muted-foreground">(excl. tax — tax applied by product)</p>
                </div>
              </div>
            </div>

            {/* ── Error + Submit ── */}
            {formError && (
              <p className="text-sm text-red-600 bg-red-50 rounded-md px-3 py-2">{formError}</p>
            )}
            <div className="flex justify-end gap-3">
              <button type="button" onClick={resetForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">
                Cancel
              </button>
              <button
                type="submit"
                disabled={create.isPending}
                className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 disabled:opacity-50"
              >
                {create.isPending ? "Creating…" : "Create Sales Order"}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* ── Orders Table ──────────────────────────────────────────────────────── */}
      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>
      ) : (
        <div ref={printRef} className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30">
              <tr>
                <th className="text-left p-3 font-medium text-muted-foreground">SO#</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
                <th className="text-left p-3 font-medium text-muted-foreground">{t("date")}</th>
                <th className="text-right p-3 font-medium text-muted-foreground">{t("total")}</th>
                <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
                <th className="p-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {data?.map(o => {
                const st = STATUS[o.status] ?? { label: "?", cls: "bg-muted" };
                return (
                  <tr key={o.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{o.soNumber}</td>
                    <td className="p-3 font-medium">{o.customerName}</td>
                    <td className="p-3 text-muted-foreground">{o.orderDate}</td>
                    <td className="p-3 text-right tabular-nums">{o.totalAmount.toLocaleString()}</td>
                    <td className="p-3 text-center">
                      <span className={`text-xs px-2 py-0.5 rounded-full ${st.cls}`}>{st.label}</span>
                    </td>
                    <td className="p-3 text-right flex items-center justify-end gap-2">
                      {o.status === 1 && (
                        <button
                          onClick={() => handleApprove(o.id)}
                          disabled={approve.isPending}
                          className="flex items-center gap-1 text-xs px-2 py-1 rounded hover:bg-accent text-emerald-600"
                        >
                          <CheckCircle className="h-3.5 w-3.5" />{t("approve")}
                        </button>
                      )}
                      {(o.status === 2 || o.status === 3) && (
                        <button
                          onClick={() => generateInvoice.mutate(o.id)}
                          disabled={generateInvoice.isPending}
                          className="flex items-center gap-1 text-xs px-2 py-1 rounded bg-blue-100 text-blue-700 hover:bg-blue-200 disabled:opacity-50"
                        >
                          <FileText className="h-3.5 w-3.5" /> Generate Invoice
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
              {!data?.length && (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-muted-foreground">{t("no_data")}</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
