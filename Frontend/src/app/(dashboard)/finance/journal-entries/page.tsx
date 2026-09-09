"use client";
import { useState } from "react";
import { useJournalEntries, usePostJournalEntry } from "@/features/finance/hooks";
import { useT } from "@/hooks/useT";
import { FileText, RefreshCw, CheckCircle, Link as LinkIcon } from "lucide-react";
import Link from "next/link";

const STATUS: Record<number, { label: string; cls: string }> = {
  1: { label: "Draft",  cls: "bg-muted text-muted-foreground" },
  2: { label: "Posted", cls: "bg-emerald-100 text-emerald-700" },
  3: { label: "Reversed", cls: "bg-red-100 text-red-700" },
};

export default function JournalEntriesPage() {
  const { t } = useT();
  const [params, setParams] = useState<{ status?: number; fromDate?: string; toDate?: string }>({});
  const { data, isLoading, refetch } = useJournalEntries(params);
  const postEntry = usePostJournalEntry();

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><FileText className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">{t("journal_entries")}</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
        </div>
      </div>
      {/* Filters */}
      <div className="flex gap-3 flex-wrap">
        <select value={params.status ?? ""} onChange={e => setParams(p => ({ ...p, status: e.target.value ? Number(e.target.value) : undefined }))} className="input text-sm">
          <option value="">All Statuses</option>
          <option value="1">Draft</option>
          <option value="2">Posted</option>
          <option value="3">Reversed</option>
        </select>
        <input type="date" value={params.fromDate ?? ""} onChange={e => setParams(p => ({ ...p, fromDate: e.target.value || undefined }))} className="input text-sm" placeholder="From" />
        <input type="date" value={params.toDate ?? ""} onChange={e => setParams(p => ({ ...p, toDate: e.target.value || undefined }))} className="input text-sm" placeholder="To" />
      </div>
      <div className="card overflow-hidden">
        {isLoading ? <div className="p-8 text-center text-muted-foreground">{t("loading")}</div> : (
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Entry #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("date")}</th>
              <th className="text-left p-3 font-medium text-muted-foreground">{t("description")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">{t("debit")}</th>
              <th className="text-right p-3 font-medium text-muted-foreground">{t("credit")}</th>
              <th className="text-center p-3 font-medium text-muted-foreground">{t("status")}</th>
              <th className="p-3"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {data?.map(je => {
                const st = STATUS[je.status] ?? { label: "?", cls: "bg-muted" };
                return (
                  <tr key={je.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{je.entryNumber}</td>
                    <td className="p-3 text-muted-foreground">{je.entryDate}</td>
                    <td className="p-3 max-w-xs truncate">{je.description}</td>
                    <td className="p-3 text-right tabular-nums">{je.totalDebit.toLocaleString()}</td>
                    <td className="p-3 text-right tabular-nums">{je.totalCredit.toLocaleString()}</td>
                    <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${st.cls}`}>{st.label}</span></td>
                    <td className="p-3 text-right">
                      {je.status === 1 && (
                        <button onClick={() => postEntry.mutate(je.id)} className="flex items-center gap-1 text-xs px-2 py-1 rounded hover:bg-accent text-emerald-600">
                          <CheckCircle className="h-3.5 w-3.5" />{t("post")}
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
              {!data?.length && <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">{t("no_data")}</td></tr>}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
