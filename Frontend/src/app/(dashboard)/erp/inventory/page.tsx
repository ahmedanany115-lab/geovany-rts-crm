"use client";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { ArrowLeftRight, RefreshCw } from "lucide-react";

interface InventoryBalance {
  id: string;
  productId: string;
  productName: string;
  productSku: string;
  warehouseId: string;
  warehouseName: string;
  quantity: number;
  reservedQuantity: number;
  availableQuantity: number;
  averageCost: number;
  totalValue: number;
}

export default function InventoryPage() {
  const { t } = useT();
  const { data, isLoading, refetch } = useQuery({
    queryKey: ["inventory-balances"],
    queryFn: () => apiFetch<InventoryBalance[]>("/inventory/balances"),
  });

  const totalValue = data?.reduce((s, b) => s + b.totalValue, 0) ?? 0;

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <ArrowLeftRight className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">{t("stock_movements")}</h1>
            <p className="text-sm text-muted-foreground">
              {t("inventory_value")}: <strong>{totalValue.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 })}</strong>
            </p>
          </div>
        </div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">{t("loading")}</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">SKU</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("products")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("warehouses")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">{t("quantity")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Reserved</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Available</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Avg Cost</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Value</th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(b => (
                <tr key={b.id} className={`hover:bg-muted/20 ${b.availableQuantity <= 0 ? "opacity-60" : ""}`}>
                  <td className="p-3 font-mono text-xs">{b.productSku}</td>
                  <td className="p-3 font-medium">{b.productName}</td>
                  <td className="p-3 text-muted-foreground">{b.warehouseName}</td>
                  <td className="p-3 text-right tabular-nums">{b.quantity.toLocaleString()}</td>
                  <td className="p-3 text-right tabular-nums text-amber-600">{b.reservedQuantity > 0 ? b.reservedQuantity.toLocaleString() : "—"}</td>
                  <td className={`p-3 text-right tabular-nums font-semibold ${b.availableQuantity > 0 ? "text-emerald-600" : "text-red-600"}`}>
                    {b.availableQuantity.toLocaleString()}
                  </td>
                  <td className="p-3 text-right tabular-nums text-muted-foreground">{b.averageCost.toLocaleString()}</td>
                  <td className="p-3 text-right tabular-nums">{b.totalValue.toLocaleString()}</td>
                </tr>
              ))}
              {!data?.length && <tr><td colSpan={8} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
