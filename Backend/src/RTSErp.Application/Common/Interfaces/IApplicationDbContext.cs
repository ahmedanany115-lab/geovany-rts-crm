using Microsoft.EntityFrameworkCore;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Domain.Entities.Operational;

namespace RTSErp.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Employee> Employees { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    // Accounting (Phase 1)
    DbSet<Currency> Currencies { get; }
    DbSet<Account> Accounts { get; }
    DbSet<FiscalPeriod> FiscalPeriods { get; }
    DbSet<JournalEntry> JournalEntries { get; }
    DbSet<JournalEntryLine> JournalEntryLines { get; }
    DbSet<TaxRate> TaxRates { get; }
    DbSet<CommissionRate> CommissionRates { get; }
    DbSet<BusinessPartner> BusinessPartners { get; }
    DbSet<BankAccount> BankAccounts { get; }

    // Operational (Phase 2)
    DbSet<Product> Products { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<InventoryBalance> InventoryBalances { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }
    DbSet<PurchaseReceipt> PurchaseReceipts { get; }
    DbSet<PurchaseReceiptLine> PurchaseReceiptLines { get; }
    DbSet<SupplierInvoice> SupplierInvoices { get; }
    DbSet<SupplierInvoiceLine> SupplierInvoiceLines { get; }
    DbSet<SalesOrder> SalesOrders { get; }
    DbSet<SalesOrderLine> SalesOrderLines { get; }
    DbSet<SalesDelivery> SalesDeliveries { get; }
    DbSet<SalesDeliveryLine> SalesDeliveryLines { get; }
    DbSet<CustomerInvoice> CustomerInvoices { get; }
    DbSet<CustomerInvoiceLine> CustomerInvoiceLines { get; }
    DbSet<CustomerPayment> CustomerPayments { get; }
    DbSet<SupplierPayment> SupplierPayments { get; }
    DbSet<BankTransaction> BankTransactions { get; }
    DbSet<Cheque> Cheques { get; }
    DbSet<SalesCommission> SalesCommissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
