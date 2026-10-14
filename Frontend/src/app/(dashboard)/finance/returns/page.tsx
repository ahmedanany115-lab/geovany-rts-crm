"use client";

import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useToast } from "@/components/ui/toast";
import { RotateCcw, RefreshCw, Plus, X, Check } from "lucide-react";

type ReturnLine = { id: string; productId: string; productName: string; quantity: number; unitPrice: number; discountPercent: number; taxRate: number };
type ReturnInvoice = {
  id: string; invoiceNumber: string; partyName: string; invoiceDate: string;
  currencyId: string; exchangeRate: number; warehouseId?: string | null; lines: ReturnLine[];
};
type Warehouse = { id: string; name: string; code: string };
type ReturnHistory = {
  id: string; entryDate: string; entryNumber: string; referenceNumber?: string;
  type: string; direction: "customer" | "supplier"; description: string; currencyCode: string; amount: number;
};

export default function ReturnsPage() {
  const { toast } = useToast();
  const qc = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [kind, setKind] = useState<"customer" | "supplier">("customer");
  const [invoiceId, setInvoiceId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [returnDate, setReturnDate] = useState(new Date().toISOString().split("T")[0]);
  const [notes, setNotes] = useState("");
  const [qty, setQty] = useState<Record<string, number>>({});

  const { data: history, isLoading, refetch } = useQuery({
    queryKey: ["returns"],
    queryFn: () => apiFetch<ReturnHistory[]>("/returns"),
  });
  const { data: customerInvoices } = useQuery({
    queryKey: ["return-customer-invoices"],
    queryFn: () => apiFetch<ReturnInvoice[]>("/returns/customer-invoices"),
  });
  const { data: supplierInvoices } = useQuery({
    queryKey: ["return-supplier-invoices"],
    queryFn: () => apiFetch<ReturnInvoice[]>("/returns/supplier-invoices"),
  });
  const { data: warehouses } = useQuery({
    queryKey: ["warehouses-return"],
    queryFn: () => apiFetch<Warehouse[]>("/warehouses?isActive=true"),
  });

  const invoices = kind === "customer" ? customerInvoices : supplierInvoices;
  const selected = useMemo(() => invoices?.find(i => i.id === invoiceId), [invoices, invoiceId]);

  const reset = () => {
    setInvoiceId(""); setWarehouseId(""); setNotes(""); setQty({});
    setReturnDate(new Date().toISOString().split("T")[0]);
  };

  const create = useMutation({
    mutationFn: () => apiFetch<{ returnNumber: string; amount: number }>("/returns/" + kind, {
      method: "POST",
      body: JSON.stringify({
        invoiceId,
        warehouseId,
        returnDate,
        notes: notes || undefined,
        lines: Object.entries(qty)
          .filter(([, q]) => Number(q) > 0)
          .map(([invoiceLineId, q]) => ({ invoiceLineId, quantity: Number(q) })),
      }),
    }),
    onSuccess: (r) => {
      qc.invalidateQueries({ queryKey: ["returns"] });
      qc.invalidateQueries({ queryKey: ["erp-dashboard"] });
      qc.invalidateQueries({ queryKey: ["inventory"] });
      toast(r.returnNumber + " posted successfully. Stock and accounting were updated.", "success");
      setShowForm(false); reset();
    },
    onError: (e: any) => toast(e?.message ?? "Return failed.", "error"),
  });

  const chooseInvoice = (id: string) => {
    setInvoiceId(id); setQty({});
    const inv = invoices?.find(i => i.id === id);
    setWarehouseId(inv?.warehouseId ?? "");
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <RotateCcw className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">Returns</h1>
            <p className="text-sm text-muted-foreground">Customer and supplier product returns with automatic stock and accounting entries.</p>
          </div>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => { setShowForm(v => !v); if (showForm) reset(); }} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showForm ? "Cancel" : "New Return"}
          </button>
        </div>
      </div>

      {showForm && (
        <div className="card p-5 space-y-4 border border-primary/20">
          <div className="grid md:grid-cols-4 gap-3">
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Return Type</label>
              <select value={kind} onChange={e => { setKind(e.target.value as any); reset(); }} className="input w-full">
                <option value="customer">Customer Return</option>
                <option value="supplier">Supplier Return</option>
              </select>
            </div>
            <div className="md:col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">Original Invoice</label>
              <select value={invoiceId} onChange={e => chooseInvoice(e.target.value)} className="input w-full">
                <option value="">Select invoice…</option>
                {invoices?.map(i => <option key={i.id} value={i.id}>{i.invoiceNumber} — {i.partyName} — {i.invoiceDate}</option>)}
              </select>
            </div>
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Return Date</label>
              <input type="date" value={returnDate} onChange={e => setReturnDate(e.target.value)} className="input w-full" />
            </div>
            <div className="md:col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">Warehouse</label>
              <select value={warehouseId} onChange={e => setWarehouseId(e.target.value)} className="input w-full">
                <option value="">Select warehouse…</option>
                {warehouses?.map(w => <option key={w.id} value={w.id}>{w.code} — {w.name}</option>)}
              </select>
            </div>
            <div className="md:col-span-2">
              <label className="text-xs text-muted-foreground block mb-1">Notes</label>
              <input value={notes} onChange={e => setNotes(e.target.value)} className="input w-full" placeholder="Reason / reference (optional)" />
            </div>
          </div>

          {selected && (
            <div className="rounded-lg border overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-muted/30"><tr>
                  <th className="text-left p-3">Product</th>
                  <th className="text-right p-3">Invoiced Qty</th>
                  <th className="text-right p-3">Unit Price</th>
                  <th className="text-right p-3 w-44">Return Qty</th>
                </tr></thead>
                <tbody className="divide-y">
                  {selected.lines.map(line => (
                    <tr key={line.id}>
                      <td className="p-3 font-medium">{line.productName}</td>
                      <td className="p-3 text-right tabular-nums">{line.quantity}</td>
                      <td className="p-3 text-right tabular-nums">{line.unitPrice.toLocaleString()}</td>
                      <td className="p-3">
                        <input
                          type="number" min={0} max={line.quantity} step="0.01"
                          value={qty[line.id] ?? ""}
                          onChange={e => setQty(q => ({ ...q, [line.id]: Number(e.target.value) }))}
                          className="input w-full text-right"
                          placeholder="0"
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <button
            onClick={() => create.mutate()}
            disabled={create.isPending || !invoiceId || !warehouseId || !Object.values(qty).some(v => Number(v) > 0)}
            className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center gap-2"
          >
            <Check className="h-4 w-4" />
            {create.isPending ? "Posting…" : "Post Return"}
          </button>
        </div>
      )}

      <div className="card overflow-hidden">
        <div className="px-5 py-3 border-b">
          <h2 className="font-semibold">Return History</h2>
        </div>
        {isLoading ? <div className="p-8 text-center text-muted-foreground">Loading…</div> : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/30"><tr>
                <th className="text-left p-3">Return #</th>
                <th className="text-left p-3">Date</th>
                <th className="text-left p-3">Type</th>
                <th className="text-left p-3">Description</th>
                <th className="text-right p-3">Amount</th>
              </tr></thead>
              <tbody className="divide-y">
                {history?.map(r => (
                  <tr key={r.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{r.referenceNumber ?? r.entryNumber}</td>
                    <td className="p-3">{r.entryDate}</td>
                    <td className="p-3"><span className="text-xs px-2 py-1 rounded-full bg-muted">{r.type}</span></td>
                    <td className="p-3 text-muted-foreground">{r.description}</td>
                    <td className="p-3 text-right font-semibold tabular-nums">{r.amount.toLocaleString()} {r.currencyCode}</td>
                  </tr>
                ))}
                {!history?.length && <tr><td colSpan={5} className="p-8 text-center text-muted-foreground">No returns recorded yet.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
