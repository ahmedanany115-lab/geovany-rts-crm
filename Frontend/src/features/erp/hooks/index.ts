"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  bankAccountsApi,
  chequesApi,
  customerInvoicesApi,
  customersApi,
  erpDashboardApi,
  inventoryApi,
  paymentsApi,
  productsApi,
  purchaseOrdersApi,
  salesOrdersApi,
  suppliersApi,
  warehousesApi,
} from "../api/erpApi";

// ── ERP Dashboard ─────────────────────────────────────────────────────────────

export const useErpDashboardKpis = () =>
  useQuery({ queryKey: ["erp-kpis"], queryFn: erpDashboardApi.kpis });

// ── Customers ─────────────────────────────────────────────────────────────────

export const useCustomers = (p?: Parameters<typeof customersApi.list>[0]) =>
  useQuery({ queryKey: ["customers", p], queryFn: () => customersApi.list(p) });

export const useCreateCustomer = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: customersApi.create, onSuccess: () => qc.invalidateQueries({ queryKey: ["customers"] }) });
};

export const useUpdateCustomer = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Parameters<typeof customersApi.update>[1] }) => customersApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["customers"] }),
  });
};

export const useToggleCustomerStatus = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: customersApi.toggleStatus, onSuccess: () => qc.invalidateQueries({ queryKey: ["customers"] }) });
};

// ── Suppliers ─────────────────────────────────────────────────────────────────

export const useSuppliers = (p?: Parameters<typeof suppliersApi.list>[0]) =>
  useQuery({ queryKey: ["suppliers", p], queryFn: () => suppliersApi.list(p) });

export const useCreateSupplier = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: suppliersApi.create, onSuccess: () => qc.invalidateQueries({ queryKey: ["suppliers"] }) });
};

export const useToggleSupplierStatus = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: suppliersApi.toggleStatus, onSuccess: () => qc.invalidateQueries({ queryKey: ["suppliers"] }) });
};

// ── Products ──────────────────────────────────────────────────────────────────

export const useProducts = (p?: Parameters<typeof productsApi.list>[0]) =>
  useQuery({ queryKey: ["products", p], queryFn: () => productsApi.list(p) });

export const useProduct = (id: string) =>
  useQuery({ queryKey: ["products", id], queryFn: () => productsApi.get(id), enabled: !!id });

export const useProductStock = (p?: Parameters<typeof productsApi.allStock>[0]) =>
  useQuery({ queryKey: ["product-stock", p], queryFn: () => productsApi.allStock(p) });

export const useCreateProduct = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: productsApi.create, onSuccess: () => qc.invalidateQueries({ queryKey: ["products"] }) });
};

export const useToggleProductStatus = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: productsApi.toggleStatus, onSuccess: () => qc.invalidateQueries({ queryKey: ["products"] }) });
};

// ── Warehouses ────────────────────────────────────────────────────────────────

export const useWarehouses = (p?: Parameters<typeof warehousesApi.list>[0]) =>
  useQuery({ queryKey: ["warehouses", p], queryFn: () => warehousesApi.list(p) });

export const useCreateWarehouse = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: warehousesApi.create, onSuccess: () => qc.invalidateQueries({ queryKey: ["warehouses"] }) });
};

export const useToggleWarehouseStatus = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: warehousesApi.toggleStatus, onSuccess: () => qc.invalidateQueries({ queryKey: ["warehouses"] }) });
};

// ── Inventory ─────────────────────────────────────────────────────────────────

export const useInventoryMovements = (p?: Parameters<typeof inventoryApi.movements>[0]) =>
  useQuery({ queryKey: ["inventory-movements", p], queryFn: () => inventoryApi.movements(p) });

export const useAdjustInventory = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: inventoryApi.adjust, onSuccess: () => { qc.invalidateQueries({ queryKey: ["inventory-movements"] }); qc.invalidateQueries({ queryKey: ["product-stock"] }); } });
};

// ── Purchase Orders ───────────────────────────────────────────────────────────

export const usePurchaseOrders = (p?: Parameters<typeof purchaseOrdersApi.list>[0]) =>
  useQuery({ queryKey: ["purchase-orders", p], queryFn: () => purchaseOrdersApi.list(p) });

export const usePurchaseOrder = (id: string) =>
  useQuery({ queryKey: ["purchase-orders", id], queryFn: () => purchaseOrdersApi.get(id), enabled: !!id });

