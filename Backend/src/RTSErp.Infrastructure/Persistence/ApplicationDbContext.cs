using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Domain.Entities.Operational;
using RTSErp.Infrastructure.Persistence.Configurations.Accounting;
using RTSErp.Infrastructure.Persistence.Configurations.Operational;

namespace RTSErp.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // ── Identity ──────────────────────────────────────────────────────────────
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // IApplicationDbContext.Users/.Roles are satisfied by IdentityDbContext's own Users/Roles DbSets.
    DbSet<ApplicationUser> IApplicationDbContext.Users => Users;
    DbSet<ApplicationRole> IApplicationDbContext.Roles => Roles;

    // ── Accounting (Phase 1) ──────────────────────────────────────────────────
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<CommissionRate> CommissionRates => Set<CommissionRate>();
    public DbSet<BusinessPartner> BusinessPartners => Set<BusinessPartner>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

    // ── Operational (Phase 2) ─────────────────────────────────────────────────
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptLine> PurchaseReceiptLines => Set<PurchaseReceiptLine>();
    public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();
    public DbSet<SupplierInvoiceLine> SupplierInvoiceLines => Set<SupplierInvoiceLine>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<SalesDelivery> SalesDeliveries => Set<SalesDelivery>();
    public DbSet<SalesDeliveryLine> SalesDeliveryLines => Set<SalesDeliveryLine>();
    public DbSet<CustomerInvoice> CustomerInvoices => Set<CustomerInvoice>();
    public DbSet<CustomerInvoiceLine> CustomerInvoiceLines => Set<CustomerInvoiceLine>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<Cheque> Cheques => Set<Cheque>();
    public DbSet<SalesCommission> SalesCommissions => Set<SalesCommission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Phase 1 accounting configurations
        builder.ApplyConfiguration(new CurrencyConfiguration());
        builder.ApplyConfiguration(new AccountConfiguration());
        builder.ApplyConfiguration(new FiscalPeriodConfiguration());
        builder.ApplyConfiguration(new JournalEntryConfiguration());
        builder.ApplyConfiguration(new JournalEntryLineConfiguration());
        builder.ApplyConfiguration(new TaxRateConfiguration());
        builder.ApplyConfiguration(new CommissionRateConfiguration());
        builder.ApplyConfiguration(new BusinessPartnerConfiguration());
        builder.ApplyConfiguration(new BankAccountConfiguration());

        // Phase 2 operational configurations
        builder.ApplyConfiguration(new ProductConfiguration());
        builder.ApplyConfiguration(new WarehouseConfiguration());
        builder.ApplyConfiguration(new InventoryBalanceConfiguration());
        builder.ApplyConfiguration(new InventoryMovementConfiguration());
        builder.ApplyConfiguration(new PurchaseOrderConfiguration());
        builder.ApplyConfiguration(new PurchaseOrderLineConfiguration());
        builder.ApplyConfiguration(new PurchaseReceiptConfiguration());
        builder.ApplyConfiguration(new PurchaseReceiptLineConfiguration());
        builder.ApplyConfiguration(new SupplierInvoiceConfiguration());
        builder.ApplyConfiguration(new SupplierInvoiceLineConfiguration());
        builder.ApplyConfiguration(new SalesOrderConfiguration());
        builder.ApplyConfiguration(new SalesOrderLineConfiguration());
        builder.ApplyConfiguration(new SalesDeliveryConfiguration());
        builder.ApplyConfiguration(new SalesDeliveryLineConfiguration());
        builder.ApplyConfiguration(new CustomerInvoiceConfiguration());
        builder.ApplyConfiguration(new CustomerInvoiceLineConfiguration());
        builder.ApplyConfiguration(new CustomerPaymentConfiguration());
        builder.ApplyConfiguration(new SupplierPaymentConfiguration());
        builder.ApplyConfiguration(new BankTransactionConfiguration());
        builder.ApplyConfiguration(new ChequeConfiguration());
        builder.ApplyConfiguration(new SalesCommissionConfiguration());

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            entity.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);
        });

        builder.Entity<Permission>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();
        });

        builder.Entity<Employee>(entity =>
        {
            entity.HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(e => e.UserId);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rt => rt.ReplacedByToken)
                .WithMany()
                .HasForeignKey(rt => rt.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(rt => new { rt.UserId, rt.RevokedAt });
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasQueryFilter(u => !u.IsDeleted);
        });
    }
}
