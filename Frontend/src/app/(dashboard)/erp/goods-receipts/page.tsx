"use client";

import { useState, useRef, useCallback } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useWarehouses, useProducts } from "@/features/erp/hooks";
import { useToast } from "@/components/ui/toast";
import { usePrint } from "@/hooks/usePrint";
import {
  Package, RefreshCw, Plus, X, Trash2, Edit2, Printer, Eye,
  Check, AlertTriangle,
} from "lucide-react";

// ── Types ─────────────────────────────────────────────────────────────────────

interface GoodsReceiptItem {
  id?: string;
  itemDescription: string;
  productId?: string;
  productName?: string;
  quantity: number;
  serialNumber?: string;
  unit?: string;
  notes?: string;
  sortOrder: number;
}

interface GoodsReceipt {
  id: string;
  receiptNumber: string;
  receiptDate: string;
  supplier?: string;
  purchaseOrderReference?: string;
  warehouseId: string;
  warehouseName: string;
  preparedBy?: string;
  receivedBy?: string;
  notes?: string;
  status: number;
  items: GoodsReceiptItem[];
  createdAt: string;
}

// ── Status config ─────────────────────────────────────────────────────────────

const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Draft",     cls: "bg-yellow-100 text-yellow-700" },
  2: { label: "Confirmed", cls: "bg-emerald-100 text-emerald-700" },
  3: { label: "Cancelled", cls: "bg-red-100 text-red-700" },
};

// ── Empty helpers ─────────────────────────────────────────────────────────────

const EMPTY_ITEM = (): GoodsReceiptItem => ({
  itemDescription: "",
  productId: "",
  quantity: 1,
  serialNumber: "",
  unit: "",
  notes: "",
  sortOrder: 1,
});

const EMPTY_FORM = () => ({
  receiptDate: new Date().toISOString().split("T")[0],
  supplier: "",
  purchaseOrderReference: "",
  warehouseId: "",
  preparedBy: "",
  receivedBy: "",
  notes: "",
});

// ── Print view component ──────────────────────────────────────────────────────