export const useCreatePurchaseOrder = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: purchaseOrdersApi.create, onSuccess: () => qc.invalidateQueries({ queryKey: ["purchase-orders"] }) });
};

export const useApprovePurchaseOrder = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: purchaseOrdersApi.approve, onSuccess: () => qc.invalidateQueries({ queryKey: ["purchase-orders"] }) });
};

// ── Sales Orders ──────────────────────────────────────────────────────────────

export const useSalesOrders = (p?: Parameters<typeof salesOrdersApi.list>[0]) =>
  useQuery({ queryKey: ["sales-orders", p], queryFn: () => salesOrdersApi.list(p) });

export const useSalesOrder = (id: string) =>
  useQuery({ queryKey: ["sales-orders", id], queryFn: () => salesOrdersApi.get(id), enabled: !!id });

export const useCreateSalesOrder = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: salesOrdersApi.create, onSuccess: () => qc.invalidateQueries({ queryKey: ["sales-orders"] }) });
};

export const useApproveSalesOrder = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: salesOrdersApi.approve, onSuccess: () => qc.invalidateQueries({ queryKey: ["sales-orders"] }) });
};

// ── Customer Invoices ─────────────────────────────────────────────────────────

export const useCustomerInvoices = (p?: Parameters<typeof customerInvoicesApi.list>[0]) =>
  useQuery({ queryKey: ["customer-invoices", p], queryFn: () => customerInvoicesApi.list(p) });

export const useCustomerInvoice = (id: string) =>
  useQuery({ queryKey: ["customer-invoices", id], queryFn: () => customerInvoicesApi.get(id), enabled: !!id });

export const useCreateCustomerInvoice = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: customerInvoicesApi.create, onSuccess: () => qc.invalidateQueries({ queryKey: ["customer-invoices"] }) });
};

export const usePostCustomerInvoice = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: customerInvoicesApi.post, onSuccess: () => qc.invalidateQueries({ queryKey: ["customer-invoices"] }) });
};

// ── Payments ──────────────────────────────────────────────────────────────────

export const useCustomerPayments = (p?: Parameters<typeof paymentsApi.listCustomer>[0]) =>
  useQuery({ queryKey: ["customer-payments", p], queryFn: () => paymentsApi.listCustomer(p) });

export const useCreateCustomerPayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: paymentsApi.createCustomer,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["customer-payments"] }); qc.invalidateQueries({ queryKey: ["customer-invoices"] }); }
  });
};

export const useSupplierPayments = (p?: Parameters<typeof paymentsApi.listSupplier>[0]) =>
  useQuery({ queryKey: ["supplier-payments", p], queryFn: () => paymentsApi.listSupplier(p) });

export const useCreateSupplierPayment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: paymentsApi.createSupplier,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["supplier-payments"] }); qc.invalidateQueries({ queryKey: ["bank-accounts"] }); }
  });
};

// ── Cheques ───────────────────────────────────────────────────────────────────

export const useCheques = (p?: Parameters<typeof chequesApi.list>[0]) =>
  useQuery({ queryKey: ["cheques", p], queryFn: () => chequesApi.list(p) });

export const useReceiveCheque = () => {
  const qc = useQueryClient();
  return useMutation({ mutationFn: chequesApi.receive, onSuccess: () => qc.invalidateQueries({ queryKey: ["cheques"] }) });
};

export const useDepositCheque = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Parameters<typeof chequesApi.deposit>[1] }) => chequesApi.deposit(id, data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["cheques"] }); qc.invalidateQueries({ queryKey: ["bank-accounts"] }); }
  });
};

// ── Bank Accounts ─────────────────────────────────────────────────────────────

export const useBankAccounts = (p?: Parameters<typeof bankAccountsApi.list>[0]) =>
  useQuery({ queryKey: ["bank-accounts", p], queryFn: () => bankAccountsApi.list(p) });

export const useBankTransactions = (p?: Parameters<typeof bankAccountsApi.transactions>[0]) =>
  useQuery({ queryKey: ["bank-transactions", p], queryFn: () => bankAccountsApi.transactions(p) });

export const useCreateBankTransaction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: bankAccountsApi.createTransaction,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["bank-transactions"] }); qc.invalidateQueries({ queryKey: ["bank-accounts"] }); }
  });
};

// ── Currency helpers ──────────────────────────────────────────────────────────
// Re-export finance currencies so ERP forms can use them without cross-feature imports
export { useCurrencies } from "@/features/finance/hooks";
