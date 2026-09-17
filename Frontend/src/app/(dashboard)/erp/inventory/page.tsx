"use client";
import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { useWarehouses } from "@/features/erp/hooks";
import { useProducts } from "@/features/erp/hooks";
import { ArrowLeftRight, RefreshCw, Plus, X, Check, AlertTriangle, Warehouse } from "lucide-react";

interface InventoryBalance {
  id: string; productId: string; productName: string; productSku: string;
  warehouseId: string; warehouseName: string; quantity: number;
  reservedQuantity: number; availableQuantity: number;
  averageCost: number; totalValue: number;
}

const EGP = (v: number) => v.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 });

export default function InventoryPage() {
  const { t } = useT();
  const { toast } = useToast();
  const qc = useQueryClient();
  const [warehouseFilter, setWarehouseFilter] = useState("");
  const [showTransfer, setShowTransfer] = useState(false);
  const [xfer, setXfer] = useState({
    productId: "", fromWarehouseId: "", toWarehouseId: "",
    quantity: "", transferDate: new Date().toISOString().split("T")[0], notes: "",
  });
  const [xferError, setXferError] = useState<string | null>(null);

  const { data: balances, isLoading, refetch } = useQuery({
    queryKey: ["inventory-balances", warehouseFilter],
    queryFn: () => apiFetch<InventoryBalance[]>(
      `/inventory/balances${warehouseFilter ? `?warehouseId=${warehouseFilter}` : ""}`
    ),
  });
  const { data: warehouses } = useWarehouses();
  const { data: products }   = useProducts({});

  const transferMut = useMutation({
    mutationFn: (data: any) => apiFetch<void>("/inventory/transfer", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["inventory-balances"] });
      toast("Stock transfer completed. Inventory balances updated.", "success");
      setShowTransfer(false);
      setXfer({ productId: "", fromWarehouseId: "", toWarehouseId: "", quantity: "", transferDate: new Date().toISOString().split("T")[0], notes: "" });
    },
    onError: (e: any) => { const msg = e?.message ?? "Transfer failed."; setXferError(msg); toast(msg, "error"); },
  });

  const handleTransfer = (e: React.FormEvent) => {
    e.preventDefault(); setXferError(null);
    if (!xfer.productId)       { setXferError("Select a product."); return; }
    if (!xfer.fromWarehouseId) { setXferError("Select source warehouse."); return; }
    if (!xfer.toWarehouseId)   { setXferError("Select destination warehouse."); return; }
    if (xfer.fromWarehouseId === xfer.toWarehouseId) { setXferError("Source and destination must differ."); return; }
    if (!xfer.quantity || Number(xfer.quantity) <= 0) { setXferError("Quantity must be > 0."); return; }

    // Find available qty in source
    const srcBalance = balances?.find(b => b.productId === xfer.productId && b.warehouseId === xfer.fromWarehouseId);
    if (srcBalance && Number(xfer.quantity) > srcBalance.availableQuantity) {
      setXferError(`Only ${srcBalance.availableQuantity} units available in source warehouse.`);
      return;
    }
    transferMut.mutate({ ...xfer, quantity: Number(xfer.quantity) });
  };

  const totalValue = balances?.reduce((s, b) => s + b.totalValue, 0) ?? 0;

  // Group by warehouse for summary
  const wh1 = balances?.filter(b => b.warehouseName?.includes("1") || b.warehouseName?.toLowerCase().includes("warehouse 1") || b.warehouseId === "11111111-0000-0000-0000-000000000001");
  const wh2 = balances?.filter(b => b.warehouseName?.includes("2") || b.warehouseName?.toLowerCase().includes("warehouse 2") || b.warehouseId === "22222222-0000-0000-0000-000000000002");

  return (
    <div className="p-6 space-y-6">
      {/* Transfer modal */}
      {showTransfer && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl p-6 max-w-md w-full space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="font-semibold flex items-center gap-2"><ArrowLeftRight className="h-5 w-5 text-primary" />Stock Transfer</h2>
              <button onClick={() => setShowTransfer(false)} className="p-1.5 rounded hover:bg-accent"><X className="h-4 w-4" /></button>
            </div>
            {xferError && <div className="flex items-center gap-2 rounded-md bg-red-50 border border-red-200 px-4 py-2 text-sm text-red-700"><AlertTriangle className="h-4 w-4 shrink-0" />{xferError}</div>}
            <form onSubmit={handleTransfer} className="space-y-3">
              <div><label className="text-xs text-muted-foreground block mb-1">Product *</label>
                <select required value={xfer.productId} onChange={e => setXfer(f => ({ ...f, productId: e.target.value }))} className="input w-full">
                  <option value="">Select product…</option>
                  {products?.filter((p: any) => p.isActive).map((p: any) => <option key={p.id} value={p.id}>{p.sku} — {p.name}</option>)}
                </select>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div><label className="text-xs text-muted-foreground block mb-1">From Warehouse *</label>
                  <select required value={xfer.fromWarehouseId} onChange={e => setXfer(f => ({ ...f, fromWarehouseId: e.target.value }))} className="input w-full">
                    <option value="">Select…</option>
                    {warehouses?.filter((w: any) => w.isActive).map((w: any) => <option key={w.id} value={w.id}>{w.name}</option>)}
                  </select>
                </div>
                <div><label className="text-xs text-muted-foreground block mb-1">To Warehouse *</label>
                  <select required value={xfer.toWarehouseId} onChange={e => setXfer(f => ({ ...f, toWarehouseId: e.target.value }))} className="input w-full">
                    <option value="">Select…</option>
                    {warehouses?.filter((w: any) => w.isActive && w.id !== xfer.fromWarehouseId).map((w: any) => <option key={w.id} value={w.id}>{w.name}</option>)}
                  </select>
                </div>
              </div>
              {xfer.productId && xfer.fromWarehouseId && (
                <div className="text-xs text-muted-foreground bg-muted/30 px-3 py-2 rounded">
                  Available in source: <strong>
                    {balances?.find(b => b.productId === xfer.productId && b.warehouseId === xfer.fromWarehouseId)?.availableQuantity ?? 0}
                  </strong> units
                </div>
              )}
              <div className="grid grid-cols-2 gap-3">
                <div><label className="text-xs text-muted-foreground block mb-1">Quantity *</label>
                  <input required type="number" min="0.01" step="0.01" value={xfer.quantity}
                    onChange={e => setXfer(f => ({ ...f, quantity: e.target.value }))} className="input w-full" />
                </div>
                <div><label className="text-xs text-muted-foreground block mb-1">Transfer Date *</label>
                  <input type="date" value={xfer.transferDate} onChange={e => setXfer(f => ({ ...f, transferDate: e.target.value }))} className="input w-full" />
                </div>
              </div>
              <div><label className="text-xs text-muted-foreground block mb-1">Notes</label>
                <input value={xfer.notes} onChange={e => setXfer(f => ({ ...f, notes: e.target.value }))} className="input w-full" placeholder="Reference, reason…" />
              </div>
              <div className="flex gap-2 pt-1">
                <button type="button" onClick={() => setShowTransfer(false)} className="btn-ghost flex-1 py-2 rounded-lg text-sm">Cancel</button>
                <button type="submit" disabled={transferMut.isPending}
                  className="btn-primary flex-1 py-2 rounded-lg text-sm disabled:opacity-50 flex items-center justify-center gap-2">
                  <Check className="h-4 w-4" />{transferMut.isPending ? "Transferring…" : "Transfer"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex items-center gap-3">
          <Warehouse className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">Inventory</h1>
            <p className="text-sm text-muted-foreground">Total Value: <strong>{EGP(totalValue)}</strong></p>
          </div>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowTransfer(true)}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            <ArrowLeftRight className="h-4 w-4" /> Transfer Stock
          </button>
        </div>
      </div>

      {/* Warehouse summary cards */}
      {warehouses && warehouses.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {warehouses.map((w: any) => {
            const wBalances = balances?.filter(b => b.warehouseId === w.id) ?? [];
            const wValue = wBalances.reduce((s, b) => s + b.totalValue, 0);
            const wItems = wBalances.reduce((s, b) => s + b.quantity, 0);
            return (
              <button key={w.id} onClick={() => setWarehouseFilter(v => v === w.id ? "" : w.id)}
                className={`card p-4 text-left hover:border-primary/40 transition-colors ${warehouseFilter === w.id ? "border-primary bg-primary/5" : ""}`}>
                <p className="font-semibold text-sm">{w.name}</p>
                <p className="text-xs text-muted-foreground mt-0.5">{w.location ?? "—"}</p>
                <p className="text-lg font-bold tabular-nums mt-2">{wItems.toLocaleString()}</p>
                <p className="text-xs text-muted-foreground">units · {EGP(wValue)}</p>
              </button>
            );
          })}
          {warehouseFilter && (
            <button onClick={() => setWarehouseFilter("")}
              className="card p-4 text-left hover:border-primary/40 transition-colors flex items-center justify-center gap-2 text-sm text-muted-foreground">
              <X className="h-4 w-4" /> Show All
            </button>
          )}
        </div>
      )}

      {/* Warehouse filter badge */}
      {warehouseFilter && (
        <p className="text-sm text-muted-foreground">
          Showing: <strong>{warehouses?.find((w: any) => w.id === warehouseFilter)?.name}</strong>
        </p>
      )}

      {/* Filter */}
      <div className="flex gap-3">
        <select value={warehouseFilter} onChange={e => setWarehouseFilter(e.target.value)} className="input text-sm">
          <option value="">All Warehouses</option>
          {warehouses?.map((w: any) => <option key={w.id} value={w.id}>{w.name}</option>)}
        </select>
      </div>

      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">SKU</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Product</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Warehouse</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Qty</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Reserved</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Available</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Value</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {balances?.map(b => (
                <tr key={b.id} className="hover:bg-muted/20">
                  <td className="p-3 font-mono text-xs">{b.productSku}</td>
                  <td className="p-3 font-medium">{b.productName}</td>
                  <td className="p-3">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                      b.warehouseName?.includes("1") ? "bg-blue-100 text-blue-700" : "bg-purple-100 text-purple-700"
                    }`}>{b.warehouseName}</span>
                  </td>
                  <td className="p-3 text-right tabular-nums font-semibold">{b.quantity.toLocaleString()}</td>
                  <td className="p-3 text-right tabular-nums text-amber-600">{b.reservedQuantity > 0 ? b.reservedQuantity.toLocaleString() : "—"}</td>
                  <td className="p-3 text-right tabular-nums text-emerald-600">{b.availableQuantity.toLocaleString()}</td>
                  <td className="p-3 text-right tabular-nums">{EGP(b.totalValue)}</td>
                  <td className="p-3 text-right">
                    <button onClick={() => { setXfer(f => ({ ...f, productId: b.productId, fromWarehouseId: b.warehouseId })); setShowTransfer(true); }}
                      className="text-xs px-2 py-1 rounded hover:bg-accent text-muted-foreground hover:text-primary flex items-center gap-1">
                      <ArrowLeftRight className="h-3.5 w-3.5" /> Transfer
                    </button>
                  </td>
                </tr>
              ))}
              {!balances?.length && <tr><td colSpan={8} className="p-8 text-center text-muted-foreground">No inventory found. Add products and adjust stock to begin.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
