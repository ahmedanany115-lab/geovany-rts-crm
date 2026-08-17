"use client";

import { RefreshCw, Landmark, TrendingUp, TrendingDown } from "lucide-react";
import { useBankAccounts, useBankTransactions } from "@/features/erp/hooks";

export default function BankAccountsPage() {
  const { data: accounts, isLoading, refetch } = useBankAccounts({ isActive: true });
  const { data: transactions } = useBankTransactions({});

  const totalBalance = accounts?.reduce((s, a) => s + a.currentBalance, 0) ?? 0;

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Landmark className="h-6 w-6 text-primary" />
          <h1 className="text-2xl font-semibold">Bank Accounts</h1>
        </div>
        <button onClick={() => refetch()} className="btn-ghost p-2 rounded-lg">
          <RefreshCw className="h-4 w-4" />
        </button>
      </div>

      {/* Total balance summary */}
      <div className="card p-6 bg-primary/5 border border-primary/20">
        <p className="text-sm text-muted-foreground">Total Bank Balance (EGP equivalent)</p>
        <p className="text-3xl font-bold mt-1">{totalBalance.toLocaleString("en-EG", { style: "currency", currency: "EGP" })}</p>
      </div>

      {/* Bank account cards */}
      {isLoading ? (
        <div className="text-center py-10 text-muted-foreground">Loading bank accounts...</div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {accounts?.map(a => (
            <div key={a.id} className="card p-5 space-y-3">
              <div className="flex items-start justify-between">
                <div>
                  <p className="font-semibold">{a.name}</p>
                  {a.bankName && <p className="text-xs text-muted-foreground">{a.bankName}</p>}
                </div>
                <span className="text-xs bg-muted px-2 py-0.5 rounded-full">{a.currencyCode}</span>
              </div>
              {a.accountNumber && (
                <p className="font-mono text-sm text-muted-foreground">••• {a.accountNumber.slice(-4)}</p>
              )}
              <div className="border-t border-border pt-3">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">Opening Balance</span>
                  <span className="tabular-nums">{a.openingBalance.toLocaleString()}</span>
                </div>
                <div className="flex justify-between text-sm mt-1">
                  <span className="font-medium">Current Balance</span>
                  <span className={`tabular-nums font-bold ${a.currentBalance >= 0 ? "text-emerald-600" : "text-red-600"}`}>
                    {a.currentBalance.toLocaleString()}
                  </span>
                </div>
                <div className="flex justify-between text-xs mt-1 text-muted-foreground">
                  <span>Movement</span>
                  <span className={`tabular-nums flex items-center gap-1 ${
                    a.currentBalance >= a.openingBalance ? "text-emerald-600" : "text-red-600"
                  }`}>
                    {a.currentBalance >= a.openingBalance ? <TrendingUp className="h-3 w-3" /> : <TrendingDown className="h-3 w-3" />}
                    {(a.currentBalance - a.openingBalance).toLocaleString()}
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Recent transactions */}
      {transactions && transactions.length > 0 && (
        <div className="space-y-3">
          <h2 className="font-semibold">Recent Transactions</h2>
          <div className="card overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/30">
                <tr>
                  <th className="text-left p-3 font-medium text-muted-foreground">Ref</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Account</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Type</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Date</th>
                  <th className="text-right p-3 font-medium text-muted-foreground">Amount</th>
                  <th className="text-left p-3 font-medium text-muted-foreground">Description</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {transactions.slice(0, 15).map(t => (
                  <tr key={t.id} className="hover:bg-muted/20">
                    <td className="p-3 font-mono text-xs">{t.transactionNumber}</td>
                    <td className="p-3">{t.bankAccountName}</td>
                    <td className="p-3">
                      <span className="text-xs bg-muted px-2 py-0.5 rounded">{t.transactionTypeName}</span>
                    </td>
                    <td className="p-3 text-muted-foreground">{t.transactionDate}</td>
                    <td className="p-3 text-right tabular-nums font-medium">
                      {t.amount.toLocaleString("en-EG", { style: "currency", currency: t.currencyCode || "EGP" })}
                    </td>
                    <td className="p-3 text-muted-foreground text-xs">{t.description ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
