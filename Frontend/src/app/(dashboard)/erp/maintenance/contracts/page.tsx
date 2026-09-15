"use client";
import { useState } from "react";
import { useMaintenanceContracts, useCreateMaintenanceContract, useChangeContractStatus } from "@/features/erp/hooks";
import { useCustomers } from "@/features/erp/hooks";
import { useCurrencies } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { ClipboardList, Plus, RefreshCw, X, Search, FileText } from "lucide-react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";

const STATUS = { 1:"Draft", 2:"Active", 3:"Suspended", 4:"Completed", 5:"Cancelled" };
const STATUS_CLS = { 1:"bg-muted text-muted-foreground", 2:"bg-emerald-100 text-emerald-700", 3:"bg-amber-100 text-amber-700", 4:"bg-blue-100 text-blue-700", 5:"bg-red-100 text-red-700" };
const INIT = { customerId:"", startDate:"", endDate:"", totalVisitsPerQuarter:1, currencyId:"", contractValue:0, notes:"", postAccountingEntries:true };

export default function ContractsPage() {
  const { t } = useT();
  const { toast } = useToast();
  const [search, setSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState(INIT);

  const { data: contracts, isLoading, refetch } = useMaintenanceContracts({ search: search || undefined });
  const { data: customers } = useCustomers({});
  const { data: currencies } = useCurrencies();
  const create = useCreateMaintenanceContract();
  const changeStatus = useChangeContractStatus();
  const qc = useQueryClient();

  const generateInvoice = useMutation({
    mutationFn: (id: string) => apiFetch<{ invoiceId: string; invoiceNumber: string }>(`/maintenance/contracts/${id}/generate-invoice`, { method: "POST" }),
    onSuccess: (result) => toast(`Invoice ${result.invoiceNumber} created. Go to Customer Invoices to post it.`, "success"),
    onError: (err: any) => toast(err?.message ?? "Failed to generate invoice.", "error"),
  });

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await create.mutateAsync({ ...form, contractValue: Number(form.contractValue), totalVisitsPerQuarter: Number(form.totalVisitsPerQuarter) });
      toast("Contract created successfully.", "success");
      setForm(INIT); setShowForm(false);
    } catch (err: any) { toast(err?.message ?? "Failed to create contract.", "error"); }
  };

  const handleActivate = async (id: string, current: number) => {
    const next = current === 1 ? 2 : current === 2 ? 3 : 2;
    try { await changeStatus.mutateAsync({ id, status: next }); toast("Status updated.", "success"); }
    catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><ClipboardList className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("maintenance_contracts")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">{showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}{showForm ? t("cancel") : "New Contract"}</button>
        </div>
      </div>

      {showForm && (
        <form onSubmit={handleCreate} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">New Service Contract</h2>
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Customer *</label>
              <select required value={form.customerId} onChange={e => setForm(f => ({ ...f, customerId: e.target.value }))} className="input w-full">
                <option value="">Select customer...</option>
                {customers?.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">Start Date *</label><input required type="date" value={form.startDate} onChange={e => setForm(f => ({ ...f, startDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">End Date *</label><input required type="date" value={form.endDate} min={form.startDate} onChange={e => setForm(f => ({ ...f, endDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("visits_per_quarter")} *</label><input required type="number" min="1" max="12" value={form.totalVisitsPerQuarter} onChange={e => setForm(f => ({ ...f, totalVisitsPerQuarter: Number(e.target.value) }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("currency")}</label>
              <select value={form.currencyId} onChange={e => setForm(f => ({ ...f, currencyId: e.target.value }))} className="input w-full">
                <option value="">Select currency...</option>
                {currencies?.map(c => <option key={c.id} value={c.id}>{c.code} — {c.name}</option>)}
              </select>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("contract_value")}</label><input type="number" step="0.01" min="0" value={form.contractValue} onChange={e => setForm(f => ({ ...f, contractValue: Number(e.target.value) }))} className="input w-full" /></div>
            <div className="flex items-center gap-2 mt-4"><input type="checkbox" id="postJe" checked={form.postAccountingEntries} onChange={e => setForm(f => ({ ...f, postAccountingEntries: e.target.checked }))} /><label htmlFor="postJe" className="text-sm">Post accounting entries on save</label></div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">{t("notes")}</label><textarea rows={2} value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} className="input w-full resize-none" /></div>
          </div>
          <button type="submit" disabled={create.isPending} className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50">{create.isPending ? t("saving") : t("save")}</button>
        </form>
      )}

      <div className="relative max-w-sm"><Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" /><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search contracts..." className="input pl-10 w-full" /></div>

      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("contract_number")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("customers")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Period</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("visits_per_quarter")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">{t("contract_value")}</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Remaining</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {contracts?.map(c => (
                <tr key={c.id} className="hover:bg-muted/20">
                  <td className="p-3 font-mono text-xs">{c.contractNumber}</td>
                  <td className="p-3 font-medium">{c.customerName}</td>
                  <td className="p-3 text-muted-foreground text-xs">{c.startDate} → {c.endDate}</td>
                  <td className="p-3 text-center">{c.totalVisitsPerQuarter}</td>
                  <td className="p-3 text-right tabular-nums">{c.contractValue.toLocaleString()} {c.currencyCode}</td>
                  <td className="p-3 text-center">
                    {c.activeQuarter > 0 ? (
                      <span className="text-xs font-medium">{c.remainingVisitsThisQuarter} <span className="text-muted-foreground">Q{c.activeQuarter}</span></span>
                    ) : "—"}
                  </td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full font-medium ${(STATUS_CLS as any)[c.status]}`}>{(STATUS as any)[c.status]}</span></td>
                  <td className="p-3 text-right">
                    <div className="flex items-center justify-end gap-1">
                      {c.status === 2 && (
                        <button onClick={() => generateInvoice.mutate(c.id)} disabled={generateInvoice.isPending}
                          title="Generate Customer Invoice"
                          className="flex items-center gap-1 text-xs px-2 py-1 rounded bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50">
                          <FileText className="h-3.5 w-3.5" /> Invoice
                        </button>
                      )}
                      {c.status < 4 && (
                        <button onClick={() => handleActivate(c.id, c.status)} className="text-xs px-2 py-1 rounded hover:bg-accent text-muted-foreground">
                          {c.status === 1 ? "Activate" : c.status === 2 ? "Suspend" : "Activate"}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {!contracts?.length && <tr><td colSpan={8} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
