"use client";

import { useState } from "react";
import {
  useSalesOrders,
  useCreateSalesOrder,
  useApproveSalesOrder,
  useCancelSalesOrder,
  useCustomers,
  useWarehouses,
  useProducts,
} from "@/features/erp/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import type { SalesOrderDto } from "@/features/erp/types";
import {
  FileCheck2, Plus, RefreshCw, X, Check, CheckCircle,
  XCircle, Trash2, AlertTriangle, ChevronDown, ChevronUp,
} from "lucide-react";

/* ── Status config ─────────────────────────────────────────────────────────── */
const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Draft / Quote",      cls: "bg-amber-100 text-amber-700" },
  2: { label: "Confirmed",          cls: "bg-blue-100 text-blue-700" },
  3: { label: "Part. Delivered",    cls: "bg-indigo-100 text-indigo-700" },
  4: { label: "Delivered",          cls: "bg-emerald-100 text-emerald-700" },
  5: { label: "Cancelled",          cls: "bg-red-100 text-red-700" },
};

/* ── Line item type ────────────────────────────────────────────────────────── */
interface LineItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  taxRate: number;
}

const EMPTY_LINE: LineItem = {
  productId: "", productName: "",
  quantity: 1, unitPrice: 0, discountPercent: 0, taxRate: 0,
};

/* ── helpers ───────────────────────────────────────────────────────────────── */
function lineTotal(l: LineItem) {
  const base  = l.quantity * l.unitPrice;
  const disc  = base * (l.discountPercent / 100);
  const net   = base - disc;
  const tax   = net * (l.taxRate / 100);
  return net + tax;
}

