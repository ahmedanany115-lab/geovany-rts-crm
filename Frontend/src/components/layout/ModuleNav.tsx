"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import {
  Home, Calculator, Package, ShoppingBag, ShoppingCart, Wrench,
  BarChart3, Users, Settings, ChevronDown, BookOpen, Receipt,
  Landmark, CalendarDays, Coins, Warehouse, ArrowLeftRight,
  FileText, Truck, DollarSign, CreditCard, LifeBuoy, ClipboardList,
  HardDrive, HeartHandshake, KanbanSquare, Briefcase, FileCheck2,
  RotateCcw, UserCog
} from "lucide-react";
import { useT } from "@/hooks/useT";
import { useRoles } from "@/hooks/useRoles";
import { cn } from "@/lib/utils";

type Item = { href: string; label: string; icon: any };
type Group = { key: string; label: string; icon: any; items: Item[]; show?: boolean };

export function ModuleNav() {
  const pathname = usePathname();
  const { lang } = useT();
  const { isFinance, isAdmin, hasRole } = useRoles();
  const [open, setOpen] = useState<string | null>(null);
  const ar = lang === "ar";
  const showSales = hasRole("Admin", "Manager", "SalesManager", "Sales", "Accountant", "Delivery");
  const showPurchasing = hasRole("Admin", "Manager", "Accountant", "Purchasing");
  const showStock = hasRole("Admin", "Manager", "SalesManager", "Sales", "Accountant", "Purchasing", "Delivery");
  const showServices = hasRole("Admin", "Manager", "SupportAgent", "Accountant", "SalesManager", "Sales", "Marketing", "ReadOnly");
  const showReports = hasRole("Admin", "Manager", "Accountant", "SalesManager", "Sales", "Purchasing", "ReadOnly");

  const groups: Group[] = [
    { key:"home", label:ar?"الرئيسية":"Home", icon:Home, items:[
      {href:"/erp/dashboard",label:ar?"لوحة التحكم":"Dashboard",icon:Home},
      {href:"/projects",label:ar?"المشروعات":"Projects",icon:Briefcase},
      {href:"/tasks/board",label:ar?"المهام":"Tasks",icon:KanbanSquare},
    ]},
    { key:"finance", label:ar?"الحسابات":"Financials", icon:Calculator, show:isFinance, items:[
      {href:"/finance/accounts",label:ar?"دليل الحسابات":"Chart of Accounts",icon:BookOpen},
      {href:"/finance/journal-entries",label:ar?"قيود اليومية":"Journal Entries",icon:FileText},
      {href:"/finance/ledger",label:ar?"دفتر الأستاذ":"Ledger",icon:Receipt},
      {href:"/finance/trial-balance",label:ar?"ميزان المراجعة":"Trial Balance",icon:BarChart3},
      {href:"/finance/income-statement",label:ar?"قائمة الدخل":"Income Statement",icon:BarChart3},
      {href:"/finance/profit-loss",label:ar?"الأرباح والخسائر":"Profit & Loss",icon:BarChart3},
      {href:"/finance/balance-sheet",label:ar?"الميزانية العمومية":"Balance Sheet",icon:BarChart3},
      {href:"/finance/cash-journal",label:ar?"يومية النقدية":"Cash Journal",icon:BookOpen},
      {href:"/finance/returns",label:ar?"المرتجعات":"Returns",icon:RotateCcw},
      {href:"/erp/bank-accounts",label:ar?"الحسابات البنكية":"Bank Accounts",icon:Landmark},
      {href:"/finance/fiscal-periods",label:ar?"الفترات المالية":"Fiscal Periods",icon:CalendarDays},
      {href:"/finance/currencies",label:ar?"العملات":"Currencies",icon:Coins},
    ]},
    { key:"stock", label:ar?"المخزون":"Stock Control", icon:Package, show:showStock, items:[
      {href:"/erp/products",label:ar?"المنتجات":"Products",icon:Package},
      {href:"/erp/warehouses",label:ar?"المخازن":"Warehouses",icon:Warehouse},
      {href:"/erp/inventory",label:ar?"حركات المخزون":"Stock Movements",icon:ArrowLeftRight},
      {href:"/erp/goods-receipts",label:ar?"إيصالات الاستلام":"Goods Receipts",icon:Truck},
    ]},
    { key:"purchasing", label:ar?"المشتريات":"Purchasing", icon:ShoppingBag, show:showPurchasing, items:[
      {href:"/erp/suppliers",label:ar?"الموردون":"Suppliers",icon:Truck},
      {href:"/erp/purchase-orders",label:ar?"أوامر الشراء":"Purchase Orders",icon:ShoppingBag},
      {href:"/erp/supplier-invoices",label:ar?"فواتير الموردين":"Supplier Invoices",icon:FileText},
      {href:"/finance/returns",label:ar?"مرتجعات الموردين":"Supplier Returns",icon:RotateCcw},
    ]},
    { key:"sales", label:ar?"المبيعات":"Sales", icon:ShoppingCart, show:showSales, items:[
      {href:"/erp/customers",label:ar?"العملاء":"Customers",icon:Users},
      {href:"/crm/leads",label:ar?"العملاء المحتملون":"Leads",icon:Users},
      {href:"/quotations",label:ar?"عروض الأسعار":"Quotations",icon:FileCheck2},
      {href:"/erp/sales-orders",label:ar?"أوامر البيع":"Sales Orders",icon:ShoppingCart},
      {href:"/erp/deliveries",label:ar?"التسليمات":"Deliveries",icon:Truck},
      {href:"/erp/customer-invoices",label:ar?"فواتير العملاء":"Customer Invoices",icon:Receipt},
      ...(isFinance ? [
        {href:"/erp/payments",label:ar?"المدفوعات والتحصيلات":"Payments",icon:DollarSign},
        {href:"/erp/cheques",label:ar?"الشيكات":"Cheques",icon:CreditCard},
        {href:"/finance/returns",label:ar?"مرتجعات العملاء":"Customer Returns",icon:RotateCcw},
      ] : []),
    ]},
    { key:"services", label:ar?"الخدمات":"Services", icon:Wrench, show:showServices, items:[
      {href:"/helpdesk",label:ar?"الدعم الفني":"Helpdesk",icon:LifeBuoy},
      {href:"/erp/maintenance",label:ar?"الصيانة":"Maintenance",icon:Wrench},
      {href:"/erp/maintenance/contracts",label:ar?"عقود الصيانة":"Contracts",icon:ClipboardList},
      {href:"/erp/maintenance/visits",label:ar?"الزيارات":"Visits",icon:CalendarDays},
      {href:"/erp/maintenance/equipment",label:ar?"المعدات":"Equipment",icon:HardDrive},
      {href:"/hr/leaves",label:ar?"الإجازات":"Leaves",icon:HeartHandshake},
      {href:"/hr/meetings",label:ar?"الاجتماعات":"Meetings",icon:CalendarDays},
      {href:"/documents",label:ar?"الوثائق":"Documents",icon:FileText},
    ]},
    { key:"reporting", label:ar?"التقارير":"Reporting", icon:BarChart3, show:showReports, items:[
      {href:"/reports/sales",label:ar?"تقارير المبيعات":"Sales Reports",icon:BarChart3},
      {href:"/finance/trial-balance",label:ar?"ميزان المراجعة":"Trial Balance",icon:BarChart3},
      {href:"/finance/income-statement",label:ar?"قائمة الدخل":"Income Statement",icon:BarChart3},
      {href:"/finance/balance-sheet",label:ar?"الميزانية":"Balance Sheet",icon:BarChart3},
    ]},
    { key:"admin", label:ar?"الإدارة":"Administration", icon:Settings, show:isAdmin, items:[
      {href:"/users/employees",label:ar?"المستخدمون":"Users",icon:UserCog},
      {href:"/admin/user-activity",label:ar?"نشاط المستخدمين":"User Activity",icon:BarChart3},
      {href:"/settings/company",label:ar?"الإعدادات":"Settings",icon:Settings},
    ]},
  ].filter(g => g.show !== false);

  return (
    <nav className="border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/85 relative z-40">
      <div className="flex items-center gap-1 overflow-x-auto px-4 min-h-11">
        {groups.map(group => {
          const active = group.items.some(i => pathname === i.href || pathname?.startsWith(i.href + "/"));
          const Icon = group.icon;
          return (
            <div key={group.key} className="relative shrink-0"
              onMouseEnter={() => setOpen(group.key)}
              onMouseLeave={() => setOpen(v => v === group.key ? null : v)}>
              <button onClick={() => setOpen(v => v === group.key ? null : group.key)}
                className={cn("flex items-center gap-1.5 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                  active ? "text-primary bg-primary/5" : "text-muted-foreground hover:text-foreground hover:bg-muted/60")}>
                <Icon className="h-4 w-4" />
                <span>{group.label}</span>
                <ChevronDown className={cn("h-3.5 w-3.5 transition-transform", open === group.key && "rotate-180")} />
              </button>
              {open === group.key && (
                <div className={cn("absolute top-full mt-0.5 min-w-[250px] rounded-xl border bg-background p-2 shadow-xl", ar ? "right-0" : "left-0")}>
                  <div className="px-2 py-1.5 text-xs font-semibold uppercase tracking-wider text-muted-foreground/70">{group.label}</div>
                  <div className="grid gap-0.5">
                    {group.items.map(item => {
                      const ItemIcon = item.icon;
                      const itemActive = pathname === item.href || pathname?.startsWith(item.href + "/");
                      return (
                        <Link key={item.href + item.label} href={item.href} onClick={() => setOpen(null)}
                          className={cn("flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm transition-colors",
                            itemActive ? "bg-accent font-semibold text-accent-foreground" : "hover:bg-muted text-foreground")}>
                          <ItemIcon className="h-4 w-4 text-muted-foreground" />
                          <span>{item.label}</span>
                        </Link>
                      );
                    })}
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </nav>
  );
}
