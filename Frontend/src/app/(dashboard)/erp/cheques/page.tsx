"use client";

import { useState } from "react";
import { CreditCard, RefreshCw, ArrowDownToLine } from "lucide-react";
import { useCheques, useDepositCheque } from "@/features/erp/hooks";
import { ChequeStatusLabels } from "@/features/erp/types";
import { useBankAccounts } from "@/features/erp/hooks";

const STATUS_COLORS: Record<number, string> = {
  1: "bg-blue-100 text-blue-700",
  2: "bg-amber-100 text-amber-700",
  3: "bg-emerald-100 text-emerald-700",
  4: "bg-red-100 text-red-700",
  5: "bg-muted text-muted-foreground",
};

export default function ChequesPage() {
  const [statusFilter, setStatusFilter] = useState<number | undefined>(1); // default: Received
  const { data: cheques, isLoading, refetch } = useCheques({ status: statusFilter });
  const { data: bankAccounts } = useBankAccounts({ isActive: true });
  const deposit = useDepositCheque();
  const [depositModal, setDepositModal] = useState<{ id: string } | null>(null);
  const [bankAccountId, setBankAccountId] = useState("");
  const [depositDate, setDepositDate] = useState(new Date().toISOString().split("T")[0]);

  const totalOutstanding = cheques?.filter(c => c.status === 1).reduce((s, c) => s + c.amount, 0) ?? 0;

  const handleDeposit = async () => {
    if (!depositModal || !bankAccountId) return;
    await deposit.mutateAsync({ id: depositModal.id, data: { bankAccountId, depositDate } });
    setDepositModal(null);
    setBankAccountId("");
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <CreditCard className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">Cheques</h1>
        </div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg">
          <RefreshCw className="h-4 w-4" />
        </button>
      </div>

      <div className="card p-4">
        <p className="text-sm text-muted-foreground">Outstanding Cheques (Received)</p>
        <p className="text-2xl font-bold mt-1 text-amber-600">{totalOutstanding.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p>
      </div>

      {/* Status filter */}
      <div className="flex gap-2">
        {[undefined, 1, 2, 3, 4].map(s => (
          <button key={String(s)} onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${
              statusFilter === s ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/80"
            }`}>
            {s === undefined ? "All" : ChequeStatusLabels[s]}
          </button>
        ))}
      </div>

      {/* Deposit Modal */}
      {depositModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-background rounded-xl p-6 w-full max-w-md shadow-xl">
            <h2 className="font-semibold mb-4">Deposit Cheque</h2>
            <div className="space-y-3">
              <div>
                <label className="text-sm text-muted-foreground">Bank Account</label>
                <select value={bankAccountId} onChange={e => setBankAccountId(e.target.value)} className="input w-full mt-1">
                  <option value="">Select bank account...</option>
                  {bankAccounts?.map(b => (
                    <option key={b.id} value={b.id}>{b.name} ({b.currencyCode})</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-sm text-muted-foreground">Deposit Date</label>
                <input type="date" value={depositDate} onChange={e => setDepositDate(e.target.value)} className="input w-full mt-1" />
              </div>
            </div>
            <div className="flex gap-2 mt-4">
              <button onClick={handleDeposit} disabled={!bankAccountId || deposit.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">
                {deposit.isPending ? "Processing..." : "Deposit"}
              </button>
              <button onClick={() => setDepositModal(null)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
            </div>
          </div>
        </div>
      )}

      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">Loading cheques...</div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30">
              <tr>
                <th className="text-left p-3 font-medium text-muted-foreground">Cheque #</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Bank</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Amount</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Due Date</th>
                <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
                <th className="p-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {cheques?.map(c => (
                <tr key={c.id} className="hover:bg-muted/20 transition-colors">
                  <td className="p-3 font-mono text-xs">{c.chequeNumber}</td>
                  <td className="p-3 font-medium">{c.customerName}</td>
                  <td className="p-3 text-muted-foreground">{c.bankName}</td>
                  <td className="p-3 text-right tabular-nums font-medium">
                    {c.amount.toLocaleString("en-EG", { style: "currency", currency: c.currencyCode || "EGP" })}
                  </td>
                  <td className="p-3 text-center text-muted-foreground">{c.dueDate}</td>
                  <td className="p-3 text-center">
                    <span className={`text-xs px-2 py-0.5 rounded-full ${STATUS_COLORS[c.status]}`}>
                      {c.statusName}
                    </span>
                  </td>
                  <td className="p-3">
                    {c.status === 1 && (
                      <button
                        onClick={() => setDepositModal({ id: c.id })}
                        className="text-amber-600 hover:text-amber-700 flex items-center gap-1 text-xs"
                        title="Deposit"
                      >
                        <ArrowDownToLine className="h-3.5 w-3.5" /> Deposit
                      </button>
                    )}
                  </td>
                </tr>
              ))}
              {!cheques?.length && (
                <tr><td colSpan={7} className="p-8 text-center text-muted-foreground">No cheques found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
