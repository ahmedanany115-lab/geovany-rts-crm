import { apiFetch } from "@/lib/api-client";
import type {
  BankAccountDto,
  BankTransactionDto,
  BusinessPartnerDto,
  ChequeDto,
  CustomerInvoiceDto,
  CustomerPaymentDto,
  ErpDashboardKpiDto,
  InventoryMovementDto,
  ProductDto,
  ProductStockDto,
  PurchaseOrderDto,
  SalesOrderDto,
  SupplierPaymentDto,
  UpsertBusinessPartnerRequest,
  UpsertProductRequest,
  WarehouseDto,
} from "../types";

const qs = (params: Record<string, unknown>) => {
  const p = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => { if (v !== undefined && v !== null) p.set(k, String(v)); });
  const s = p.toString();
  return s ? `?${s}` : "";
};

// ── Customers ─────────────────────────────────────────────────────────────────

export const customersApi = {
  list: (p?: { isActive?: boolean; search?: string }) =>
    apiFetch<BusinessPartnerDto[]>(`/customers${qs(p ?? {})}`),
  get: (id: string) => apiFetch<BusinessPartnerDto>(`/customers/${id}`),
  create: (data: UpsertBusinessPartnerRequest) =>
    apiFetch<{ id: string }>("/customers", { method: "POST", body: JSON.stringify(data) }),
  update: (id: string, data: UpsertBusinessPartnerRequest) =>
    apiFetch<void>(`/customers/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  toggleStatus: (id: string) => apiFetch<void>(`/customers/${id}/toggle-status`, { method: "PATCH" }),
};

// ── Suppliers ─────────────────────────────────────────────────────────────────

export const suppliersApi = {
  list: (p?: { isActive?: boolean; search?: string }) =>
    apiFetch<BusinessPartnerDto[]>(`/suppliers${qs(p ?? {})}`),
  get: (id: string) => apiFetch<BusinessPartnerDto>(`/suppliers/${id}`),
  create: (data: UpsertBusinessPartnerRequest) =>
    apiFetch<{ id: string }>("/suppliers", { method: "POST", body: JSON.stringify(data) }),
  update: (id: string, data: UpsertBusinessPartnerRequest) =>
    apiFetch<void>(`/suppliers/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  toggleStatus: (id: string) => apiFetch<void>(`/suppliers/${id}/toggle-status`, { method: "PATCH" }),
};

// ── Products ──────────────────────────────────────────────────────────────────

export const productsApi = {
  list: (p?: { search?: string; category?: string; isActive?: boolean }) =>
    apiFetch<ProductDto[]>(`/products${qs(p ?? {})}`),
  get: (id: string) => apiFetch<ProductDto>(`/products/${id}`),
  stock: (id: string) => apiFetch<ProductStockDto[]>(`/products/${id}/stock`),
  allStock: (p?: { warehouseId?: string }) => apiFetch<ProductStockDto[]>(`/products/stock${qs(p ?? {})}`),
  create: (data: UpsertProductRequest) =>
    apiFetch<{ id: string }>("/products", { method: "POST", body: JSON.stringify(data) }),
  update: (id: string, data: UpsertProductRequest) =>
    apiFetch<void>(`/products/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  toggleStatus: (id: string) => apiFetch<void>(`/products/${id}/toggle-status`, { method: "PATCH" }),
};

// ── Warehouses ────────────────────────────────────────────────────────────────

export const warehousesApi = {
  list: (p?: { isActive?: boolean }) => apiFetch<WarehouseDto[]>(`/warehouses${qs(p ?? {})}`),
  create: (data: { code: string; name: string; location?: string; notes?: string }) =>
    apiFetch<{ id: string }>("/warehouses", { method: "POST", body: JSON.stringify(data) }),
  update: (id: string, data: { code: string; name: string; location?: string; notes?: string }) =>
    apiFetch<void>(`/warehouses/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  toggleStatus: (id: string) => apiFetch<void>(`/warehouses/${id}/toggle-status`, { method: "PATCH" }),
};

// ── Inventory ─────────────────────────────────────────────────────────────────

export const inventoryApi = {
  movements: (p?: { productId?: string; warehouseId?: string; fromDate?: string; toDate?: string }) =>
    apiFetch<InventoryMovementDto[]>(`/inventory/movements${qs(p ?? {})}`),
  adjust: (data: { productId: string; warehouseId: string; quantity: number; unitCost: number; movementDate: string; notes?: string }) =>
    apiFetch<{ id: string }>("/inventory/adjust", { method: "POST", body: JSON.stringify(data) }),
  transfer: (data: { productId: string; fromWarehouseId: string; toWarehouseId: string; quantity: number; transferDate: string; notes?: string }) =>
    apiFetch<void>("/inventory/transfer", { method: "POST", body: JSON.stringify(data) }),
};

// ── Purchase Orders ───────────────────────────────────────────────────────────

export const purchaseOrdersApi = {
  list: (p?: { status?: number; supplierId?: string }) =>
    apiFetch<PurchaseOrderDto[]>(`/purchaseorders${qs(p ?? {})}`),
  get: (id: string) => apiFetch<PurchaseOrderDto>(`/purchaseorders/${id}`),
  create: (data: unknown) =>
    apiFetch<{ id: string }>("/purchaseorders", { method: "POST", body: JSON.stringify(data) }),
  approve: (id: string) => apiFetch<void>(`/purchaseorders/${id}/approve`, { method: "POST" }),
  createReceipt: (data: unknown) =>
    apiFetch<{ id: string }>("/purchasereceipts", { method: "POST", body: JSON.stringify(data) }),
  createInvoice: (data: unknown) =>
    apiFetch<{ id: string }>("/supplierinvoices", { method: "POST", body: JSON.stringify(data) }),
  postInvoice: (id: string) => apiFetch<void>(`/supplierinvoices/${id}/post`, { method: "POST" }),
};

// ── Sales Orders ──────────────────────────────────────────────────────────────

export const salesOrdersApi = {
  list: (p?: { status?: number; customerId?: string }) =>
    apiFetch<SalesOrderDto[]>(`/salesorders${qs(p ?? {})}`),
  get: (id: string) => apiFetch<SalesOrderDto>(`/salesorders/${id}`),
  create: (data: unknown) =>
    apiFetch<{ id: string }>("/salesorders", { method: "POST", body: JSON.stringify(data) }),
  approve: (id: string) => apiFetch<void>(`/salesorders/${id}/approve`, { method: "POST" }),
  createDelivery: (data: unknown) =>
    apiFetch<{ id: string }>("/salesdeliveries", { method: "POST", body: JSON.stringify(data) }),
};

// ── Customer Invoices ─────────────────────────────────────────────────────────

export const customerInvoicesApi = {
  list: (p?: { status?: number; customerId?: string }) =>
    apiFetch<CustomerInvoiceDto[]>(`/customerinvoices${qs(p ?? {})}`),
  get: (id: string) => apiFetch<CustomerInvoiceDto>(`/customerinvoices/${id}`),
  create: (data: unknown) =>
    apiFetch<{ id: string }>("/customerinvoices", { method: "POST", body: JSON.stringify(data) }),
  post: (id: string) => apiFetch<void>(`/customerinvoices/${id}/post`, { method: "POST" }),
};

// ── Payments ──────────────────────────────────────────────────────────────────

export const paymentsApi = {
  listCustomer: (p?: { customerId?: string; status?: number }) =>
    apiFetch<CustomerPaymentDto[]>(`/customerpayments${qs(p ?? {})}`),
  createCustomer: (data: unknown) =>
    apiFetch<{ id: string }>("/customerpayments", { method: "POST", body: JSON.stringify(data) }),
  listSupplier: (p?: { supplierId?: string }) =>
    apiFetch<SupplierPaymentDto[]>(`/supplierpayments${qs(p ?? {})}`),
  createSupplier: (data: unknown) =>
    apiFetch<{ id: string }>("/supplierpayments", { method: "POST", body: JSON.stringify(data) }),
};

// ── Cheques ───────────────────────────────────────────────────────────────────

export const chequesApi = {
  list: (p?: { status?: number; customerId?: string }) =>
    apiFetch<ChequeDto[]>(`/cheques${qs(p ?? {})}`),
  receive: (data: unknown) => apiFetch<{ id: string }>("/cheques", { method: "POST", body: JSON.stringify(data) }),
  deposit: (id: string, data: { bankAccountId: string; depositDate: string }) =>
    apiFetch<void>(`/cheques/${id}/deposit`, { method: "POST", body: JSON.stringify(data) }),
  bounce: (id: string, bounceDate: string) =>
    apiFetch<void>(`/cheques/${id}/bounce`, { method: "POST", body: JSON.stringify(bounceDate) }),
};

// ── Bank Accounts ─────────────────────────────────────────────────────────────

export const bankAccountsApi = {
  list: (p?: { isActive?: boolean }) => apiFetch<BankAccountDto[]>(`/bankaccounts${qs(p ?? {})}`),
  transactions: (p?: { bankAccountId?: string; fromDate?: string; toDate?: string }) =>
    apiFetch<BankTransactionDto[]>(`/banktransactions${qs(p ?? {})}`),
  createTransaction: (data: unknown) =>
    apiFetch<{ id: string }>("/banktransactions", { method: "POST", body: JSON.stringify(data) }),
};

// ── ERP Dashboard ─────────────────────────────────────────────────────────────

export const erpDashboardApi = {
  kpis: () => apiFetch<ErpDashboardKpiDto>("/erpdashboard/kpis"),
};