function PrintView({ receipt }: { receipt: GoodsReceipt }) {
  return (
    <div className="p-8 font-mono text-sm print:block">
      <h1 className="text-2xl font-bold mb-4">GOODS RECEIPT</h1>
      <div className="grid grid-cols-2 gap-x-8 gap-y-1 mb-4">
        <div><span className="font-semibold">Document:</span> {receipt.receiptNumber}</div>
        <div><span className="font-semibold">Date:</span> {new Date(receipt.receiptDate).toLocaleDateString("en-GB")}</div>
        <div><span className="font-semibold">Supplier:</span> {receipt.supplier ?? "—"}</div>
        <div><span className="font-semibold">Warehouse:</span> {receipt.warehouseName}</div>
        <div><span className="font-semibold">Prepared By:</span> {receipt.preparedBy ?? "—"}</div>
        <div><span className="font-semibold">Received By:</span> {receipt.receivedBy ?? "—"}</div>
        {receipt.purchaseOrderReference && (
          <div className="col-span-2"><span className="font-semibold">Reference:</span> {receipt.purchaseOrderReference}</div>
        )}
      </div>
      <table className="w-full border-collapse text-xs mb-4">
        <thead>
          <tr className="border-b-2 border-black">
            <th className="text-left py-1 pr-2 w-8">No.</th>
            <th className="text-left py-1 pr-2">Item Description</th>
            <th className="text-right py-1 pr-2 w-16">Qty</th>
            <th className="text-left py-1 pr-2 w-28">Serial No.</th>
            <th className="text-left py-1 pr-2 w-16">Unit</th>
            <th className="text-left py-1">Notes</th>
          </tr>
        </thead>
        <tbody>
          {receipt.items.map((item, idx) => (
            <tr key={idx} className="border-b border-gray-300">
              <td className="py-1 pr-2">{idx + 1}</td>
              <td className="py-1 pr-2">{item.itemDescription}</td>
              <td className="py-1 pr-2 text-right">{item.quantity}</td>
              <td className="py-1 pr-2">{item.serialNumber ?? "—"}</td>
              <td className="py-1 pr-2">{item.unit ?? "—"}</td>
              <td className="py-1">{item.notes ?? "—"}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {receipt.notes && (
        <div><span className="font-semibold">Notes:</span> {receipt.notes}</div>
      )}
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export default function GoodsReceiptsPage() {
  const { toast } = useToast();
  const qc = useQueryClient();
  const { printRef, handlePrint } = usePrint("Goods Receipt");

  // ── Data fetching ────────────────────────────────────────────────────────────
  const { data: receipts, isLoading, refetch } = useQuery({
    queryKey: ["goods-receipts"],
    queryFn: () => apiFetch<GoodsReceipt[]>("/goodsreceipts"),
  });
  const { data: warehouses } = useWarehouses();
  const { data: products }   = useProducts({});

  // ── Mutations ────────────────────────────────────────────────────────────────
  const createMut = useMutation({
    mutationFn: (data: unknown) => apiFetch<GoodsReceipt>("/goodsreceipts", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["goods-receipts"] }); toast("Goods Receipt created.", "success"); },
    onError: (err: any) => toast(err?.message ?? "Failed to create.", "error"),
  });
  const updateMut = useMutation({
    mutationFn: ({ id, data }: { id: string; data: unknown }) =>
      apiFetch<GoodsReceipt>(`/goodsreceipts/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["goods-receipts"] }); toast("Goods Receipt updated.", "success"); },
    onError: (err: any) => toast(err?.message ?? "Failed to update.", "error"),
  });
  const deleteMut = useMutation({
    mutationFn: (id: string) => apiFetch<void>(`/goodsreceipts/${id}`, { method: "DELETE" }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["goods-receipts"] }); toast("Goods Receipt deleted.", "success"); },
    onError: (err: any) => toast(err?.message ?? "Failed to delete.", "error"),
  });

  // ── Local UI state ───────────────────────────────────────────────────────────
  const [showForm, setShowForm]       = useState(false);
  const [editId, setEditId]           = useState<string | null>(null);
  const [form, setForm]               = useState(EMPTY_FORM());
  const [items, setItems]             = useState<GoodsReceiptItem[]>([EMPTY_ITEM()]);
  const [formError, setFormError]     = useState<string | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);
  const [viewReceipt, setViewReceipt] = useState<GoodsReceipt | null>(null);

  // ── Form helpers ─────────────────────────────────────────────────────────────
  const resetForm = () => {
    setForm(EMPTY_FORM()); setItems([EMPTY_ITEM()]); setFormError(null);
    setShowForm(false); setEditId(null);
  };

  const openCreate = () => { resetForm(); setShowForm(true); };

  const openEdit = (r: GoodsReceipt) => {
    setEditId(r.id);
    setForm({
      receiptDate: r.receiptDate?.split("T")[0] ?? new Date().toISOString().split("T")[0],
      supplier: r.supplier ?? "",
      purchaseOrderReference: r.purchaseOrderReference ?? "",
      warehouseId: r.warehouseId,
      preparedBy: r.preparedBy ?? "",
      receivedBy: r.receivedBy ?? "",
      notes: r.notes ?? "",
    });
    setItems(r.items?.length ? r.items.map(it => ({ ...it })) : [EMPTY_ITEM()]);
    setFormError(null);
    setShowForm(true);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const setItem = (i: number, field: keyof GoodsReceiptItem, val: string | number) =>
    setItems(prev => prev.map((it, idx) => idx === i ? { ...it, [field]: val } : it));

  const pickProduct = (i: number, pid: string) => {
    const p = products?.find((x: any) => x.id === pid);
    setItems(prev => prev.map((it, idx) =>
      idx === i ? { ...it, productId: pid, itemDescription: it.itemDescription || ((p as any)?.name ?? "") } : it
    ));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setFormError(null);
    if (!form.warehouseId) { setFormError("Please select a warehouse."); return; }
    if (items.some(it => !it.itemDescription.trim())) { setFormError("All items need a description."); return; }
    const payload = {
      ...form,
      items: items.map((it, i) => ({
        ...it,
        quantity: Number(it.quantity),
        sortOrder: i + 1,
        productId: it.productId || undefined,
      })),
    };
    if (editId) {
      await updateMut.mutateAsync({ id: editId, data: payload });
    } else {
      await createMut.mutateAsync(payload);
    }
    resetForm();
  };

  const handleDelete = async (id: string) => {
    await deleteMut.mutateAsync(id);
    setDeleteConfirm(null);
  };

  const openPrint = (r: GoodsReceipt) => {
    setViewReceipt(r);
    setTimeout(() => handlePrint(), 100);
  };

  const isPending = createMut.isPending || updateMut.isPending;

  // ── Print hidden layer ────────────────────────────────────────────────────────
  return (
    <>
      {/* Hidden print area */}
      <div className="hidden print:block" ref={printRef}>
        {viewReceipt && <PrintView receipt={viewReceipt} />}
      </div>

      <div className="p-6 space-y-6 print:hidden">
        {/* ── Header ── */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Package className="h-6 w-6 text-primary" />
            <h1 className="text-2xl font-semibold">Goods Receipts</h1>
          </div>
          <div className="flex gap-2">
            <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg" title="Refresh">
              <RefreshCw className="h-4 w-4" />
            </button>
            <button
              onClick={showForm ? resetForm : openCreate}
              className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"
            >
              {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
              {showForm ? "Cancel" : "New Receipt"}
            </button>
          </div>
        </div>

        {/* ── Create / Edit Form ── */}
        {showForm && (
          <form onSubmit={handleSubmit} className="card p-5 space-y-4 border border-primary/20">
            <h2 className="font-semibold text-sm">{editId ? "Edit Goods Receipt" : "New Goods Receipt"}</h2>
            {formError && (
              <div className="rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700 flex items-center gap-2">
                <AlertTriangle className="h-4 w-4 shrink-0" /> {formError}
              </div>
            )}

            <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Receipt Date *</label>
                <input
                  type="date" required value={form.receiptDate}
                  onChange={e => setForm(f => ({ ...f, receiptDate: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div className="col-span-2 md:col-span-1">
                <label className="text-xs text-muted-foreground block mb-1">Warehouse *</label>
                <select
                  required value={form.warehouseId}
                  onChange={e => setForm(f => ({ ...f, warehouseId: e.target.value }))}
                  className="input w-full"
                >
                  <option value="">Select warehouse…</option>
                  {warehouses?.filter((w: any) => w.isActive).map((w: any) => (
                    <option key={w.id} value={w.id}>{w.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Supplier</label>
                <input
                  type="text" value={form.supplier} placeholder="e.g. Acme Corp"
                  onChange={e => setForm(f => ({ ...f, supplier: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">PO Reference</label>
                <input
                  type="text" value={form.purchaseOrderReference} placeholder="e.g. PO2026-00123"
                  onChange={e => setForm(f => ({ ...f, purchaseOrderReference: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Prepared By</label>
                <input
                  type="text" value={form.preparedBy}
                  onChange={e => setForm(f => ({ ...f, preparedBy: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Received By</label>
                <input
                  type="text" value={form.receivedBy}
                  onChange={e => setForm(f => ({ ...f, receivedBy: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div className="col-span-2 md:col-span-3">
                <label className="text-xs text-muted-foreground block mb-1">Notes</label>
                <textarea
                  rows={2} value={form.notes}
                  onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
                  className="input w-full resize-none"
                />
              </div>
            </div>

            {/* Items table */}
            <div>
              <div className="grid grid-cols-12 gap-2 text-xs font-medium text-muted-foreground pb-1 border-b">
                <span className="col-span-3">Item Description *</span>
                <span className="col-span-2">Product (opt.)</span>
                <span className="col-span-1">Qty</span>
                <span className="col-span-2">Serial No.</span>
                <span className="col-span-1">Unit</span>
                <span className="col-span-2">Notes</span>
                <span className="col-span-1"></span>
              </div>
              <div className="space-y-1.5 mt-2">
                {items.map((item, i) => (
                  <div key={i} className="grid grid-cols-12 gap-2 items-center">
                    <div className="col-span-3">
                      <input
                        required type="text" value={item.itemDescription} placeholder="Description"
                        onChange={e => setItem(i, "itemDescription", e.target.value)}
                        className="input w-full text-sm py-1.5"
                      />
                    </div>
                    <div className="col-span-2">
                      <select
                        value={item.productId ?? ""}
                        onChange={e => pickProduct(i, e.target.value)}
                        className="input w-full text-sm py-1.5"
                      >
                        <option value="">None</option>
                        {products?.filter((p: any) => p.isActive).map((p: any) => (
                          <option key={p.id} value={p.id}>{p.sku} — {p.name}</option>
                        ))}
                      </select>
                    </div>
                    <div className="col-span-1">
                      <input
                        type="number" min="0.01" step="0.01" value={item.quantity}
                        onChange={e => setItem(i, "quantity", e.target.value)}
                        className="input w-full text-sm py-1.5"
                      />
                    </div>
                    <div className="col-span-2">
                      <input
                        type="text" value={item.serialNumber ?? ""} placeholder="SN-…"
                        onChange={e => setItem(i, "serialNumber", e.target.value)}
                        className="input w-full text-sm py-1.5"
                      />
                    </div>
                    <div className="col-span-1">
                      <input
                        type="text" value={item.unit ?? ""} placeholder="pcs"
                        onChange={e => setItem(i, "unit", e.target.value)}
                        className="input w-full text-sm py-1.5"
                      />
                    </div>
                    <div className="col-span-2">
                      <input
                        type="text" value={item.notes ?? ""}
                        onChange={e => setItem(i, "notes", e.target.value)}
                        className="input w-full text-sm py-1.5"
                      />
                    </div>
                    <div className="col-span-1 flex justify-end">
                      <button
                        type="button"
                        onClick={() => setItems(p => p.length > 1 ? p.filter((_, idx) => idx !== i) : p)}
                        disabled={items.length <= 1}
                        className="p-1 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 disabled:opacity-20"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
              <button
                type="button"
                onClick={() => setItems(l => [...l, { ...EMPTY_ITEM(), sortOrder: l.length + 1 }])}
                className="mt-2 text-xs flex items-center gap-1 text-primary hover:underline"
              >
                <Plus className="h-3.5 w-3.5" /> Add Item
              </button>
            </div>

            <div className="flex gap-2">
              <button
                type="submit" disabled={isPending}
                className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2"
              >
                <Check className="h-4 w-4" />
                {isPending ? "Saving…" : editId ? "Update Receipt" : "Create Receipt"}
              </button>
              <button type="button" onClick={resetForm} className="btn-ghost px-4 py-2 rounded-lg text-sm">
                Cancel
              </button>
            </div>
          </form>
        )}

        {/* ── Delete confirm ── */}
        {deleteConfirm && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
            <div className="card p-6 max-w-sm w-full space-y-4 shadow-xl">
              <div className="flex items-center gap-3 text-red-600">
                <AlertTriangle className="h-5 w-5" />
                <h3 className="font-semibold">Delete Goods Receipt?</h3>
              </div>
              <p className="text-sm text-muted-foreground">This action cannot be undone.</p>
              <div className="flex gap-2 justify-end">
                <button onClick={() => setDeleteConfirm(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
                <button
                  onClick={() => handleDelete(deleteConfirm)}
                  disabled={deleteMut.isPending}
                  className="px-4 py-2 rounded-lg text-sm bg-red-600 text-white hover:bg-red-700 disabled:opacity-50"
                >
                  {deleteMut.isPending ? "Deleting…" : "Delete"}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* ── Table ── */}
        {isLoading ? (
          <div className="text-center py-10 text-muted-foreground">Loading goods receipts…</div>
        ) : (
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30">
                <tr>
                  <th className="text-left p-3 font-medium text-muted-foreground">Receipt #</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Supplier</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Warehouse</th>
                  <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Prepared By</th>
                  <th className="p-3 text-right font-medium text-muted-foreground">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {receipts?.map(r => {
                  const st = STATUS[r.status] ?? { label: "?", cls: "bg-muted" };
                  return (
                    <tr key={r.id} className="hover:bg-muted/20 transition-colors">
                      <td className="p-3 font-mono text-xs">{r.receiptNumber}</td>
                      <td className="p-3 text-muted-foreground">
                        {r.receiptDate ? new Date(r.receiptDate).toLocaleDateString("en-GB") : "—"}
                      </td>
                      <td className="p-3">{r.supplier ?? "—"}</td>
                      <td className="p-3">{r.warehouseName}</td>
                      <td className="p-3 text-center">
                        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${st.cls}`}>{st.label}</span>
                      </td>
                      <td className="p-3 text-muted-foreground">{r.preparedBy ?? "—"}</td>
                      <td className="p-3">
                        <div className="flex items-center gap-1 justify-end">
                          <button
                            onClick={() => openPrint(r)}
                            title="Print"
                            className="p-1.5 rounded hover:bg-muted text-muted-foreground hover:text-foreground"
                          >
                            <Printer className="h-4 w-4" />
                          </button>
                          {r.status === 1 && (
                            <>
                              <button
                                onClick={() => openEdit(r)}
                                title="Edit"
                                className="p-1.5 rounded hover:bg-muted text-muted-foreground hover:text-foreground"
                              >
                                <Edit2 className="h-4 w-4" />
                              </button>
                              <button
                                onClick={() => setDeleteConfirm(r.id)}
                                title="Delete"
                                className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600"
                              >
                                <Trash2 className="h-4 w-4" />
                              </button>
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
                {!receipts?.length && (
                  <tr>
                    <td colSpan={7} className="p-8 text-center text-muted-foreground">
                      No goods receipts found. Create your first receipt above.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  );
}
