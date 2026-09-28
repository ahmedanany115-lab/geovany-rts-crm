"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useProducts } from "@/features/erp/hooks";
import { useToast } from "@/components/ui/toast";
import { usePrint } from "@/hooks/usePrint";
import {
  ShieldCheck, RefreshCw, Plus, X, Trash2, Edit2, Printer,
  Check, AlertTriangle,
} from "lucide-react";

// ── Types ─────────────────────────────────────────────────────────────────────

interface WarrantyCertificate {
  id: string;
  certificateNumber: string;
  certificateDate: string;
  customer?: string;
  customerContact?: string;
  product?: string;
  productId?: string;
  productName?: string;
  serialNumber?: string;
  quantity: number;
  invoiceReference?: string;
  salesOrderReference?: string;
  warrantyStartDate: string;
  warrantyEndDate: string;
  warrantyTerms?: string;
  notes?: string;
  status: number;
  createdAt: string;
}

// ── Status config ─────────────────────────────────────────────────────────────

const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Active",  cls: "bg-emerald-100 text-emerald-700" },
  2: { label: "Expired", cls: "bg-yellow-100 text-yellow-700" },
  3: { label: "Voided",  cls: "bg-red-100 text-red-700" },
};

// ── Empty helpers ─────────────────────────────────────────────────────────────

const today = () => new Date().toISOString().split("T")[0];
const oneYear = () => {
  const d = new Date(); d.setFullYear(d.getFullYear() + 1);
  return d.toISOString().split("T")[0];
};

const EMPTY_FORM = () => ({
  certificateDate: today(),
  customer: "",
  customerContact: "",
  product: "",
  productId: "",
  serialNumber: "",
  quantity: 1,
  invoiceReference: "",
  salesOrderReference: "",
  warrantyStartDate: today(),
  warrantyEndDate: oneYear(),
  warrantyTerms: "",
  notes: "",
});

// ── Print view ────────────────────────────────────────────────────────────────

