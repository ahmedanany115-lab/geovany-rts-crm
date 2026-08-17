"use client";
import { Warehouse, RefreshCw } from "lucide-react";
import { useWarehouses } from "@/features/erp/hooks";
export default function WarehousesPage() {
  const { data: warehouses, isLoading, refetch } = useWarehouses();
  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Warehouse className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Warehouses</h1></div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      {isLoading ? <div className="text-center py-10 text-muted-foreground">Loading warehouses...</div> : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {warehouses?.map(w => (
            <div key={w.id} className="card p-5">
              <div className="flex items-start justify-between">
                <div>
                  <p className="font-semibold">{w.name}</p>
                  <p className="text-xs font-mono text-muted-foreground">{w.code}</p>
                </div>
                <span className={`text-xs px-2 py-0.5 rounded-full ${w.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>{w.isActive ? "Active" : "Inactive"}</span>
              </div>
              {w.location && <p className="text-sm text-muted-foreground mt-2">{w.location}</p>}
              <p className="text-sm text-muted-foreground mt-2">{w.productCount} products in stock</p>
            </div>
          ))}
          {!warehouses?.length && <div className="col-span-3 text-center py-10 text-muted-foreground">No warehouses found</div>}
        </div>
      )}
    </div>
  );
}
