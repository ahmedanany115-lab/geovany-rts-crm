"use client";
import { useState } from "react";
import { ShoppingBag, RefreshCw, CheckCircle } from "lucide-react";
import { usePurchaseOrders, useApprovePurchaseOrder } from "@/features/erp/hooks";
import { PurchaseOrderStatusLabels } from "@/features/erp/types";
const STATUS_COLORS: Record<number, string> = {1:"bg-muted text-muted-foreground",2:"bg-blue-100 text-blue-700",3:"bg-amber-100 text-amber-700",4:"bg-emerald-100 text-emerald-700",5:"bg-red-100 text-red-700"};
export default function PurchaseOrdersPage() {
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const { data: orders, isLoading, refetch } = usePurchaseOrders({ status: statusFilter });
  const approve = useApprovePurchaseOrder();
  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><ShoppingBag className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Purchase Orders</h1></div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      <div className="flex gap-2 flex-wrap">{[undefined,1,2,3,4,5].map(s=><button key={String(s)} onClick={()=>setStatusFilter(s)} className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${statusFilter===s?"bg-primary text-primary-foreground":"bg-muted text-muted-foreground hover:bg-muted/80"}`}>{s===undefined?"All":PurchaseOrderStatusLabels[s]}</button>)}</div>
      {isLoading ? <div className="text-center py-10 text-muted-foreground">Loading purchase orders...</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr><th className="text-left p-3 font-medium text-muted-foreground">PO #</th><th className="text-left p-3 font-medium text-muted-foreground">Supplier</th><th className="text-left p-3 font-medium text-muted-foreground">Date</th><th className="text-right p-3 font-medium text-muted-foreground">Total</th><th className="text-center p-3 font-medium text-muted-foreground">Status</th><th className="p-3"></th></tr></thead>
            <tbody className="divide-y divide-border">
              {orders?.map(o=>(
                <tr key={o.id} className="hover:bg-muted/20 transition-colors">
                  <td className="p-3 font-mono text-xs">{o.poNumber}</td>
                  <td className="p-3 font-medium">{o.supplierName}</td>
                  <td className="p-3 text-muted-foreground">{o.orderDate}</td>
                  <td className="p-3 text-right tabular-nums font-medium">{o.totalAmount.toLocaleString("en-EG",{style:"currency",currency:o.currencyCode||"EGP"})}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${STATUS_COLORS[o.status]}`}>{o.statusName}</span></td>
                  <td className="p-3">{o.status===1&&<button onClick={()=>approve.mutate(o.id)} disabled={approve.isPending} className="text-blue-600 hover:text-blue-700"><CheckCircle className="h-4 w-4" /></button>}</td>
                </tr>
              ))}
              {!orders?.length && <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">No purchase orders found</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
