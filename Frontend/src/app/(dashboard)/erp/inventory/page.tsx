"use client";

import { useState } from "react";
import { Package, RefreshCw, ArrowUpDown, ArrowRight } from "lucide-react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useProducts, useWarehouses, useProductStock } from "@/features/erp/hooks";
import { InventoryMovementDto } from "@/features/erp/types";

const MOVEMENT_COLORS: Record<number, string> = {
  1: "text-blue-600",
  2: "text-emerald-600",
  3: "text-red-600",
  4: "text-orange-600",
  5: "text-teal-600",
  6: "text-emerald-600",
  7: "text-red-600",
  8: "text-emerald-600",
  9: "text-orange-600",
};

const MOVEMENT_LABELS: Record<number, string> = {
  1: "Opening Balance", 2: "Purchase Receipt", 3: "Sales Issue",
  4: "Transfer Out", 5: "Transfer In", 6: "Adjustment In",
  7: "Adjustment Out", 8: "Return from Customer", 9: "Return to Supplier",
};

export default function InventoryPage() {
  const [tab, setTab] = useState<"stock" | "movements" | "adjust" | "transfer">("stock");
  const [productFilter, setProductFilter] = useState("");
  const [warehouseFilter, setWarehouseFilter] = useState("");
  const qc = useQueryClient();

  const { data: products } = useProducts({ isActive: true });
  const { data: warehouses } = useWarehouses({ isActive: true });
  const { data: stock, isLoading: stockLoading, refetch: refetchStock } = useProductStock(
    warehouseFilter ? { warehouseId: warehouseFilter } : undefined
  );

  const { data: movements, isLoading: movLoading, refetch: refetchMov } = useQuery({
    queryKey: ["inventory-movements", productFilter, warehouseFilter],
    queryFn: () => {
      const params = new URLSearchParams();
      if (productFilter) params.set("productId", productFilter);
      if (warehouseFilter) params.set("warehouseId", warehouseFilter);
      return apiFetch<InventoryMovementDto[]>(`/inventory/movements?${params}`);
    },
    enabled: tab === "movements",
  });

  // Adjust form
  const today = new Date().toISOString().split("T")[0];
  const [adjForm, setAdjForm] = useState({ productId: "", warehouseId: "", quantity: "0", unitCost: "0", movementDate: today, notes: "" });
  const adjust = useMutation({
    mutationFn: (data: unknown) => apiFetch<{ id: string }>("/inventory/adjust", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["product-stock"] }); qc.invalidateQueries({ queryKey: ["inventory-movements"] }); setAdjForm({ productId: "", warehouseId: "", quantity: "0", unitCost: "0", movementDate: today, notes: "" }); },
  });

  // Transfer form
  const [trnForm, setTrnForm] = useState({ productId: "", fromWarehouseId: "", toWarehouseId: "", quantity: "0", transferDate: today, notes: "" });
  const transfer = useMutation({
    mutationFn: (data: unknown) => apiFetch<void>("/inventory/transfer", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["product-stock"] }); qc.invalidateQueries({ queryKey: ["inventory-movements"] }); setTrnForm({ productId: "", fromWarehouseId: "", toWarehouseId: "", quantity: "0", transferDate: today, notes: "" }); },
  });

  const totalInventoryValue = stock?.reduce((s, b) => s + b.quantity * b.averageCost, 0) ?? 0;

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Package className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">Inventory</h1>
        </div>
        <button onClick={() => { refetchStock(); refetchMov(); }} className="btn-ghost p-2 rounded-lg">
          <RefreshCw className="h-4 w-4" />
        </button>
      </div>

      {/* KPI */}
      <div className="grid grid-cols-3 gap-4">
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Total Inventory Value</p>
          <p className="text-2xl font-bold mt-1">
            {totalInventoryValue.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}
          </p>
        </div>
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Products in Stock</p>
          <p className="text-2xl font-bold mt-1">{stock?.filter(s => s.quantity > 0).length ?? 0}</p>
        </div>
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Zero / Low Stock</p>
          <p className="text-2xl font-bold mt-1 text-amber-600">
            {stock?.filter(s => s.availableQuantity <= 0).length ?? 0}
          </p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-border">
        {(["stock", "movements", "adjust", "transfer"] as const).map(t => (
          <button key={t} onClick={() => setTab(t)}
            className={`px-4 py-2 text-sm font-medium capitalize transition-colors border-b-2 -mb-px ${
              tab === t ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"
            }`}>
            {t === "adjust" ? "Adjustment" : t === "transfer" ? "Warehouse Transfer" : t.charAt(0).toUpperCase() + t.slice(1)}
          </button>
        ))}
      </div>

      {/* Filters */}
      {(tab === "stock" || tab === "movements") && (
        <div className="flex gap-3">
          <select value={warehouseFilter} onChange={e => setWarehouseFilter(e.target.value)} className="input w-48">
            <option value="">All Warehouses</option>
            {warehouses?.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
          </select>
          {tab === "movements" && (
            <select value={productFilter} onChange={e => setProductFilter(e.target.value)} className="input w-64">
              <option value="">All Products</option>
              {products?.map(p => <option key={p.id} value={p.id}>{p.sku} – {p.name}</option>)}
            </select>
          )}
        </div>
      )}

      {/* Stock Tab */}
      {tab === "stock" && (
        stockLoading ? <div className="text-center py-10 text-muted-foreground">Loading stock...</div> : (
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30">
                <tr>
                  <th className="text-left p-3 font-medium text-muted-foreground">Product</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Warehouse</th>
                  <th className="text-right p-3 font-medium text-muted-foreground">Qty on Hand</th>
                  <th className="text-right p-3 font-medium text-muted-foreground">Reserved</th>
                  <th className="text-right p-3 font-medium text-muted-foreground">Available</th>
                  <th className="text-right p-3 font-medium text-muted-foreground">Avg Cost</th>
                  <th className="text-right p-3 font-medium text-muted-foreground">Value</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {stock?.map(s => (
                  <tr key={`${s.productId}-${s.warehouseId}`} className={`hover:bg-muted/20 ${s.availableQuantity <= 0 ? "opacity-60" : ""}`}>
                    <td className="p-3 font-medium">{s.productName}</td>
                    <td className="p-3 text-muted-foreground">{s.warehouseName}</td>
                    <td className="p-3 text-right tabular-nums">{s.quantity.toLocaleString()}</td>
                    <td className="p-3 text-right tabular-nums text-muted-foreground">{s.reservedQuantity.toLocaleString()}</td>
                    <td className={`p-3 text-right tabular-nums font-medium ${s.availableQuantity <= 0 ? "text-red-600" : "text-emerald-600"}`}>
                      {s.availableQuantity.toLocaleString()}
                    </td>
                    <td className="p-3 text-right tabular-nums text-muted-foreground">{s.averageCost.toFixed(2)}</td>
                    <td className="p-3 text-right tabular-nums font-medium">
                      {(s.quantity * s.averageCost).toLocaleString("en-EG", { style: "currency", currency: "EGP" })}
                    </td>
                  </tr>
                ))}
                {!stock?.length && <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">No stock records</td></tr>}
              </tbody>
            </table>
          </div>
        )
      )}

      {/* Movements Tab */}
      {tab === "movements" && (
        movLoading ? <div className="text-center py-10 text-muted-foreground">Loading movements...</div> : (
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30">
                <tr>
                  <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Product</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Warehouse</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Type</th>
                  <th className="text-right p-3 font-medium text-muted-foreground">Quantity</th>
                  <th className="text-right p-3 font-medium text-muted-foreground">Unit Cost</th>
                  <th className="text-right p-3 font-medium text-muted-foreground">Total</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Reference</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {movements?.map(m => (
                  <tr key={m.id} className="hover:bg-muted/20">
                    <td className="p-3 text-muted-foreground">{m.movementDate}</td>
                    <td className="p-3 font-medium">{m.productName}</td>
                    <td className="p-3 text-muted-foreground">{m.warehouseName}</td>
                    <td className={`p-3 font-medium ${MOVEMENT_COLORS[m.movementType] || ""}`}>
                      {MOVEMENT_LABELS[m.movementType] || m.movementTypeName}
                    </td>
                    <td className={`p-3 text-right tabular-nums font-medium ${m.quantity > 0 ? "text-emerald-600" : "text-red-600"}`}>
                      {m.quantity > 0 ? "+" : ""}{m.quantity.toLocaleString()}
                    </td>
                    <td className="p-3 text-right tabular-nums text-muted-foreground">{m.unitCost.toFixed(2)}</td>
                    <td className="p-3 text-right tabular-nums">{m.totalCost.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</td>
                    <td className="p-3 text-muted-foreground text-xs font-mono">{m.referenceNumber ?? "—"}</td>
                  </tr>
                ))}
                {!movements?.length && <tr><td colSpan={8} className="p-8 text-center text-muted-foreground">No movements found</td></tr>}
              </tbody>
            </table>
          </div>
        )
      )}

      {/* Adjustment Tab */}
      {tab === "adjust" && (
        <div className="card p-5 max-w-lg space-y-4">
          <h2 className="font-semibold">Stock Adjustment</h2>
          <p className="text-sm text-muted-foreground">Use positive quantity for adjustment-in (increase stock), negative for adjustment-out.</p>
          <div className="space-y-3">
            <div>
              <label className="text-sm text-muted-foreground block mb-1">Product *</label>
              <select required value={adjForm.productId} onChange={e => setAdjForm(f => ({ ...f, productId: e.target.value }))} className="input w-full">
                <option value="">Select product...</option>
                {products?.map(p => <option key={p.id} value={p.id}>{p.sku} – {p.name}</option>)}
              </select>
            </div>
            <div>
              <label className="text-sm text-muted-foreground block mb-1">Warehouse *</label>
              <select required value={adjForm.warehouseId} onChange={e => setAdjForm(f => ({ ...f, warehouseId: e.target.value }))} className="input w-full">
                <option value="">Select warehouse...</option>
                {warehouses?.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
              </select>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-sm text-muted-foreground block mb-1">Quantity (± )</label>
                <input type="number" step="0.01" value={adjForm.quantity} onChange={e => setAdjForm(f => ({ ...f, quantity: e.target.value }))} className="input w-full" />
              </div>
              <div>
                <label className="text-sm text-muted-foreground block mb-1">Unit Cost</label>
                <input type="number" min="0" step="0.01" value={adjForm.unitCost} onChange={e => setAdjForm(f => ({ ...f, unitCost: e.target.value }))} className="input w-full" />
              </div>
            </div>
            <div>
              <label className="text-sm text-muted-foreground block mb-1">Date</label>
              <input type="date" value={adjForm.movementDate} onChange={e => setAdjForm(f => ({ ...f, movementDate: e.target.value }))} className="input w-full" />
            </div>
            <div>
              <label className="text-sm text-muted-foreground block mb-1">Notes</label>
              <input value={adjForm.notes} onChange={e => setAdjForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" placeholder="Reason for adjustment..." />
            </div>
            <button
              onClick={() => adjust.mutate({ ...adjForm, quantity: parseFloat(adjForm.quantity), unitCost: parseFloat(adjForm.unitCost) })}
              disabled={adjust.isPending || !adjForm.productId || !adjForm.warehouseId}
              className="btn-primary px-5 py-2 rounded-lg text-sm">
              {adjust.isPending ? "Saving..." : "Post Adjustment"}
            </button>
            {adjust.isSuccess && <p className="text-sm text-emerald-600">✓ Adjustment posted successfully</p>}
            {adjust.isError && <p className="text-sm text-red-600">Error posting adjustment</p>}
          </div>
        </div>
      )}

      {/* Transfer Tab */}
      {tab === "transfer" && (
        <div className="card p-5 max-w-lg space-y-4">
          <h2 className="font-semibold">Warehouse Transfer</h2>
          <p className="text-sm text-muted-foreground">Move stock between warehouses. Preserves average cost.</p>
          <div className="space-y-3">
            <div>
              <label className="text-sm text-muted-foreground block mb-1">Product *</label>
              <select required value={trnForm.productId} onChange={e => setTrnForm(f => ({ ...f, productId: e.target.value }))} className="input w-full">
                <option value="">Select product...</option>
                {products?.map(p => <option key={p.id} value={p.id}>{p.sku} – {p.name}</option>)}
              </select>
            </div>
            <div className="grid grid-cols-5 gap-2 items-end">
              <div className="col-span-2">
                <label className="text-sm text-muted-foreground block mb-1">From Warehouse *</label>
                <select required value={trnForm.fromWarehouseId} onChange={e => setTrnForm(f => ({ ...f, fromWarehouseId: e.target.value }))} className="input w-full">
                  <option value="">Select...</option>
                  {warehouses?.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
                </select>
              </div>
              <div className="col-span-1 flex justify-center pb-2">
                <ArrowRight className="h-5 w-5 text-muted-foreground" />
              </div>
              <div className="col-span-2">
                <label className="text-sm text-muted-foreground block mb-1">To Warehouse *</label>
                <select required value={trnForm.toWarehouseId} onChange={e => setTrnForm(f => ({ ...f, toWarehouseId: e.target.value }))} className="input w-full">
                  <option value="">Select...</option>
                  {warehouses?.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
                </select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-sm text-muted-foreground block mb-1">Quantity *</label>
                <input type="number" min="0.01" step="0.01" value={trnForm.quantity} onChange={e => setTrnForm(f => ({ ...f, quantity: e.target.value }))} className="input w-full" />
              </div>
              <div>
                <label className="text-sm text-muted-foreground block mb-1">Transfer Date</label>
                <input type="date" value={trnForm.transferDate} onChange={e => setTrnForm(f => ({ ...f, transferDate: e.target.value }))} className="input w-full" />
              </div>
            </div>
            <div>
              <label className="text-sm text-muted-foreground block mb-1">Notes</label>
              <input value={trnForm.notes} onChange={e => setTrnForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" />
            </div>
            <button
              onClick={() => transfer.mutate({ ...trnForm, quantity: parseFloat(trnForm.quantity) })}
              disabled={transfer.isPending || !trnForm.productId || !trnForm.fromWarehouseId || !trnForm.toWarehouseId}
              className="btn-primary px-5 py-2 rounded-lg text-sm flex items-center gap-2">
              <ArrowUpDown className="h-4 w-4" />
              {transfer.isPending ? "Transferring..." : "Execute Transfer"}
            </button>
            {transfer.isSuccess && <p className="text-sm text-emerald-600">✓ Transfer completed</p>}
            {transfer.isError && <p className="text-sm text-red-600">Error processing transfer</p>}
          </div>
        </div>
      )}
    </div>
  );
}