export default function QuotationsPage() {
  const { t } = useT();
  const { toast } = useToast();

  /* ── data ── */
  const { data: allOrders, isLoading, refetch } = useSalesOrders({});
  const { data: customers }  = useCustomers({});
  const { data: warehouses } = useWarehouses();
  const { data: products }   = useProducts({});
  const { data: currencies } = useCurrencies();

  /* ── mutations ── */
  const create  = useCreateSalesOrder();
  const approve = useApproveSalesOrder();
  const cancel  = useCancelSalesOrder();

  /* ── tabs ── */
  const [tab, setTab] = useState<"quotes" | "pipeline">("quotes");
  const quotes   = (allOrders ?? []).filter(o => o.status === 1);
  const pipeline = (allOrders ?? []).filter(o => o.status > 1 && o.status < 5);
  const rows     = tab === "quotes" ? quotes : pipeline;

  /* ── form state ── */
  const [showForm,      setShowForm]      = useState(false);
  const [formError,     setFormError]     = useState<string | null>(null);
  const [cancelTarget,  setCancelTarget]  = useState<SalesOrderDto | null>(null);
  const [expandedId,    setExpandedId]    = useState<string | null>(null);

  const [hdr, setHdr] = useState({
    customerId:   "",
    orderDate:    new Date().toISOString().split("T")[0],
    currencyId:   "",
    warehouseId:  "",
    notes:        "",
  });
  const [lines, setLines] = useState<LineItem[]>([{ ...EMPTY_LINE }]);

  const resetForm = () => {
    setHdr({ customerId: "", orderDate: new Date().toISOString().split("T")[0], currencyId: "", warehouseId: "", notes: "" });
    setLines([{ ...EMPTY_LINE }]);
    setFormError(null);
    setShowForm(false);
  };

  const setLineField = (i: number, field: keyof LineItem, value: string | number) => {
    setLines(prev => prev.map((l, idx) => idx === i ? { ...l, [field]: value } : l));
  };

  const pickProduct = (i: number, productId: string) => {
    const p = products?.find(x => x.id === productId);
    setLines(prev => prev.map((l, idx) => idx === i
      ? { ...l, productId, productName: p?.name ?? "", unitPrice: p?.salesPrice ?? 0 }
      : l));
  };

  const grandTotal = lines.reduce((s, l) => s + lineTotal(l), 0);

  /* ── submit create ── */
  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!hdr.customerId)  { setFormError("Select a customer.");  return; }
    if (!hdr.currencyId)  { setFormError("Select a currency.");  return; }
    if (!hdr.warehouseId) { setFormError("Select a warehouse."); return; }
    if (lines.some(l => !l.productId)) { setFormError("All lines need a product."); return; }
    if (lines.some(l => l.quantity <= 0)) { setFormError("Quantity must be > 0."); return; }

    try {
      await create.mutateAsync({
        ...hdr,
        exchangeRate: 1,
        lines: lines.map(l => ({
          productId:       l.productId,
          quantity:        l.quantity,
          unitPrice:       l.unitPrice,
          discountPercent: l.discountPercent,
          taxRate:         l.taxRate,
        })),
      });
      toast("Quotation created as Draft.", "success");
      resetForm();
    } catch (err: any) {
      const msg = err?.message ?? "Failed to create.";
      setFormError(msg); toast(msg, "error");
    }
  };

  /* ── confirm ── */
  const handleApprove = async (o: SalesOrderDto) => {
    try {
      await approve.mutateAsync(o.id);
      toast(`${o.soNumber} confirmed → Sales Order.`, "success");
    } catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  /* ── cancel ── */
  const handleCancel = async () => {
    if (!cancelTarget) return;
    try {
      await cancel.mutateAsync(cancelTarget.id);
      toast(`${cancelTarget.soNumber} cancelled.`, "info");
      setCancelTarget(null);
    } catch (err: any) {
      toast(err?.message ?? "Failed.", "error");
      setCancelTarget(null);
    }
  };

  return (
    <div className="p-6 space-y-6">

      {/* Cancel confirm modal */}
      {cancelTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-sm w-full space-y-4">
            <div className="flex items-center gap-3 text-red-600">
              <AlertTriangle className="h-5 w-5 shrink-0" />
              <h2 className="font-semibold">Cancel Quotation?</h2>
            </div>
            <p className="text-sm text-muted-foreground">
              Cancel <strong>{cancelTarget.soNumber}</strong> for <strong>{cancelTarget.customerName}</strong>?
              This cannot be undone.
            </p>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setCancelTarget(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Keep</button>
              <button onClick={handleCancel} disabled={cancel.isPending}
                className="px-4 py-2 rounded-lg text-sm bg-red-600 text-white hover:bg-red-700 disabled:opacity-50">
                {cancel.isPending ? "Cancelling…" : "Yes, Cancel"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <FileCheck2 className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">{t("quotations")}</h1>
            <p className="text-sm text-muted-foreground">
              {quotes.length} open · {pipeline.length} in progress
            </p>
          </div>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={showForm ? resetForm : () => setShowForm(true)}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? t("cancel") : t("new_quotation")}
          </button>
        </div>
      </div>

      {/* Create form */}
      {showForm && (
        <form onSubmit={handleCreate} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold">New Quotation (Draft)</h2>
          {formError && (
            <div className="rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700">{formError}</div>
          )}

          {/* Header fields */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">Customer *</label>
              <select value={hdr.customerId} onChange={e => setHdr(h => ({ ...h, customerId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {customers?.filter(c => c.isActive).map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Date *</label>
              <input type="date" value={hdr.orderDate} onChange={e => setHdr(h => ({ ...h, orderDate: e.target.value }))} className="input w-full" />
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Currency *</label>
              <select value={hdr.currencyId} onChange={e => setHdr(h => ({ ...h, currencyId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {currencies?.filter(c => c.isActive).map(c => <option key={c.id} value={c.id}>{c.code}</option>)}
              </select>
            </div>
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">Warehouse *</label>
              <select value={hdr.warehouseId} onChange={e => setHdr(h => ({ ...h, warehouseId: e.target.value }))} className="input w-full">
                <option value="">Select…</option>
                {warehouses?.filter(w => w.isActive).map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
              </select>
            </div>
            <div className="col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">Notes</label>
              <input value={hdr.notes} onChange={e => setHdr(h => ({ ...h, notes: e.target.value }))} className="input w-full" placeholder="Internal note…" />
            </div>
          </div>

          {/* Line items */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium">Line Items</span>
              <button type="button" onClick={() => setLines(l => [...l, { ...EMPTY_LINE }])}
                className="text-xs flex items-center gap-1 text-primary hover:underline">
                <Plus className="h-3.5 w-3.5" /> Add Line
              </button>
            </div>
            <div className="space-y-2">
              {lines.map((line, i) => (
                <div key={i} className="grid grid-cols-12 gap-2 items-end">
                  <div className="col-span-4">
                    {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Product *</label>}
                    <select value={line.productId} onChange={e => pickProduct(i, e.target.value)} className="input w-full text-sm">
                      <option value="">Select…</option>
                      {products?.filter(p => p.isActive).map(p => <option key={p.id} value={p.id}>{p.sku} — {p.name}</option>)}
                    </select>
                  </div>
                  <div className="col-span-2">
                    {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Qty *</label>}
                    <input type="number" min="0.01" step="0.01" value={line.quantity}
                      onChange={e => setLineField(i, "quantity", Number(e.target.value))}
                      className="input w-full text-sm" />
                  </div>
                  <div className="col-span-2">
                    {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Unit Price</label>}
                    <input type="number" min="0" step="0.01" value={line.unitPrice}
                      onChange={e => setLineField(i, "unitPrice", Number(e.target.value))}
                      className="input w-full text-sm" />
                  </div>
                  <div className="col-span-1">
                    {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Disc%</label>}
                    <input type="number" min="0" max="100" step="0.1" value={line.discountPercent}
                      onChange={e => setLineField(i, "discountPercent", Number(e.target.value))}
                      className="input w-full text-sm" />
                  </div>
                  <div className="col-span-1">
                    {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Tax%</label>}
                    <input type="number" min="0" max="100" step="0.1" value={line.taxRate}
                      onChange={e => setLineField(i, "taxRate", Number(e.target.value))}
                      className="input w-full text-sm" />
                  </div>
                  <div className="col-span-1">
                    {i === 0 && <label className="text-xs text-muted-foreground block mb-1">Total</label>}
                    <div className="text-sm tabular-nums text-right py-2 pr-1 font-medium">
                      {lineTotal(line).toLocaleString()}
                    </div>
                  </div>
                  <div className="col-span-1 flex items-end pb-1">
                    <button type="button" onClick={() => setLines(prev => prev.filter((_, idx) => idx !== i))}
                      disabled={lines.length === 1}
                      className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 disabled:opacity-30">
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
            <div className="text-right mt-2 font-semibold text-sm">
              Total: {grandTotal.toLocaleString()}
            </div>
          </div>

          <div className="flex gap-2">
            <button type="submit" disabled={create.isPending}
              className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2">
              <Check className="h-4 w-4" />
              {create.isPending ? t("saving") : "Save as Draft"}
            </button>
            <button type="button" onClick={resetForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">{t("cancel")}</button>
          </div>
        </form>
      )}

      {/* Info banner */}
      <div className="rounded-lg border border-blue-100 bg-blue-50 px-4 py-2.5 text-sm text-blue-800">
        <strong>Workflow:</strong> Draft = Quotation → <em>Confirm</em> converts to Sales Order → Invoice posted → AR journal entry auto-generated.
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-border">
        {([["quotes", `Open Quotes (${quotes.length})`], ["pipeline", `Pipeline (${pipeline.length})`]] as const).map(([k, lbl]) => (
          <button key={k} onClick={() => setTab(k)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${tab === k ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}>
            {lbl}
          </button>
        ))}
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>
      ) : (
        <div className="space-y-2">
          {rows.map(o => {
            const st       = STATUS[o.status] ?? { label: "?", cls: "bg-muted" };
            const expanded = expandedId === o.id;
            return (
              <div key={o.id} className="card overflow-hidden">
                <div className="flex items-center gap-3 p-3 hover:bg-muted/10">
                  {/* Expand toggle */}
                  <button onClick={() => setExpandedId(expanded ? null : o.id)}
                    className="p-1 rounded hover:bg-accent text-muted-foreground shrink-0">
                    {expanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                  </button>

                  <div className="flex-1 grid grid-cols-5 gap-2 items-center min-w-0 text-sm">
                    <span className="font-mono text-xs text-muted-foreground">{o.soNumber}</span>
                    <span className="font-semibold truncate col-span-2">{o.customerName}</span>
                    <span className="text-muted-foreground">{o.orderDate?.toString()}</span>
                    <span className="tabular-nums font-medium text-right">
                      {o.totalAmount.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 })}
                    </span>
                  </div>

                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium shrink-0 ${st.cls}`}>{st.label}</span>

                  {/* Actions */}
                  <div className="flex items-center gap-1 shrink-0">
                    {o.status === 1 && (
                      <button onClick={() => handleApprove(o)} disabled={approve.isPending}
                        title="Confirm quotation → Sales Order"
                        className="flex items-center gap-1 text-xs px-2.5 py-1.5 rounded bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50">
                        <CheckCircle className="h-3.5 w-3.5" /> Confirm
                      </button>
                    )}
                    {(o.status === 1 || o.status === 2) && (
                      <button onClick={() => setCancelTarget(o)}
                        title="Cancel"
                        className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600">
                        <XCircle className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </div>

                {/* Expanded line items */}
                {expanded && (
                  <div className="border-t bg-muted/10 p-3">
                    {o.notes && (
                      <p className="text-sm text-muted-foreground mb-2 italic">{o.notes}</p>
                    )}
                    {o.lines && o.lines.length > 0 ? (
                      <table className="w-full text-xs">
                        <thead><tr className="text-muted-foreground">
                          <th className="text-left pb-1">Product</th>
                          <th className="text-right pb-1">Qty</th>
                          <th className="text-right pb-1">Unit Price</th>
                          <th className="text-right pb-1">Disc%</th>
                          <th className="text-right pb-1">Total</th>
                        </tr></thead>
                        <tbody className="divide-y divide-border/50">
                          {o.lines.map(l => (
                            <tr key={l.id}>
                              <td className="py-1">{l.productName}</td>
                              <td className="py-1 text-right tabular-nums">{l.quantity}</td>
                              <td className="py-1 text-right tabular-nums">{l.unitPrice.toLocaleString()}</td>
                              <td className="py-1 text-right tabular-nums">{l.discountPercent > 0 ? `${l.discountPercent}%` : "—"}</td>
                              <td className="py-1 text-right tabular-nums font-medium">{l.lineTotal.toLocaleString()}</td>
                            </tr>
                          ))}
                        </tbody>
                        <tfoot><tr className="font-semibold border-t">
                          <td colSpan={4} className="pt-1 text-right text-muted-foreground">Total</td>
                          <td className="pt-1 text-right tabular-nums">{o.totalAmount.toLocaleString()}</td>
                        </tr></tfoot>
                      </table>
                    ) : (
                      <p className="text-xs text-muted-foreground">No line items loaded. View details in Sales Orders.</p>
                    )}
                  </div>
                )}
              </div>
            );
          })}
          {rows.length === 0 && (
            <div className="card p-8 text-center text-muted-foreground">
              {tab === "quotes"
                ? "No open quotations. Click \"New Quotation\" to create one."
                : "No confirmed orders in pipeline."}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
