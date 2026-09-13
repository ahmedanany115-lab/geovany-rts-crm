"use client";
import { useState } from "react";
import { useMaintenanceVisits, useScheduleVisit, useCompleteVisit, useCancelVisit, useMaintenanceContracts } from "@/features/erp/hooks";
import { useT } from "@/hooks/useT";
import { useToast } from "@/components/ui/toast";
import { CalendarDays, Plus, RefreshCw, X, CheckCircle, XCircle, ChevronDown } from "lucide-react";

const STATUS = { 1:"Scheduled", 2:"In Progress", 3:"Completed", 4:"Cancelled" };
const STATUS_CLS = { 1:"bg-blue-100 text-blue-700", 2:"bg-amber-100 text-amber-700", 3:"bg-emerald-100 text-emerald-700", 4:"bg-red-100 text-red-700" };

export default function VisitsPage() {
  const { t } = useT();
  const { toast } = useToast();
  const [filterStatus, setFilterStatus] = useState<number | undefined>();
  const [showForm, setShowForm] = useState(false);
  const [completingId, setCompletingId] = useState<string | null>(null);
  const [completeForm, setCompleteForm] = useState({ actualDate: "", workDescription: "", extraChargesAmount: 0, extraChargesNotes: "" });
  const [form, setForm] = useState({ quarterId: "", scheduledDate: "", technicianName: "", notes: "" });

  const { data: visits, isLoading, refetch } = useMaintenanceVisits({ status: filterStatus });
  const { data: contracts } = useMaintenanceContracts({ status: 2 });
  const schedule  = useScheduleVisit();
  const complete  = useCompleteVisit();
  const cancel    = useCancelVisit();

  const handleSchedule = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await schedule.mutateAsync(form);
      toast("Visit scheduled.", "success");
      setForm({ quarterId: "", scheduledDate: "", technicianName: "", notes: "" });
      setShowForm(false);
    } catch (err: any) { toast(err?.message ?? "Failed to schedule visit.", "error"); }
  };

  const handleComplete = async (id: string) => {
    try {
      await complete.mutateAsync({ id, data: { ...completeForm, actualDate: completeForm.actualDate || new Date().toISOString().split("T")[0], extraChargesAmount: Number(completeForm.extraChargesAmount) } });
      toast("Visit completed.", "success");
      setCompletingId(null);
    } catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  const handleCancel = async (id: string) => {
    try { await cancel.mutateAsync({ id }); toast("Visit cancelled.", "info"); }
    catch (err: any) { toast(err?.message ?? "Failed.", "error"); }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><CalendarDays className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("maintenance_visits")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">{showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}{showForm ? t("cancel") : t("schedule_visit")}</button>
        </div>
      </div>

      {showForm && (
        <form onSubmit={handleSchedule} className="card p-5 space-y-3 border border-primary/20">
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Contract Quarter *</label>
              <select required value={form.quarterId} onChange={e => setForm(f => ({ ...f, quarterId: e.target.value }))} className="input w-full">
                <option value="">Select active contract...</option>
                {contracts?.flatMap(c => Array.from({ length: c.totalQuarters }, (_, i) => ({ contractId: c.id, contractNumber: c.contractNumber, customerName: c.customerName, qNum: i + 1 }))).map(q => (
                  <option key={`${q.contractId}-${q.qNum}`} value={`${q.contractId}-Q${q.qNum}`}>{q.customerName} / {q.contractNumber} / Q{q.qNum}</option>
                ))}
              </select>
              <p className="text-xs text-muted-foreground mt-1">Note: Enter the Quarter ID from the contract detail view for precise scheduling.</p>
            </div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("date")} *</label><input required type="date" value={form.scheduledDate} onChange={e => setForm(f => ({ ...f, scheduledDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">{t("technician")}</label><input value={form.technicianName} onChange={e => setForm(f => ({ ...f, technicianName: e.target.value }))} className="input w-full" /></div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">{t("notes")}</label><input value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" /></div>
          </div>
          <button type="submit" disabled={schedule.isPending} className="btn-primary px-5 py-2 rounded-lg text-sm disabled:opacity-50">{schedule.isPending ? t("saving") : t("schedule_visit")}</button>
        </form>
      )}

      <div className="flex gap-2">
        {[undefined, 1, 2, 3, 4].map(s => (
          <button key={String(s)} onClick={() => setFilterStatus(s)} className={`text-xs px-3 py-1.5 rounded-full font-medium transition-colors ${filterStatus === s ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/80"}`}>
            {s === undefined ? "All" : (STATUS as any)[s]}
          </button>
        ))}
      </div>

      {isLoading ? <div className="text-center py-10 text-muted-foreground">{t("loading")}</div> : (
        <div className="space-y-3">
          {visits?.map(v => (
            <div key={v.id} className="card p-4">
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="font-semibold">{v.customerName}</span>
                    <span className="text-xs text-muted-foreground">{v.contractNumber} / Q{v.quarterNumber}</span>
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${(STATUS_CLS as any)[v.status]}`}>{(STATUS as any)[v.status]}</span>
                  </div>
                  <div className="flex gap-4 mt-1 text-xs text-muted-foreground flex-wrap">
                    <span>📅 {v.scheduledDate}</span>
                    {v.actualDate && <span>✅ {v.actualDate}</span>}
                    {v.technicianName && <span>🔧 {v.technicianName}</span>}
                    {v.extraChargesAmount > 0 && <span className="text-amber-600 font-medium">Extra: {v.extraChargesAmount.toLocaleString()}</span>}
                  </div>
                  {v.workDescription && <p className="text-sm mt-1 text-muted-foreground">{v.workDescription}</p>}
                </div>
                <div className="flex items-center gap-1 shrink-0">
                  {v.status === 1 && (
                    completingId === v.id ? (
                      <div className="space-y-2 min-w-[220px]">
                        <input type="date" value={completeForm.actualDate} onChange={e => setCompleteForm(f => ({ ...f, actualDate: e.target.value }))} className="input w-full text-xs py-1" />
                        <textarea rows={2} value={completeForm.workDescription} onChange={e => setCompleteForm(f => ({ ...f, workDescription: e.target.value }))} placeholder="Work done..." className="input w-full text-xs py-1 resize-none" />
                        <input type="number" step="0.01" value={completeForm.extraChargesAmount} onChange={e => setCompleteForm(f => ({ ...f, extraChargesAmount: Number(e.target.value) }))} placeholder="Extra charges (0 = none)" className="input w-full text-xs py-1" />
                        <div className="flex gap-1">
                          <button onClick={() => handleComplete(v.id)} disabled={complete.isPending} className="flex-1 text-xs py-1.5 rounded bg-emerald-600 text-white hover:bg-emerald-700 flex items-center justify-center gap-1 disabled:opacity-50"><CheckCircle className="h-3.5 w-3.5" />Done</button>
                          <button onClick={() => setCompletingId(null)} className="p-1.5 rounded hover:bg-accent"><X className="h-3.5 w-3.5" /></button>
                        </div>
                      </div>
                    ) : (
                      <>
                        <button onClick={() => setCompletingId(v.id)} className="flex items-center gap-1 text-xs px-2 py-1.5 rounded bg-emerald-50 text-emerald-700 hover:bg-emerald-100"><CheckCircle className="h-3.5 w-3.5" />{t("complete_visit")}</button>
                        <button onClick={() => handleCancel(v.id)} className="flex items-center gap-1 text-xs px-2 py-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600"><XCircle className="h-3.5 w-3.5" /></button>
                      </>
                    )
                  )}
                </div>
              </div>
            </div>
          ))}
          {!visits?.length && <div className="card p-8 text-center text-muted-foreground">{t("no_data")}</div>}
        </div>
      )}
    </div>
  );
}
