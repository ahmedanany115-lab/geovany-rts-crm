"use client";

import { useState } from "react";
import { Package, Plus, RefreshCw, PowerOff, AlertTriangle } from "lucide-react";
import { useProducts, useToggleProductStatus } from "@/features/erp/hooks";

function StockBadge({ qty, min }: { qty: number; min: number }) {
  const isLow = qty <= min && qty > 0;
  const isZero = qty === 0;
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${
      isZero ? "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400"
    : isLow  ? "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400"
             : "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400"
    }`}>
      {(isZero || isLow) && <AlertTriangle className="h-3 w-3" />}
      {qty.toLocaleString()}
    </span>
  );
}

export default function ProductsPage() {
  const [search, setSearch] = useState("");
  const { data: products, isLoading, refetch } = useProducts({ search: search || undefined });
  const toggle = useToggleProductStatus();

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Package className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">Products & Inventory</h1>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg">
            <RefreshCw className="h-4 w-4" />
          </button>
          <button className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            <Plus className="h-4 w-4" /> Add Product
          </button>
        </div>
      </div>

      {/* Stats summary */}
      <div className="grid grid-cols-3 gap-4">
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Total Products</p>
          <p className="text-2xl font-bold mt-1">{products?.length ?? 0}</p>
        </div>
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Active Products</p>
          <p className="text-2xl font-bold mt-1 text-emerald-600">{products?.filter(p => p.isActive).length ?? 0}</p>
        </div>
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Low / Out of Stock</p>
          <p className="text-2xl font-bold mt-1 text-amber-600">
            {products?.filter(p => p.totalQuantity <= p.minimumStock).length ?? 0}
          </p>
        </div>
      </div>

      <div className="relative max-w-md">
        <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search products..." className="input w-full pl-4" />
      </div>

      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">Loading products...</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30">
              <tr>
                <th className="text-left p-3 font-medium text-muted-foreground">SKU</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Product</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Category</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Sales Price</th>
                <th className="text-right p-3 font-medium text-muted-foreground">VAT</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Stock</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
                <th className="p-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {products?.map(p => (
                <tr key={p.id} className="hover:bg-muted/20 transition-colors">
                  <td className="p-3 font-mono text-xs text-muted-foreground">{p.sku}</td>
                  <td className="p-3">
                    <p className="font-medium">{p.name}</p>
                    <p className="text-xs text-muted-foreground">{p.unit}</p>
                  </td>
                  <td className="p-3 text-muted-foreground">{p.category ?? "—"}</td>
                  <td className="p-3 text-right tabular-nums">
                    {p.salesPrice.toLocaleString("en-EG", { style: "currency", currency: p.currencyCode || "EGP" })}
                  </td>
                  <td className="p-3 text-right text-muted-foreground">{p.taxRatePercent}%</td>
                  <td className="p-3 text-center">
                    <StockBadge qty={p.totalQuantity} min={p.minimumStock} />
                  </td>
                  <td className="p-3 text-center">
                    <span className={`text-xs px-2 py-0.5 rounded-full ${p.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>
                      {p.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className="p-3">
                    <button onClick={() => toggle.mutate(p.id)} className="text-muted-foreground hover:text-foreground">
                      <PowerOff className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ))}
              {!products?.length && (
                <tr><td colSpan={8} className="p-8 text-center text-muted-foreground">No products found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
