// ── Business Partners ─────────────────────────────────────────────────────────

export interface BusinessPartnerDto {
  id: string;
  code: string;
  name: string;
  nameAr?: string;
  partnerType: number;
  partnerTypeName: string;
  isActive: boolean;
  taxNumber?: string;
  phone?: string;
  email?: string;
  address?: string;
  notes?: string;
  creditLimit?: number;
  currencyCode?: string;
  receivableAccountCode?: string;
  payableAccountCode?: string;
  createdAt: string;
}

export interface UpsertBusinessPartnerRequest {
  code: string;
  name: string;
  nameAr?: string;
  partnerType: number;
  taxNumber?: string;
  phone?: string;
  email?: string;
  address?: string;
  notes?: string;
  creditLimit?: number;
  currencyId?: string;
  receivableAccountId?: string;
  payableAccountId?: string;
}

// ── Products ──────────────────────────────────────────────────────────────────

export interface ProductDto {
  id: string;
  sku: string;
  name: string;
  description?: string;
  category?: string;
  unit: string;
  barcode?: string;
  purchasePrice: number;
  salesPrice: number;
  currencyCode: string;
  taxRateName?: string;
  taxRatePercent: number;
  isActive: boolean;
  minimumStock: number;
  totalQuantity: number;
  createdAt: string;
}

export interface ProductStockDto {
  productId: string;
  productName: string;
  warehouseId: string;
  warehouseName: string;
  quantity: number;
  reservedQuantity: number;
  availableQuantity: number;
  averageCost: number;
}

export interface UpsertProductRequest {
  sku: string;
  name: string;
  description?: string;
  category?: string;
  unit: string;
  barcode?: string;
  purchasePrice: number;
  salesPrice: number;
  currencyId: string;
  taxRateId?: string;
  minimumStock: number;
  inventoryAccountId?: string;
  cogsAccountId?: string;
  salesAccountId?: string;
  purchaseAccountId?: string;
}

// ── Warehouses ────────────────────────────────────────────────────────────────

export interface WarehouseDto {
  id: string;
  code: string;
  name: string;
  location?: string;
  notes?: string;
  isActive: boolean;
  productCount: number;
}

// ── Inventory ─────────────────────────────────────────────────────────────────

export interface InventoryMovementDto {
  id: string;
  productName: string;
  warehouseName: string;
  movementType: number;
  movementTypeName: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  movementDate: string;
  referenceNumber?: string;
  notes?: string;
}

// ── Purchase Orders ───────────────────────────────────────────────────────────

export interface PurchaseOrderLineDto {
  id: string;
  productId: string;
  productName: string;
  productSKU: string;
  quantity: number;
  receivedQuantity: number;
  pendingQuantity: number;
  unitPrice: number;
  discountPercent: number;
  taxRate: number;
  taxAmount: number;
  lineTotal: number;
  netAmount: number;
}

export interface PurchaseOrderDto {
  id: string;
  poNumber: string;
  supplierId: string;
  supplierName: string;
  orderDate: string;
  currencyCode: string;
  exchangeRate: number;
  warehouseName: string;
  status: number;
  statusName: string;
  subTotal: number;
  taxAmount: number;
  totalAmount: number;
  notes?: string;
  createdAt: string;
  lines: PurchaseOrderLineDto[];
}

export interface CreatePurchaseOrderRequest {
  supplierId: string;
  orderDate: string;
  currencyId: string;
  exchangeRate: number;
  warehouseId: string;
  notes?: string;
  lines: Array<{
    productId: string;
    quantity: number;
    unitPrice: number;
    discountPercent: number;
    taxRateOverride?: number;
  }>;
}

// ── Sales Orders ──────────────────────────────────────────────────────────────

export interface SalesOrderLineDto {
  id: string;
  productId: string;
  productName: string;
  productSKU: string;
  quantity: number;
  deliveredQuantity: number;
  pendingQuantity: number;
  unitPrice: number;
  discountPercent: number;
  taxRate: number;
  taxAmount: number;
  lineTotal: number;
  netAmount: number;
}

export interface SalesOrderDto {
  id: string;
  soNumber: string;
  customerId: string;
  customerName: string;
  orderDate: string;
  currencyCode: string;
  exchangeRate: number;
  warehouseName: string;
  salespersonName?: string;
  status: number;
  statusName: string;
  subTotal: number;
  taxAmount: number;
  totalAmount: number;
  notes?: string;
  createdAt: string;
  lines: SalesOrderLineDto[];
}

