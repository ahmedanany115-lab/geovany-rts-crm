"use client";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import { useT } from "@/hooks/useT";
import { usePrint } from "@/hooks/usePrint";
import { exportCsv } from "@/lib/export-csv";
import { ClipboardList, Printer, Download, RefreshCw } from "lucide-react";

const EGP = (v: number) => v.toLocaleString("en-EG", { style: "currency", currency: "EGP", maximumFractionDigits: 0 });

export default function ProjectsReportPage() {
  const { t } = useT();
  const { printRef, handlePrint } = usePrint("RTS ERP — Maintenance Contracts Report");

  const { data: contracts, isLoading, refetch } = useQuery({
    queryKey: ["maintenance-contracts-report"],
    queryFn: () => apiFetch<any[]>("/maintenance/contracts"),
  });

  const summary = {
    total:  contracts?.length ?? 0,
    active: contracts?.filter((c: any) => c.status === 2).length ?? 0,
    value:  contracts?.reduce((s: number, c: any) => s + (c.contractValue ?? 0), 0) ?? 0,
  };

  const STATUS: Record<number,string> = { 1:"Draft",2:"Active",3:"Suspended",4:"Expired",5:"Cancelled" };
  const ST_CLS: Record<number,string> = {
    1:"bg-muted text-muted-foreground", 2:"bg-emerald-100 text-emerald-700",
    3:"bg-amber-100 text-amber-700",    4:"bg-red-100 text-red-700",
    5:"bg-muted text-muted-foreground",
  };

  const handleExport = () => {
    if (!contracts?.length) return;
    exportCsv(contracts.map((c: any) => ({
      contractNumber: c.contractNumber, customer: c.customerName,
      status: STATUS[c.status] ?? c.status, startDate: c.startDate, endDate: c.endDate,
      value: c.contractValue, quarters: c.contractQuarters,
    })), "maintenance-contracts.csv", [
      { key: "contractNumber", header: "Contract #" },
      { key: "customer",       header: "Customer" },
      { key: "status",         header: "Status" },
      { key: "startDate",      header: "Start Date" },
      { key: "endDate",        header: "End Date" },
      { key: "value",          header: "Value (EGP)" },
      { key: "quarters",       header: "Quarters" },
    ]);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex items-center gap-3">
          <ClipboardList className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">Maintenance Contracts Report</h1>
            <p className="text-sm text-muted-foreground">{summary.active} active · {EGP(summary.value)} total contract value</p>
          </div>
        </div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={handleExport} className="btn-ghost flex items-center gap-2 px-3 py-2 rounded-lg text-sm"><Download className="h-4 w-4" />CSV</button>
          <button onClick={handlePrint} className="btn-ghost flex items-center gap-2 px-3 py-2 rounded-lg text-sm"><Printer className="h-4 w-4" />Print</button>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: "Total Contracts", value: String(summary.total),  cls: "text-foreground" },
          { label: "Active",          value: String(summary.active), cls: "text-emerald-600" },
          { label: "Total Value",     value: EGP(summary.value),    cls: "text-blue-600" },
        ].map(k => (
          <div key={k.label} className="card p-4">
            <p className="text-xs text-muted-foreground">{k.label}</p>
            <p className={`text-xl font-bold ${k.cls}`}>{k.value}</p>
          </div>
        ))}
      </div>

      <div ref={printRef}>
        {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30"><tr>
                <th className="text-left p-3 font-medium text-muted-foreground">Contract #</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Start</th>
                <th className="text-left p-3 font-medium text-muted-foreground">End</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Value</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Qtrs</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
              </tr></thead>
              <tbody className="divide-y divide-border">
                {(contracts ?? []).map((c: any) => (
                  <tr key={c.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{c.contractNumber}</td>
                    <td className="p-3 font-medium">{c.customerName}</td>
                    <td className="p-3 text-muted-foreground text-xs">{c.startDate}</td>
                    <td className="p-3 text-muted-foreground text-xs">{c.endDate}</td>
                    <td className="p-3 text-right tabular-nums">{EGP(c.contractValue ?? 0)}</td>
                    <td className="p-3 text-center">{c.contractQuarters ?? "—"}</td>
                    <td className="p-3 text-center">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${ST_CLS[c.status] ?? "bg-muted"}`}>
                        {STATUS[c.status] ?? c.status}
                      </span>
                    </td>
                  </tr>
                ))}
                {!(contracts?.length) && <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">No maintenance contracts.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
