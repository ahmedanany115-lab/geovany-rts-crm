"use client";
import { useState } from "react";
import { RefreshCw, Landmark, TrendingUp, TrendingDown, ArrowLeftRight, Plus, X } from "lucide-react";
import { useBankAccounts, useBankTransactions, useCreateBankTransaction } from "@/features/erp/hooks";

const TXN_TYPE_LABELS: Record<number, string> = { 1: "Deposit", 2: "Withdrawal", 3: "Transfer", 4: "Customer Receipt", 5: "Supplier Payment" };
const TXN_COLORS: Record<number, string> = { 1: "text-emerald-600", 2: "text-red-600", 3: "text-blue-600", 4: "text-emerald-600", 5: "text-red-600" };

export default function BankAccountsPage() {
  const [selectedAccountId, setSelectedAccountId] = useState<string | undefined>(undefined);
  const [showTxnForm, setShowTxnForm] = useState(false);
  const today = new Date().toISOString().split("T")[0];
  const [txnForm, setTxnForm] = useState({ bankAccountId: "", transactionType: "1", amount: "", description: "", reference: "", transactionDate: today, destinationBankAccountId: "", currencyId: "00000000-0000-0000-0000-000000000001", exchangeRate: "1" });

  const { data: accounts, isLoading, refetch } = useBankAccounts();
  const { data: transactions, refetch: refetchTxns } = useBankTransactions({ bankAccountId: selectedAccountId });
  const createTxn = useCreateBankTransaction();

  const totalEGP = accounts?.filter(a => a.currencyCode === "EGP").reduce((s, a) => s + a.currentBalance, 0) ?? 0;
  const totalUSD = accounts?.filter(a => a.currencyCode === "USD").reduce((s, a) => s + a.currentBalance, 0) ?? 0;

  const handleTxn = async (e: React.FormEvent) => {
    e.preventDefault();
    const selAcc = accounts?.find(a => a.id === txnForm.bankAccountId);
    await createTxn.mutateAsync({
      ...txnForm,
      transactionType: parseInt(txnForm.transactionType),
      amount: parseFloat(txnForm.amount),
      amountBase: parseFloat(txnForm.amount) * parseFloat(txnForm.exchangeRate),
      exchangeRate: parseFloat(txnForm.exchangeRate),
      destinationBankAccountId: txnForm.destinationBankAccountId || undefined,
      currencyId: selAcc ? "00000000-0000-0000-0000-000000000001" : txnForm.currencyId,
    });
    setShowTxnForm(false);
    setTxnForm({ bankAccountId: "", transactionType: "1", amount: "", description: "", reference: "", transactionDate: today, destinationBankAccountId: "", currencyId: "00000000-0000-0000-0000-000000000001", exchangeRate: "1" });
    refetch(); refetchTxns();
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3"><Landmark className="h-6 w-6 text-primary" /><h1 className="text-2xl font-semibold">Bank Accounts</h1></div>
        <div className="flex gap-2">
          <button onClick={() => { refetch(); refetchTxns(); }} className="btn-ghost p-2 rounded-lg"><RefreshCw className="h-4 w-4" /></button>
          <button onClick={() => setShowTxnForm(v => !v)} className="btn-primary flex items-center gap-2 px-4 py-2 rounded-lg text-sm">
            {showTxnForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {showTxnForm ? "Cancel" : "New Transaction"}
          </button>
        </div>
      </div>

      {/* Balance summary */}
      <div className="grid grid-cols-2 gap-4">
        <div className="card p-4 bg-emerald-500/5 border border-emerald-200 dark:border-emerald-800">
          <p className="text-sm text-muted-foreground">Total EGP Balance</p>
          <p className="text-2xl font-bold mt-1 text-emerald-700 dark:text-emerald-400">{totalEGP.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p>
        </div>
        <div className="card p-4 bg-blue-500/5 border border-blue-200 dark:border-blue-800">
          <p className="text-sm text-muted-foreground">Total USD Balance</p>
          <p className="text-2xl font-bold mt-1 text-blue-700 dark:text-blue-400">{totalUSD.toLocaleString("en-US", { style: "currency", currency: "USD" })}</p>
        </div>
      </div>

      {/* Transaction form */}
      {showTxnForm && (
        <form onSubmit={handleTxn} className="card p-5 space-y-4 border border-primary/20">
          <h2 className="font-semibold text-sm">New Bank Transaction</h2>
          <p className="text-xs text-muted-foreground">Deposit/Withdrawal/Transfer creates a balanced journal entry automatically.</p>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-xs text-muted-foreground block mb-1">Bank Account *</label>
              <select required value={txnForm.bankAccountId} onChange={e => setTxnForm(f => ({ ...f, bankAccountId: e.target.value }))} className="input w-full">
                <option value="">Select account...</option>
                {accounts?.map(a => <option key={a.id} value={a.id}>{a.name} ({a.currencyCode}) — Balance: {a.currentBalance.toLocaleString()}</option>)}
              </select></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Transaction Type *</label>
              <select required value={txnForm.transactionType} onChange={e => setTxnForm(f => ({ ...f, transactionType: e.target.value }))} className="input w-full">
                <option value="1">Deposit (Dr Bank / Cr Cash)</option>
                <option value="2">Withdrawal (Dr Cash / Cr Bank)</option>
                <option value="3">Transfer (Dr Dest / Cr Source)</option>
              </select></div>
            {txnForm.transactionType === "3" && (
              <div className="col-span-2"><label className="text-xs text-muted-foreground block mb-1">Destination Account *</label>
                <select required value={txnForm.destinationBankAccountId} onChange={e => setTxnForm(f => ({ ...f, destinationBankAccountId: e.target.value }))} className="input w-full">
                  <option value="">Select destination...</option>
                  {accounts?.filter(a => a.id !== txnForm.bankAccountId).map(a => <option key={a.id} value={a.id}>{a.name} ({a.currencyCode})</option>)}
                </select></div>
            )}
            <div><label className="text-xs text-muted-foreground block mb-1">Amount *</label>
              <input type="number" required min="0.01" step="0.01" value={txnForm.amount} onChange={e => setTxnForm(f => ({ ...f, amount: e.target.value }))} className="input w-full" placeholder="0.00" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Date</label>
              <input type="date" value={txnForm.transactionDate} onChange={e => setTxnForm(f => ({ ...f, transactionDate: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Description</label>
              <input value={txnForm.description} onChange={e => setTxnForm(f => ({ ...f, description: e.target.value }))} className="input w-full" /></div>
            <div><label className="text-xs text-muted-foreground block mb-1">Reference</label>
              <input value={txnForm.reference} onChange={e => setTxnForm(f => ({ ...f, reference: e.target.value }))} className="input w-full" /></div>
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={createTxn.isPending} className="btn-primary px-4 py-2 rounded-lg text-sm">{createTxn.isPending ? "Processing..." : "Post Transaction"}</button>
            <button type="button" onClick={() => setShowTxnForm(false)} className="btn-ghost px-4 py-2 rounded-lg text-sm">Cancel</button>
          </div>
          {createTxn.isError && <p className="text-sm text-red-600">Error processing transaction.</p>}
          {createTxn.isSuccess && <p className="text-sm text-emerald-600">✓ Transaction posted with journal entry.</p>}
        </form>
      )}

      {/* Account cards */}
      {isLoading ? <div className="text-center py-10 text-muted-foreground">Loading accounts...</div> : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {accounts?.map(a => (
            <button key={a.id} onClick={() => setSelectedAccountId(selectedAccountId === a.id ? undefined : a.id)}
              className={`card p-4 text-left transition-all ${selectedAccountId === a.id ? "border-primary ring-1 ring-primary" : "hover:border-primary/40"}`}>
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <Landmark className="h-5 w-5 text-muted-foreground" />
                  <span className="font-mono text-xs text-muted-foreground">{a.code}</span>
                </div>
                <span className="text-xs px-2 py-0.5 rounded-full bg-muted text-muted-foreground">{a.currencyCode}</span>
              </div>
              <h3 className="font-semibold">{a.name}</h3>
              {a.bankName && <p className="text-xs text-muted-foreground mt-0.5">{a.bankName}</p>}
              {a.accountNumber && <p className="text-xs text-muted-foreground font-mono">{a.accountNumber}</p>}
              <div className="mt-3 pt-3 border-t border-border">
                <p className="text-xs text-muted-foreground">Current Balance</p>
                <p className={`text-xl font-bold tabular-nums ${a.currentBalance >= 0 ? "text-emerald-600" : "text-red-600"}`}>
                  {a.currentBalance.toLocaleString(a.currencyCode === "USD" ? "en-US" : "en-EG", { style: "currency", currency: a.currencyCode })}
                </p>
              </div>
              {selectedAccountId === a.id && <p className="text-xs text-primary mt-2">▼ Showing transactions below</p>}
            </button>
          ))}
          {!accounts?.length && <div className="col-span-3 p-8 text-center text-muted-foreground card">No bank accounts configured.</div>}
        </div>
      )}

      {/* Transactions for selected account */}
      {selectedAccountId && (
        <div className="space-y-3">
          <h2 className="font-semibold flex items-center gap-2"><ArrowLeftRight className="h-4 w-4" /> Transaction History — {accounts?.find(a => a.id === selectedAccountId)?.name}</h2>
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30"><tr>
                <th className="text-left p-3 font-medium text-muted-foreground">Ref #</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Type</th>
                <th className="text-left p-3 font-medium text-muted-foreground">Description</th>
                <th className="text-right p-3 font-medium text-muted-foreground">Amount</th>
              </tr></thead>
              <tbody className="divide-y divide-border">
                {transactions?.map(t => (
                  <tr key={t.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{t.transactionNumber}</td>
                    <td className="p-3 text-muted-foreground">{t.transactionDate}</td>
                    <td className={`p-3 font-medium ${TXN_COLORS[t.transactionType] || ""}`}>{TXN_TYPE_LABELS[t.transactionType] || t.transactionTypeName}</td>
                    <td className="p-3 text-muted-foreground">{t.description ?? "—"}{t.destinationBankAccountName ? ` → ${t.destinationBankAccountName}` : ""}</td>
                    <td className={`p-3 text-right tabular-nums font-medium ${TXN_COLORS[t.transactionType] || ""}`}>
                      {[1, 4].includes(t.transactionType) ? "+" : "−"}{t.amount.toLocaleString("en-EG", { style: "currency", currency: t.currencyCode || "EGP" })}
                    </td>
                  </tr>
                ))}
                {!transactions?.length && <tr><td colSpan={5} className="p-6 text-center text-muted-foreground">No transactions found for this account.</td></tr>}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
