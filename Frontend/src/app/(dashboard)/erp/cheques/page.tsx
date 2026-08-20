"use client";
import { useState } from "react";
import { CreditCard, RefreshCw, ArrowDownToLine, Plus, X } from "lucide-react";
import { useCheques, useDepositCheque, useReceiveCheque, useBankAccounts, useCustomers } from "@/features/erp/hooks";
import { ChequeStatusLabels } from "@/features/erp/types";
import { useEgpCurrencyId } from "@/features/erp/hooks/useCurrency";

const STATUS_COLORS: Record<number, string> = {
  1: "bg-blue-100 text-blue-700",
  2: "bg-amber-100 text-amber-700",
  3: "bg-emerald-100 text-emerald-700",
  4: "bg-red-100 text-red-700",
  5: "bg-muted text-muted-foreground",
};

export default function ChequesPage() {
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [showReceiveForm, setShowReceiveForm] = useState(false);
  const [depositModal, setDepositModal] = useState<{ id: string; amount: number; customer: string } | null>(null);
  const [bankAccountId, setBankAccountId] = useState("");
  const [depositDate, setDepositDate] = useState(new Date().toISOString().split("T")[0]);
  const today = new Date().toISOString().split("T")[0];

  const [recvForm, setRecvForm] = useState({
    customerId: "", chequeNumber: "", bankName: "", amount: "",
    currencyId: "", issueDate: today, dueDate: today, receivedDate: today, notes: "",
  });

  const { data: cheques, isLoading, refetch } = useCheques({ status: statusFilter });
  const { data: bankAccounts } = useBankAccounts({ isActive: true });
  const { data: customers } = useCustomers({ isActive: true });
  const deposit = useDepositCheque();
  const receive = useReceiveCheque();
  const egpId = useEgpCurrencyId();

  const totalReceived = cheques?.filter(c => c.status === 1).reduce((s, c) => s + c.amount, 0) ?? 0;
  const totalDeposited = cheques?.filter(c => c.status === 2).reduce((s, c) => s + c.amount, 0) ?? 0;

  const handleDeposit = async () => {
    if (!depositModal || !bankAccountId) return;
    await deposit.mutateAsync({ id: depositModal.id, data: { bankAccountId, depositDate } });
    setDepositModal(null); setBankAccountId("");
  };

  const handleReceive = async (e: React.FormEvent) => {
    e.preventDefault();
    // currencyId: use EGP from seeded data
    const egpCurrencyId = egpId || recvForm.currencyId;
    await receive.mutateAsync({
      ...recvForm,
      amount: parseFloat(recvForm.amount),
      amountBase: parseFloat(recvForm.amount),
      currencyId: recvForm.currencyId || egpCurrencyId,
      exchangeRate: 1,
    });
    setRecvForm({ customerId: "", chequeNumber: "", bankName: "", amount: "", currencyId: "", issueDate: today, dueDate: today, receivedDate: today, notes: "" });
    setShowReceiveForm(false);
  };

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><CreditCard className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Cheques Receivable</h1></div>
        <div className="flex gap-2">
          <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowReceiveForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showReceiveForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showReceiveForm ? "Cancel" : "Receive Cheque"}
          </button>
        </div>
      </div>

      {/* KPI */}
      <div className="grid grid-cols-3 gap-4">
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Received (Pending Deposit)</p>
          <p className="text-2xl font-bold mt-1 text-blue-600">{totalReceived.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p>
          <p className="text-xs text-muted-foreground mt-1">Dr Cheques Receivable / Cr Customer AR</p>
        </div>
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Deposited (In Bank)</p>
          <p className="text-2xl font-bold mt-1 text-amber-600">{totalDeposited.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p>
          <p className="text-xs text-muted-foreground mt-1">Dr Bank / Cr Cheques Receivable</p>
        </div>
        <div className="card p-4">
          <p className="text-sm text-muted-foreground">Total Cheques</p>
          <p className="text-2xl font-bold mt-1">{cheques?.length ?? 0}</p>
        </div>
      </div>

      {/* Receive Form */}
      {showReceiveForm && (
        <form onSubmit={handleReceive} className="card p-5 space-y-4 border border-blue-200 dark:border-blue-800">
          <div>
            <h2 className="font-semibold">Receive New Cheque</h2>
            <p className="text-xs text-muted-foreground mt-0.5">Creates: Dr Cheques Receivable / Cr Customer Receivable</p>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">Customer *</label>
              <select required value={recvForm.customerId} onChange={e => setRecvForm(f => ({ ...f, customerId: e.target.value }))} className="input w-full">
                <option value="">Select customer...</option>
                {customers?.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Cheque Number *</label>
              <input required value={recvForm.chequeNumber} onChange={e => setRecvForm(f => ({ ...f, chequeNumber: e.target.value }))} className="input w-full" placeholder="CHQ-001234" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Bank Name *</label>
              <input required value={recvForm.bankName} onChange={e => setRecvForm(f => ({ ...f, bankName: e.target.value }))} className="input w-full" placeholder="CIB, NBE, Ahly..." /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Amount *</label>
              <input type="number" required min="0.01" step="0.01" value={recvForm.amount} onChange={e => setRecvForm(f => ({ ...f, amount: e.target.value }))} className="input w-full" placeholder="0.00" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Issue Date</label>
              <input type="date" value={recvForm.issueDate} onChange={e => setRecvForm(f => ({ ...f, issueDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Due Date *</label>
              <input type="date" required value={recvForm.dueDate} onChange={e => setRecvForm(f => ({ ...f, dueDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Received Date</label>
              <input type="date" value={recvForm.receivedDate} onChange={e => setRecvForm(f => ({ ...f, receivedDate: e.target.value }))} className="input w-full" /></div>
            <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Notes</label>
              <input value={recvForm.notes} onChange={e => setRecvForm(f => ({ ...f, notes: e.target.value }))} className="input w-full" placeholder="Optional..." /></div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={receive.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">{receive.isPending ? "Recording..." : "Record Cheque Receipt"}</button>
            <button type="button" onClick={() => setShowReceiveForm(false)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
          </div>
          {receive.isError && <p className="text-sm text-red-600">Error recording cheque. Check all required fields.</p>}
          {receive.isSuccess && <p className="text-sm text-emerald-600">✓ Cheque recorded. Journal entry created automatically.</p>}
        </form>
      )}

      {/* Deposit Modal */}
      {depositModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-background rounded-xl p-6 w-full max-w-md shadow-xl border border-border">
            <h2 className="font-semibold mb-1">Deposit Cheque</h2>
            <p className="text-sm text-muted-foreground mb-4">
              {depositModal.customer} — {depositModal.amount.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}
            </p>
            <p className="text-xs text-muted-foreground mb-4">Creates: Dr Bank / Cr Cheques Receivable</p>
            <div className="space-y-3">
              <div>
                <label className="text-sm text-muted-foreground">Bank Account *</label>
                <select value={bankAccountId} onChange={e => setBankAccountId(e.target.value)} className="input w-full mt-1">
                  <option value="">Select bank account...</option>
                  {bankAccounts?.map(b => <option key={b.id} value={b.id}>{b.name} ({b.currencyCode}) — {b.currentBalance.toLocaleString()}</option>)}
                </select>
              </div>
              <div>
                <label className="text-sm text-muted-foreground">Deposit Date</label>
                <input type="date" value={depositDate} onChange={e => setDepositDate(e.target.value)} className="input w-full mt-1" />
              </div>
            </div>
            <div className="flex gap-2 mt-4">
              <button onClick={handleDeposit} disabled={!bankAccountId || deposit.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">
                {deposit.isPending ? "Processing..." : "Deposit to Bank"}
              </button>
              <button onClick={() => { setDepositModal(null); setBankAccountId(""); }} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
            </div>
          </div>
        </div>
      )}

      {/* Status filter */}
      <div className="flex gap-2 flex-wrap">
        {[undefined, 1, 2, 3, 4, 5].map(s => (
          <button key={String(s)} onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${statusFilter === s ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground hover:bg-muted/80"}`}>
            {s === undefined ? "All" : ChequeStatusLabels[s]}
          </button>
        ))}
      </div>

      {isLoading ? <div className="text-center py-10 text-muted-foreground">Loading cheques...</div> : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/30"><tr>
              <th className="text-left p-3 font-medium text-muted-foreground">Cheque #</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Customer</th>
              <th className="text-left p-3 font-medium text-muted-foreground">Bank</th>
              <th className="text-right p-3 font-medium text-muted-foreground">Amount</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Issue Date</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Due Date</th>
              <th className="text-center p-3 font-medium text-muted-foreground">Status</th>
              <th className="p-3 w-24"></th>
            </tr></thead>
            <tbody className="divide-y divide-border">
              {cheques?.map(c => (
                <tr key={c.id} className="hover:bg-muted/20 transition-colors">
                  <td className="p-3 font-mono text-xs">{c.chequeNumber}</td>
                  <td className="p-3 font-medium">{c.customerName}</td>
                  <td className="p-3 text-muted-foreground">{c.bankName}</td>
                  <td className="p-3 text-right tabular-nums font-medium">{c.amount.toLocaleString("en-EG", { style: "currency", currency: c.currencyCode || "EGP" })}</td>
                  <td className="p-3 text-center text-muted-foreground">{c.issueDate}</td>
                  <td className="p-3 text-center text-muted-foreground">{c.dueDate}</td>
                  <td className="p-3 text-center"><span className={`text-xs px-2 py-0.5 rounded-full ${STATUS_COLORS[c.status]}`}>{c.statusName}</span></td>
                  <td className="p-3">
                    {c.status === 1 && (
                      <button onClick={() => setDepositModal({ id: c.id, amount: c.amount, customer: c.customerName })}
                        className="text-amber-600 hover:text-amber-700 flex items-center gap-1 text-xs" title="Deposit to bank">
                        <ArrowDownToLine className="h-3.5 w-3.5" /> Deposit
                      </button>
                    )}
                    {c.status === 3 && <span className="text-xs text-emerald-600">✓ Cleared</span>}
                    {c.status === 4 && <span className="text-xs text-red-600">✗ Bounced</span>}
                  </td>
                </tr>
              ))}
              {!cheques?.length && <tr><td colSpan={8} className="p-8 text-center text-muted-foreground">No cheques found. Receive a cheque from a customer to get started.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
