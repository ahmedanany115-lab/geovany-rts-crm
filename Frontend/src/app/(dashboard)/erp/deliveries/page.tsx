"use client";

import { Truck, RefreshCw, Trash2 } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useToast } from "@/components/ui/toast";

interface SalesDeliveryDto {
  id: string;
  deliveryNumber: string;
  salesOrderNumber: string;
  customerName: string;
  warehouseName: string;
  deliveryDate: string;
  totalCOGS: number;
  createdAt: string;
}

export default function DeliveriesPage() {
  const { toast } = useToast();
  const qc = useQueryClient();
  const { data: deliveries, isLoading, refetch } = useQuery({
    queryKey: ["sales-deliveries"],
    queryFn: () => apiFetch<SalesDeliveryDto[]>("/salesdeliveries"),
  });

  const remove = useMutation({
    mutationFn: (id: string) => apiFetch<void>(`/salesdeliveries/${id}`, { method: "DELETE" }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["sales-deliveries"] });
      qc.invalidateQueries({ queryKey: ["sales-orders"] });
      qc.invalidateQueries({ queryKey: ["product-stock"] });
      toast("Delivery reversed and removed.", "info");
    },
    onError: (e: any) => toast(e?.message ?? "Unable to reverse delivery.", "error"),
  });

  const totalCOGS = deliveries?.reduce((s, d) => s + d.totalCOGS, 0) ?? 0;

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Truck className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">Sales Deliveries</h1>
        </div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg">
          <RefreshCw className="h-4 w-4" />
        </button>
      </div>

      <div className="grid grid-cols-3 gap-4">
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Total Deliveries</p>
          <p className="text-2xl font-bold mt-1">{deliveries?.length ?? 0}</p>
        </div>
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Total COGS Issued</p>
          <p className="text-2xl font-bold mt-1">
            {totalCOGS.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}
          </p>
        </div>
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Note</p>
          <p className="text-xs text-muted-foreground mt-1">Deliveries are created from Sales Orders. Each delivery decreases warehouse inventory and posts a COGS journal entry.</p>
        </div>
      </div>

      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">Loading deliveries...</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30">
              <tr>
                <th className="text-left p-3 font-medium text-muted-foreground">Delivery #</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Sales Order</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Warehouse</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
                <th className="text-right p-3 font-medium text-muted-foreground">COGS</th>
                <th className="p-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {deliveries?.map(d => (
                <tr key={d.id} className="hover:bg-muted/20 transition-colors">
                  <td className="p-3 font-mono text-xs">{d.deliveryNumber}</td>
                  <td className="p-3 text-muted-foreground font-mono text-xs">{d.salesOrderNumber}</td>
                  <td className="p-3 font-medium">{d.customerName}</td>
                  <td className="p-3 text-muted-foreground">{d.warehouseName}</td>
                  <td className="p-3 text-muted-foreground">{d.deliveryDate}</td>
                  <td className="p-3 text-right tabular-nums">
                    {d.totalCOGS.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}
                  </td>
                  <td className="p-3 text-right">
                    <button
                      onClick={() => confirm(`Reverse delivery ${d.deliveryNumber}? Stock and COGS will be reversed.`) && remove.mutate(d.id)}
                      disabled={remove.isPending}
                      title="Reverse and remove delivery"
                      className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600 disabled:opacity-50">
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </td>
                </tr>
              ))}
              {!deliveries?.length && (
                <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">
                  No deliveries found. Create a delivery from an approved Sales Order.
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