export interface CreateSalesOrderRequest {
  customerId: string;
  orderDate: string;
  currencyId: string;
  exchangeRate: number;
  warehouseId: string;
  salespersonId?: string;
  notes?: string;
  lines: Array<{
    productId: string;
    quantity: number;
    unitPrice: number;
    discountPercent: number;
    taxRateOverride?: number;
  }>;
}

// ── Customer Invoices ─────────────────────────────────────────────────────────

export interface CustomerInvoiceLineDto {
  id: string;
  productId: string;
  productName: string;
  description: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  taxRate: number;
  taxAmount: number;
  lineTotal: number;
  netAmount: number;
}

export interface CustomerInvoiceDto {
  id: string;
  invoiceNumber: string;
  customerId: string;
  customerName: string;
  invoiceDate: string;
  dueDate: string;
  currencyCode: string;
  exchangeRate: number;
  salespersonName?: string;
  status: number;
  statusName: string;
  subTotal: number;
  discountAmount: number;
  taxAmount: number;
  totalAmount: number;
  paidAmount: number;
  balanceDue: number;
  createdAt: string;
  lines: CustomerInvoiceLineDto[];
}

// ── Payments ──────────────────────────────────────────────────────────────────

export interface CustomerPaymentDto {
  id: string;
  paymentNumber: string;
  customerId: string;
  customerName: string;
  paymentDate: string;
  currencyCode: string;
  amount: number;
  paymentMethod: number;
  paymentMethodName: string;
  status: number;
  statusName: string;
  bankAccountName?: string;
  notes?: string;
  createdAt: string;
}

export interface SupplierPaymentDto {
  id: string;
  paymentNumber: string;
  supplierId: string;
  supplierName: string;
  paymentDate: string;
  currencyCode: string;
  amount: number;
  paymentMethod: number;
  paymentMethodName: string;
  status: number;
  statusName: string;
  bankAccountName?: string;
  notes?: string;
  createdAt: string;
}

// ── Cheques ───────────────────────────────────────────────────────────────────

export interface ChequeDto {
  id: string;
  chequeNumber: string;
  customerName: string;
  bankName: string;
  currencyCode: string;
  amount: number;
  issueDate: string;
  dueDate: string;
  receivedDate: string;
  status: number;
  statusName: string;
  notes?: string;
  createdAt: string;
}

// ── Bank Accounts ─────────────────────────────────────────────────────────────

export interface BankAccountDto {
  id: string;
  code: string;
  name: string;
  bankName?: string;
  accountNumber?: string;
  iban?: string;
  currencyCode: string;
  openingBalance: number;
  currentBalance: number;
  isActive: boolean;
}

export interface BankTransactionDto {
  id: string;
  transactionNumber: string;
  bankAccountName: string;
  transactionType: number;
  transactionTypeName: string;
  transactionDate: string;
  currencyCode: string;
  amount: number;
  description?: string;
  reference?: string;
  destinationBankAccountName?: string;
  createdAt: string;
}

// ── Dashboard ─────────────────────────────────────────────────────────────────

export interface BankBalanceDto {
  bankName: string;
  currency: string;
  balance: number;
}

export interface ErpDashboardKpiDto {
  totalSalesThisMonth: number;
  totalSalesThisYear: number;
  pendingSalesOrders: number;
  totalPurchasesThisMonth: number;
  pendingPurchaseOrders: number;
  totalReceivables: number;
  totalPayables: number;
  bankBalances: BankBalanceDto[];
  inventoryValue: number;
  lowStockProducts: number;
  outstandingCheques: number;
  outstandingChequeCount: number;
  pendingCommission: number;
}

// ── Enum Labels ───────────────────────────────────────────────────────────────

export const PurchaseOrderStatusLabels: Record<number, string> = {
  1: "Draft", 2: "Approved", 3: "Partially Received", 4: "Received", 5: "Cancelled"
};

export const SalesOrderStatusLabels: Record<number, string> = {
  1: "Draft", 2: "Approved", 3: "Partially Delivered", 4: "Delivered", 5: "Cancelled"
};

export const InvoiceStatusLabels: Record<number, string> = {
  1: "Draft", 2: "Posted", 3: "Partially Paid", 4: "Paid", 5: "Cancelled"
};

export const ChequeStatusLabels: Record<number, string> = {
  1: "Received", 2: "Deposited", 3: "Cleared", 4: "Bounced", 5: "Cancelled"
};

export const PaymentMethodLabels: Record<number, string> = {
  1: "Bank Transfer", 2: "Cheque", 3: "Cash"
};
