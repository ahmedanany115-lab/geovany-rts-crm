// ── Lightweight i18n ──────────────────────────────────────────────────────────
// Single source of truth for all static UI strings.
// Usage:  const { t } = useT();   then   t("dashboard")

export type Lang = "en" | "ar";

export const translations = {
  // Navigation / Sidebar
  dashboard:          { en: "Dashboard",          ar: "لوحة التحكم" },
  crm:                { en: "CRM",                ar: "إدارة العملاء" },
  customers:          { en: "Customers",          ar: "العملاء" },
  contacts:           { en: "Contacts",           ar: "جهات الاتصال" },
  leads:              { en: "Leads",              ar: "العملاء المحتملون" },
  projects:           { en: "Projects",           ar: "المشاريع" },
  tasks:              { en: "Tasks",              ar: "المهام" },
  helpdesk:           { en: "Help Desk",          ar: "الدعم الفني" },
  reports:            { en: "Reports",            ar: "التقارير" },
  users:              { en: "Users",              ar: "المستخدمون" },
  settings:           { en: "Settings",           ar: "الإعدادات" },
  profile:            { en: "Profile",            ar: "الملف الشخصي" },
  logout:             { en: "Log out",            ar: "تسجيل الخروج" },

  // ERP Groups
  erp_overview:       { en: "ERP Overview",       ar: "نظرة عامة على ERP" },
  erp_dashboard:      { en: "ERP Dashboard",      ar: "لوحة ERP" },
  finance:            { en: "Finance",            ar: "المالية" },
  chart_of_accounts:  { en: "Chart of Accounts",  ar: "دليل الحسابات" },
  journal_entries:    { en: "Journal Entries",    ar: "القيود اليومية" },
  ledger:             { en: "Ledger",             ar: "دفتر الأستاذ" },
  trial_balance:      { en: "Trial Balance",      ar: "ميزان المراجعة" },
  fiscal_periods:     { en: "Fiscal Periods",     ar: "الفترات المحاسبية" },
  currencies:         { en: "Currencies",         ar: "العملات" },
  bank_accounts:      { en: "Bank Accounts",      ar: "الحسابات البنكية" },
  sales:              { en: "Sales",              ar: "المبيعات" },
  sales_orders:       { en: "Sales Orders",       ar: "أوامر البيع" },
  deliveries:         { en: "Deliveries",         ar: "التوصيلات" },
  customer_invoices:  { en: "Customer Invoices",  ar: "فواتير العملاء" },
  payments:           { en: "Payments",           ar: "المدفوعات" },
  cheques:            { en: "Cheques",            ar: "الشيكات" },
  purchasing:         { en: "Purchasing",         ar: "المشتريات" },
  suppliers:          { en: "Suppliers",          ar: "الموردون" },
  purchase_orders:    { en: "Purchase Orders",    ar: "أوامر الشراء" },
  supplier_invoices:  { en: "Supplier Invoices",  ar: "فواتير الموردين" },
  inventory:          { en: "Inventory",          ar: "المخزون" },
  products:           { en: "Products",           ar: "المنتجات" },
  warehouses:         { en: "Warehouses",         ar: "المستودعات" },
  stock_movements:    { en: "Stock & Movements",  ar: "المخزون والحركات" },

  // Common actions
  add:                { en: "Add",                ar: "إضافة" },
  save:               { en: "Save",               ar: "حفظ" },
  cancel:             { en: "Cancel",             ar: "إلغاء" },
  edit:               { en: "Edit",               ar: "تعديل" },
  delete:             { en: "Delete",             ar: "حذف" },
  search:             { en: "Search",             ar: "بحث" },
  filter:             { en: "Filter",             ar: "تصفية" },
  refresh:            { en: "Refresh",            ar: "تحديث" },
  loading:            { en: "Loading…",           ar: "جارٍ التحميل…" },
  no_data:            { en: "No data found.",     ar: "لا توجد بيانات." },
  active:             { en: "Active",             ar: "نشط" },
  inactive:           { en: "Inactive",           ar: "غير نشط" },
  status:             { en: "Status",             ar: "الحالة" },
  actions:            { en: "Actions",            ar: "إجراءات" },
  activate:           { en: "Activate",           ar: "تفعيل" },
  deactivate:         { en: "Deactivate",         ar: "إلغاء التفعيل" },
  saving:             { en: "Saving…",            ar: "جارٍ الحفظ…" },
  create:             { en: "Create",             ar: "إنشاء" },
  close:              { en: "Close",              ar: "إغلاق" },
  submit:             { en: "Submit",             ar: "إرسال" },
  approve:            { en: "Approve",            ar: "اعتماد" },
  post:               { en: "Post",               ar: "ترحيل" },
  view:               { en: "View",               ar: "عرض" },
  name:               { en: "Name",              ar: "الاسم" },
  code:               { en: "Code",              ar: "الكود" },
  email:              { en: "Email",             ar: "البريد الإلكتروني" },
  phone:              { en: "Phone",             ar: "الهاتف" },
  address:            { en: "Address",           ar: "العنوان" },
  notes:              { en: "Notes",             ar: "ملاحظات" },
  date:               { en: "Date",              ar: "التاريخ" },
  amount:             { en: "Amount",            ar: "المبلغ" },
  total:              { en: "Total",             ar: "الإجمالي" },
  balance:            { en: "Balance",           ar: "الرصيد" },
  description:        { en: "Description",       ar: "الوصف" },
  type:               { en: "Type",              ar: "النوع" },
  role:               { en: "Role",              ar: "الدور" },
  roles:              { en: "Roles",             ar: "الأدوار" },
  quantity:           { en: "Quantity",          ar: "الكمية" },
  price:              { en: "Price",             ar: "السعر" },
  tax:                { en: "Tax",               ar: "الضريبة" },
  currency:           { en: "Currency",          ar: "العملة" },
  period:             { en: "Period",            ar: "الفترة" },
  debit:              { en: "Debit",             ar: "مدين" },
  credit:             { en: "Credit",            ar: "دائن" },

  // Dashboard KPIs
  total_revenue:      { en: "Total Revenue",       ar: "إجمالي الإيرادات" },
  total_expenses:     { en: "Total Expenses",      ar: "إجمالي المصروفات" },
  outstanding_ar:     { en: "Outstanding AR",      ar: "الذمم المدينة" },
  outstanding_ap:     { en: "Outstanding AP",      ar: "الذمم الدائنة" },
  inventory_value:    { en: "Inventory Value",     ar: "قيمة المخزون" },
  low_stock:          { en: "Low Stock Items",     ar: "أصناف منخفضة المخزون" },
  open_cheques:       { en: "Open Cheques",        ar: "الشيكات المفتوحة" },
  commissions:        { en: "Commissions",         ar: "العمولات" },

  // Page-level
  coming_soon:        { en: "Coming in next sprint", ar: "قريباً في الإصدار القادم" },
  employees:          { en: "Employees",           ar: "الموظفون" },
  system_users:       { en: "System Users",        ar: "مستخدمو النظام" },
  sales_report:       { en: "Sales Report",        ar: "تقرير المبيعات" },
  revenue_report:     { en: "Revenue Report",      ar: "تقرير الإيرادات" },
  company_settings:   { en: "Company Settings",    ar: "إعدادات الشركة" },
  general_settings:   { en: "General Settings",    ar: "الإعدادات العامة" },
} as const;

export type TKey = keyof typeof translations;

export function t(key: TKey, lang: Lang): string {
  return translations[key]?.[lang] ?? key;
}
