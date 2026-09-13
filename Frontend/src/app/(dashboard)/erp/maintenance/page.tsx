"use client";
import { useMaintenanceContracts, useMaintenanceVisits } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { Wrench, RefreshCw, AlertTriangle, CheckCircle, Clock, FileText } from "lucide-react";
import Link from "next/link";

const STATUS_COLORS: Record<number, string> = {
  1: "bg-muted text-muted-foreground",
  2: "bg-emerald-100 text-emerald-700",
  3: "bg-amber-100 text-amber-700",
  4: "bg-blue-100 text-blue-700",
  5: "bg-red-100 text-red-700",
};

export default function MaintenanceDashboard() {
  const { t } = useT();
  const { data: contracts, refetch: rc } = useMaintenanceContracts();
  const { data: visits, refetch: rv }    = useMaintenanceVisits();

  const today = new Date().toISOString().split("T")[0];
  const active  = contracts?.filter(c => c.status === 2) ?? [];
  const expiring = contracts?.filter(c => c.status === 2 && c.endDate <= new Date(Date.now() + 30*86400000).toISOString().split("T")[0]) ?? [];
  const upcoming = visits?.filter(v => v.status === 1 && v.scheduledDate >= today).slice(0, 5) ?? [];
  const overdue  = visits?.filter(v => v.status === 1 && v.scheduledDate < today) ?? [];

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Wrench className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("maintenance")}</h1></div>
        <button onClick={() => { rc(); rv(); }} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
      </div>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: "Active Contracts", value: active.length, icon: FileText, color: "text-emerald-600" },
          { label: "Expiring (30d)", value: expiring.length, icon: AlertTriangle, color: "text-amber-600" },
          { label: "Upcoming Visits", value: upcoming.length, icon: Clock, color: "text-blue-600" },
          { label: "Overdue Visits", value: overdue.length, icon: AlertTriangle, color: "text-red-600" },
        ].map(k => (
          <div key={k.label} className="card p-4">
            <div className="flex items-center gap-2 mb-1"><k.icon className={`h-5 w-5 ${k.color}`} /><p className="text-sm text-muted-foreground">{k.label}</p></div>
            <p className={`text-2xl font-bold tabular-nums ${k.color}`}>{k.value}</p>
          </div>
        ))}
      </div>
      <div className="grid md:grid-cols-2 gap-6">
        <div className="card overflow-hidden">
          <div className="p-4 border-b font-medium flex items-center justify-between">
            Upcoming Visits <Link href="/erp/maintenance/visits" className="text-xs text-primary">View all →</Link>
          </div>
          <div className="divide-y">
            {upcoming.length === 0 && <p className="p-4 text-sm text-muted-foreground">No upcoming visits.</p>}
            {upcoming.map(v => (
              <div key={v.id} className="p-3 flex items-center justify-between text-sm">
                <div>
                  <p className="font-medium">{v.customerName}</p>
                  <p className="text-xs text-muted-foreground">{v.contractNumber} · Q{v.quarterNumber} · {v.technicianName ?? "Unassigned"}</p>
                </div>
                <span className="text-xs font-mono">{v.scheduledDate}</span>
              </div>
            ))}
          </div>
        </div>
        <div className="card overflow-hidden">
          <div className="p-4 border-b font-medium flex items-center justify-between">
            Active Contracts <Link href="/erp/maintenance/contracts" className="text-xs text-primary">View all →</Link>
          </div>
          <div className="divide-y">
            {active.length === 0 && <p className="p-4 text-sm text-muted-foreground">No active contracts.</p>}
            {active.slice(0, 5).map(c => (
              <div key={c.id} className="p-3 flex items-center justify-between text-sm">
                <div>
                  <p className="font-medium">{c.customerName}</p>
                  <p className="text-xs text-muted-foreground">{c.contractNumber} · {c.remainingVisitsThisQuarter} visits left Q{c.activeQuarter}</p>
                </div>
                <span className="text-xs text-muted-foreground">{c.endDate}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
