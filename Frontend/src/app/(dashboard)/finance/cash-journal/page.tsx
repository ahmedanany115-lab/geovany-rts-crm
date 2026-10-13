"use client";

import Link from "next/link";
import { useState } from "react";
import {
  useCashReceipts,
  useCashPayments,
  usePostCashReceipt,
  useVoidCashReceipt,
  useDeleteCashReceipt,
  usePostCashPayment,
  useVoidCashPayment,
  useDeleteCashPayment,
} from "@/features/finance/hooks";
import { useToast } from "@/components/ui/toast";
import {
  BookOpen, Plus, ArrowDownCircle, ArrowUpCircle, RefreshCw,
  Pencil, Send, Ban, Trash2, ChevronDown,
} from "lucide-react";

const STATUS = {
  1: { label: "Draft", cls: "bg-amber-100 text-amber-700" },
  2: { label: "Posted", cls: "bg-emerald-100 text-emerald-700" },
  3: { label: "Voided", cls: "bg-red-100 text-red-600" },
};

function money(n: number) {
  return n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export default function CashJournalPage() {
  const { toast } = useToast();
  const [status, setStatus] = useState<number | undefined>();
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [showAdd, setShowAdd] = useState(false);

  const params = { status, fromDate: fromDate || undefined, toDate: toDate || undefined };
  const receipts = useCashReceipts(params);
  const payments = useCashPayments(params);

  const postReceipt = usePostCashReceipt();
  const voidReceipt = useVoidCashReceipt();
  const deleteReceipt = useDeleteCashReceipt();
  const postPayment = usePostCashPayment();
  const voidPayment = useVoidCashPayment();
  const deletePayment = useDeleteCashPayment();

  const refresh = () => {
    receipts.refetch();
    payments.refetch();
  };

  const run = async (action: () => Promise<unknown>, success: string) => {
    try {
      await action();
      toast(success, "success");
      refresh();
    } catch (err: any) {
      toast(err?.message ?? "Action failed.", "error");
    }
  };

  const receiptRows = receipts.data ?? [];
  const paymentRows = payments.data ?? [];

  return (
    <div className="p-6 space-y-5">
      <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-3">
        <div className="flex items-center gap-3">
          <BookOpen className="h-6 w-6 text-primary" />
          <div>
            <h1 className="text-2xl font-semibold">Cash Journal</h1>
            <p className="text-sm text-muted-foreground">Receipts and payments in one workspace</p>
          </div>
        </div>

        <div className="relative">
          <button
            onClick={() => setShowAdd(v => !v)}
            className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm"
          >
            <Plus className="h-4 w-4" /> Add Cash Transaction <ChevronDown className="h-4 w-4" />
          </button>
          {showAdd && (
            <div className="absolute right-0 mt-2 w-56 rounded-lg border bg-background shadow-lg z-20 overflow-hidden">
              <Link href="/finance/cash-receipts?new=1" className="flex items-center gap-2 px-4 py-3 text-sm hover:bg-muted/40">
                <ArrowDownCircle className="h-4 w-4 text-emerald-600" /> New Cash Receipt
              </Link>
              <Link href="/finance/cash-payments?new=1" className="flex items-center gap-2 px-4 py-3 text-sm hover:bg-muted/40">
                <ArrowUpCircle className="h-4 w-4 text-red-600" /> New Cash Payment
              </Link>
            </div>
          )}
        </div>
      </div>

      <div className="card p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="text-xs text-muted-foreground block mb-1">Status</label>
            <select
              value={status ?? ""}
              onChange={e => setStatus(e.target.value ? Number(e.target.value) : undefined)}
              className="input text-sm"
            >
              <option value="">All Statuses</option>
              <option value="1">Draft</option>
              <option value="2">Posted</option>
              <option value="3">Voided</option>
            </select>
          </div>
          <div>
            <label className="text-xs text-muted-foreground block mb-1">From</label>
            <input type="date" value={fromDate} onChange={e => setFromDate(e.target.value)} className="input text-sm" />
          </div>
          <div>
            <label className="text-xs text-muted-foreground block mb-1">To</label>
            <input type="date" value={toDate} onChange={e => setToDate(e.target.value)} className="input text-sm" />
          </div>
          <button onClick={refresh} className="btn-ghost border px-3 py-2 rounded-lg">
            <RefreshCw className="h-4 w-4" />
          </button>
        </div>
      </div>

      <div className="grid xl:grid-cols-2 gap-5">
        <section className="card overflow-hidden">
          <div className="px-4 py-3 border-b flex items-center justify-between">
            <div className="flex items-center gap-2">
              <ArrowDownCircle className="h-5 w-5 text-emerald-600" />
              <div>
                <h2 className="font-semibold">Cash Receipts</h2>
                <p className="text-xs text-muted-foreground">{receiptRows.length} record(s)</p>
              </div>
            </div>
            <Link href="/finance/cash-receipts?new=1" className="text-xs text-primary hover:underline">+ Add Receipt</Link>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/30 border-b">
                <tr>
                  <th className="text-left px-3 py-2">Receipt #</th>
                  <th className="text-left px-3 py-2">Date</th>
                  <th className="text-left px-3 py-2">Received From</th>
                  <th className="text-right px-3 py-2">Amount</th>
                  <th className="text-left px-3 py-2">Status</th>
                  <th className="px-3 py-2"></th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {receipts.isLoading ? (
                  <tr><td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">Loading receipts…</td></tr>
                ) : receiptRows.length === 0 ? (
                  <tr><td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">No cash receipts found.</td></tr>
                ) : receiptRows.map((r: any) => {
                  const st = STATUS[r.status as keyof typeof STATUS] ?? { label: r.statusName ?? "—", cls: "bg-muted" };
                  return (
                    <tr key={r.id} className="hover:bg-muted/20">
                      <td className="px-3 py-2 font-mono text-xs">{r.receiptNumber}</td>
                      <td className="px-3 py-2 text-muted-foreground">{r.receiptDate}</td>
                      <td className="px-3 py-2 max-w-[160px] truncate">{r.receivedFrom || "—"}</td>
                      <td className="px-3 py-2 text-right font-medium tabular-nums">{money(r.amount)} {r.currencyCode}</td>
                      <td className="px-3 py-2"><span className={`text-xs px-2 py-0.5 rounded-full ${st.cls}`}>{st.label}</span></td>
                      <td className="px-3 py-2">
                        <div className="flex items-center justify-end gap-1">
                          {r.status === 1 && (
                            <>
                              <Link href={`/finance/cash-receipts?edit=${r.id}`} title="Edit" className="p-1.5 rounded hover:bg-blue-50 text-muted-foreground hover:text-blue-600">
                                <Pencil className="h-3.5 w-3.5" />
                              </Link>
                              <button title="Post" onClick={() => confirm(`Post ${r.receiptNumber}?`) && run(() => postReceipt.mutateAsync(r.id), `${r.receiptNumber} posted.`)} className="p-1.5 rounded hover:bg-emerald-50 text-muted-foreground hover:text-emerald-600">
                                <Send className="h-3.5 w-3.5" />
                              </button>
                              <button title="Delete" onClick={() => confirm(`Delete ${r.receiptNumber}?`) && run(() => deleteReceipt.mutateAsync(r.id), `${r.receiptNumber} deleted.`)} className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600">
                                <Trash2 className="h-3.5 w-3.5" />
                              </button>
                            </>
                          )}
                          {r.status === 2 && (
                            <button title="Void" onClick={() => confirm(`Void ${r.receiptNumber}?`) && run(() => voidReceipt.mutateAsync(r.id), `${r.receiptNumber} voided.`)} className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600">
                              <Ban className="h-3.5 w-3.5" />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>

        <section className="card overflow-hidden">
          <div className="px-4 py-3 border-b flex items-center justify-between">
            <div className="flex items-center gap-2">
              <ArrowUpCircle className="h-5 w-5 text-red-600" />
              <div>
                <h2 className="font-semibold">Cash Payments</h2>
                <p className="text-xs text-muted-foreground">{paymentRows.length} record(s)</p>
              </div>
            </div>
            <Link href="/finance/cash-payments?new=1" className="text-xs text-primary hover:underline">+ Add Payment</Link>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/30 border-b">
                <tr>
                  <th className="text-left px-3 py-2">Payment #</th>
                  <th className="text-left px-3 py-2">Date</th>
                  <th className="text-left px-3 py-2">Paid To</th>
                  <th className="text-right px-3 py-2">Amount</th>
                  <th className="text-left px-3 py-2">Status</th>
                  <th className="px-3 py-2"></th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {payments.isLoading ? (
                  <tr><td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">Loading payments…</td></tr>
                ) : paymentRows.length === 0 ? (
                  <tr><td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">No cash payments found.</td></tr>
                ) : paymentRows.map((p: any) => {
                  const st = STATUS[p.status as keyof typeof STATUS] ?? { label: p.statusName ?? "—", cls: "bg-muted" };
                  return (
                    <tr key={p.id} className="hover:bg-muted/20">
                      <td className="px-3 py-2 font-mono text-xs">{p.paymentNumber}</td>
                      <td className="px-3 py-2 text-muted-foreground">{p.paymentDate}</td>
                      <td className="px-3 py-2 max-w-[160px] truncate">{p.paidTo || "—"}</td>
                      <td className="px-3 py-2 text-right font-medium tabular-nums">{money(p.amount)} {p.currencyCode}</td>
                      <td className="px-3 py-2"><span className={`text-xs px-2 py-0.5 rounded-full ${st.cls}`}>{st.label}</span></td>
                      <td className="px-3 py-2">
                        <div className="flex items-center justify-end gap-1">
                          {p.status === 1 && (
                            <>
                              <Link href={`/finance/cash-payments?edit=${p.id}`} title="Edit" className="p-1.5 rounded hover:bg-blue-50 text-muted-foreground hover:text-blue-600">
                                <Pencil className="h-3.5 w-3.5" />
                              </Link>
                              <button title="Post" onClick={() => confirm(`Post ${p.paymentNumber}?`) && run(() => postPayment.mutateAsync(p.id), `${p.paymentNumber} posted.`)} className="p-1.5 rounded hover:bg-emerald-50 text-muted-foreground hover:text-emerald-600">
                                <Send className="h-3.5 w-3.5" />
                              </button>
                              <button title="Delete" onClick={() => confirm(`Delete ${p.paymentNumber}?`) && run(() => deletePayment.mutateAsync(p.id), `${p.paymentNumber} deleted.`)} className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600">
                                <Trash2 className="h-3.5 w-3.5" />
                              </button>
                            </>
                          )}
                          {p.status === 2 && (
                            <button title="Void" onClick={() => confirm(`Void ${p.paymentNumber}?`) && run(() => voidPayment.mutateAsync(p.id), `${p.paymentNumber} voided.`)} className="p-1.5 rounded hover:bg-red-50 text-muted-foreground hover:text-red-600">
                              <Ban className="h-3.5 w-3.5" />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </div>
  );
}
