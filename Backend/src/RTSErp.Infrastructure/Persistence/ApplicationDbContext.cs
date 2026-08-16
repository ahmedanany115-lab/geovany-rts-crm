using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RTSErp.Application.Common.Interfaces;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Infrastructure.Persistence.Configurations.Accounting;

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

    // ── Accounting ────────────────────────────────────────────────────────────
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<CommissionRate> CommissionRates => Set<CommissionRate>();
    public DbSet<BusinessPartner> BusinessPartners => Set<BusinessPartner>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all accounting configurations from this assembly
        builder.ApplyConfiguration(new CurrencyConfiguration());
        builder.ApplyConfiguration(new AccountConfiguration());
        builder.ApplyConfiguration(new FiscalPeriodConfiguration());
        builder.ApplyConfiguration(new JournalEntryConfiguration());
        builder.ApplyConfiguration(new JournalEntryLineConfiguration());
        builder.ApplyConfiguration(new TaxRateConfiguration());
        builder.ApplyConfiguration(new CommissionRateConfiguration());
        builder.ApplyConfiguration(new BusinessPartnerConfiguration());
        builder.ApplyConfiguration(new BankAccountConfiguration());

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