function PrintView({ cert }: { cert: WarrantyCertificate }) {
  return (
    <div className="p-10 font-sans text-sm">
      <div className="text-center mb-8">
        <h1 className="text-3xl font-bold tracking-wide mb-1">WARRANTY CERTIFICATE</h1>
        <div className="h-1 w-32 bg-black mx-auto" />
      </div>
      <div className="grid grid-cols-2 gap-x-8 gap-y-1 mb-6">
        <div><span className="font-semibold">Certificate No:</span> {cert.certificateNumber}</div>
        <div><span className="font-semibold">Date:</span> {new Date(cert.certificateDate).toLocaleDateString("en-GB")}</div>
      </div>

      <div className="border-t pt-4 mb-4">
        <h2 className="font-bold mb-2">Customer</h2>
        <div><span className="font-semibold">Name:</span> {cert.customer ?? "—"}</div>
        <div><span className="font-semibold">Contact:</span> {cert.customerContact ?? "—"}</div>
      </div>

      <div className="border-t pt-4 mb-4">
        <h2 className="font-bold mb-2">Product</h2>
        <div><span className="font-semibold">Product:</span> {cert.productName ?? cert.product ?? "—"}</div>
        <div><span className="font-semibold">Serial Number:</span> {cert.serialNumber ?? "—"}</div>
        <div><span className="font-semibold">Quantity:</span> {cert.quantity}</div>
      </div>

      <div className="border-t pt-4 mb-4">
        <h2 className="font-bold mb-2">Warranty Period</h2>
        <div><span className="font-semibold">From:</span> {new Date(cert.warrantyStartDate).toLocaleDateString("en-GB")}</div>
        <div><span className="font-semibold">To:</span> {new Date(cert.warrantyEndDate).toLocaleDateString("en-GB")}</div>
      </div>

      {cert.warrantyTerms && (
        <div className="border-t pt-4 mb-4">
          <h2 className="font-bold mb-2">Warranty Terms</h2>
          <p className="whitespace-pre-wrap">{cert.warrantyTerms}</p>
        </div>
      )}

      {cert.notes && (
        <div className="border-t pt-4 mb-4">
          <h2 className="font-bold mb-2">Notes</h2>
          <p className="whitespace-pre-wrap">{cert.notes}</p>
        </div>
      )}

      <div className="border-t pt-4 text-xs text-muted-foreground space-y-0.5">
        {cert.invoiceReference && <div><span className="font-semibold">Reference Invoice:</span> {cert.invoiceReference}</div>}
        {cert.salesOrderReference && <div><span className="font-semibold">Sales Order:</span> {cert.salesOrderReference}</div>}
      </div>
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export default function WarrantyCertificatesPage() {
  const { toast } = useToast();
  const qc = useQueryClient();
  const { printRef, handlePrint } = usePrint("Warranty Certificate");

  // ── Data ─────────────────────────────────────────────────────────────────────
  const { data: certs, isLoading, refetch } = useQuery({
    queryKey: ["warranty-certificates"],
    queryFn: () => apiFetch<WarrantyCertificate[]>("/warrantycertificates"),
  });
  const { data: products } = useProducts({});

  // ── Mutations ─────────────────────────────────────────────────────────────────
  const createMut = useMutation({
    mutationFn: (data: unknown) =>
      apiFetch<WarrantyCertificate>("/warrantycertificates", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["warranty-certificates"] }); toast("Certificate created.", "success"); },
    onError: (err: any) => toast(err?.message ?? "Failed to create.", "error"),
  });
  const updateMut = useMutation({
    mutationFn: ({ id, data }: { id: string; data: unknown }) =>
      apiFetch<WarrantyCertificate>(`/warrantycertificates/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["warranty-certificates"] }); toast("Certificate updated.", "success"); },
    onError: (err: any) => toast(err?.message ?? "Failed to update.", "error"),
  });
  const deleteMut = useMutation({
    mutationFn: (id: string) => apiFetch<void>(`/warrantycertificates/${id}`, { method: "DELETE" }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["warranty-certificates"] }); toast("Certificate deleted.", "success"); },
    onError: (err: any) => toast(err?.message ?? "Failed to delete.", "error"),
  });

  // ── UI state ──────────────────────────────────────────────────────────────────
  const [showForm, setShowForm]       = useState(false);
  const [editId, setEditId]           = useState<string | null>(null);
  const [form, setForm]               = useState(EMPTY_FORM());
  const [formError, setFormError]     = useState<string | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);
  const [viewCert, setViewCert]       = useState<WarrantyCertificate | null>(null);

  // ── Form helpers ──────────────────────────────────────────────────────────────
  const resetForm = () => {
    setForm(EMPTY_FORM()); setFormError(null); setShowForm(false); setEditId(null);
  };

  const openCreate = () => { resetForm(); setShowForm(true); };

  const openEdit = (c: WarrantyCertificate) => {
    setEditId(c.id);
    setForm({
      certificateDate: c.certificateDate?.split("T")[0] ?? today(),
      customer: c.customer ?? "",
      customerContact: c.customerContact ?? "",
      product: c.product ?? "",
      productId: c.productId ?? "",
      serialNumber: c.serialNumber ?? "",
      quantity: c.quantity ?? 1,
      invoiceReference: c.invoiceReference ?? "",
      salesOrderReference: c.salesOrderReference ?? "",
      warrantyStartDate: c.warrantyStartDate?.split("T")[0] ?? today(),
      warrantyEndDate: c.warrantyEndDate?.split("T")[0] ?? oneYear(),
      warrantyTerms: c.warrantyTerms ?? "",
      notes: c.notes ?? "",
    });
    setFormError(null);
    setShowForm(true);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const pickProduct = (pid: string) => {
    const p = products?.find((x: any) => x.id === pid);
    setForm(f => ({
      ...f,
      productId: pid,
      product: f.product || ((p as any)?.name ?? ""),
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setFormError(null);
    if (!form.warrantyStartDate) { setFormError("Warranty start date is required."); return; }
    if (!form.warrantyEndDate)   { setFormError("Warranty end date is required."); return; }
    if (new Date(form.warrantyEndDate) <= new Date(form.warrantyStartDate)) {
      setFormError("Warranty end date must be after start date."); return;
    }
    const payload = {
      ...form,
      quantity: Number(form.quantity),
      productId: form.productId || undefined,
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

  const openPrint = (c: WarrantyCertificate) => {
    setViewCert(c);
    setTimeout(() => handlePrint(), 100);
  };

  const isPending = createMut.isPending || updateMut.isPending;

  // ── Warranty period helper ────────────────────────────────────────────────────
  const warrantyPeriod = (c: WarrantyCertificate) => {
    if (!c.warrantyStartDate || !c.warrantyEndDate) return "—";
    const start = new Date(c.warrantyStartDate);
    const end   = new Date(c.warrantyEndDate);
    const months = Math.round((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24 * 30.5));
    return months >= 12
      ? `${Math.round(months / 12)}y`
      : `${months}m`;
  };

  return (
    <>
      {/* Hidden print area */}
      <div className="hidden print:block" ref={printRef}>
        {viewCert && <PrintView cert={viewCert} />}
      </div>

      <div className="p-6 space-y-6 print:hidden">
        {/* ── Header ── */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <ShieldCheck className="h-6 w-6 text-primary" />
            <h1 className="text-2xl font-semibold">Warranty Certificates</h1>
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
              {showForm ? "Cancel" : "New Certificate"}
            </button>
          </div>
        </div>

        {/* ── Create / Edit Form ── */}
        {showForm && (
          <form onSubmit={handleSubmit} className="card p-5 space-y-4 border border-primary/20">
            <h2 className="font-semibold text-sm">{editId ? "Edit Certificate" : "New Warranty Certificate"}</h2>
            {formError && (
              <div className="rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700 flex items-center gap-2">
                <AlertTriangle className="h-4 w-4 shrink-0" /> {formError}
              </div>
            )}

            <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Certificate Date *</label>
                <input
                  type="date" required value={form.certificateDate}
                  onChange={e => setForm(f => ({ ...f, certificateDate: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Customer</label>
                <input
                  type="text" value={form.customer} placeholder="Customer name"
                  onChange={e => setForm(f => ({ ...f, customer: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Customer Contact</label>
                <input
                  type="text" value={form.customerContact} placeholder="Phone / email"
                  onChange={e => setForm(f => ({ ...f, customerContact: e.target.value }))}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="text-xs text-muted-foreground block mb-1">Product (linked)</label>
                <select
                  value={form.productId}
                  onChange={e => pickProduct(e.target.value)}
                  className="input w-full"
                >
                  <option value="">None</option>
                  {products?.filter((p: any) => p.isActive).map((p: any) => (
                    <option key={p.id} value={p.id}>{p.sku} — {p.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Product Name</label>
                <input
                  type="text" value={form.product} placeholder="Or type product name"
                  onChange={e => setForm(f => ({ ...f, product: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Serial Number</label>
                <input
                  type="text" value={form.serialNumber} placeholder="SN-…"
                  onChange={e => setForm(f => ({ ...f, serialNumber: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Quantity</label>
                <input
                  type="number" min="1" step="1" value={form.quantity}
                  onChange={e => setForm(f => ({ ...f, quantity: Number(e.target.value) }))}
                  className="input w-full"
                />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Invoice Reference</label>
                <input
                  type="text" value={form.invoiceReference}
                  onChange={e => setForm(f => ({ ...f, invoiceReference: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Sales Order Reference</label>
                <input
                  type="text" value={form.salesOrderReference}
                  onChange={e => setForm(f => ({ ...f, salesOrderReference: e.target.value }))}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="text-xs text-muted-foreground block mb-1">Warranty Start *</label>
                <input
                  type="date" required value={form.warrantyStartDate}
                  onChange={e => setForm(f => ({ ...f, warrantyStartDate: e.target.value }))}
                  className="input w-full"
                />
              </div>
              <div>
                <label className="text-xs text-muted-foreground block mb-1">Warranty End *</label>
                <input
                  type="date" required value={form.warrantyEndDate}
                  onChange={e => setForm(f => ({ ...f, warrantyEndDate: e.target.value }))}
                  className="input w-full"
                />
              </div>

              <div className="col-span-2 md:col-span-3">
                <label className="text-xs text-muted-foreground block mb-1">Warranty Terms</label>
                <textarea
                  rows={4} value={form.warrantyTerms}
                  placeholder="Describe the warranty terms and conditions…"
                  onChange={e => setForm(f => ({ ...f, warrantyTerms: e.target.value }))}
                  className="input w-full resize-none"
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

            <div className="flex gap-2">
              <button
                type="submit" disabled={isPending}
                className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2"
              >
                <Check className="h-4 w-4" />
                {isPending ? "Saving…" : editId ? "Update Certificate" : "Create Certificate"}
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
                <h3 className="font-semibold">Delete Certificate?</h3>
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
          <div className="text-center py-10 text-muted-foreground">Loading certificates…</div>
        ) : (
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30">
                <tr>
                  <th className="text-left p-3 font-medium text-muted-foreground">Certificate #</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Product</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Serial No.</th>
                  <th className="text-center p-3 font-medium text-muted-foreground">Period</th>
                  <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
                  <th className="p-3 text-right font-medium text-muted-foreground">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {certs?.map(c => {
                  const st = STATUS[c.status] ?? { label: "?", cls: "bg-muted" };
                  return (
                    <tr key={c.id} className="hover:bg-muted/20 transition-colors">
                      <td className="p-3 font-mono text-xs">{c.certificateNumber}</td>
                      <td className="p-3 text-muted-foreground">
                        {c.certificateDate ? new Date(c.certificateDate).toLocaleDateString("en-GB") : "—"}
                      </td>
                      <td className="p-3">{c.customer ?? "—"}</td>
                      <td className="p-3">{c.productName ?? c.product ?? "—"}</td>
                      <td className="p-3 font-mono text-xs">{c.serialNumber ?? "—"}</td>
                      <td className="p-3 text-center text-muted-foreground text-xs">{warrantyPeriod(c)}</td>
                      <td className="p-3 text-center">
                        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${st.cls}`}>{st.label}</span>
                      </td>
                      <td className="p-3">
                        <div className="flex items-center gap-1 justify-end">
                          <button
                            onClick={() => openPrint(c)}
                            title="Print"
                            className="p-1.5 rounded hover:bg-muted text-muted-foreground hover:text-foreground"
                          >
                            <Printer className="h-4 w-4" />
                          </button>
                          <button
                            onClick={() => openEdit(c)}
                            title="Edit"
                            className="p-1.5 rounded hover:bg-muted text-muted-foreground hover:text-foreground"
                          >
                            <Edit2 className="h-4 w-4" />
                          </button>
                          {c.status === 1 && (
                            <button
                              onClick={() => setDeleteConfirm(c.id)}
                              title="Delete"
                              className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600"
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
                {!certs?.length && (
                  <tr>
                    <td colSpan={8} className="p-8 text-center text-muted-foreground">
                      No warranty certificates found. Create your first certificate above.
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
