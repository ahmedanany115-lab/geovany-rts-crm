using Microsoft.EntityFrameworkCore;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Employee> Employees { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    // Accounting
    DbSet<Currency> Currencies { get; }
    DbSet<Account> Accounts { get; }
    DbSet<FiscalPeriod> FiscalPeriods { get; }
    DbSet<JournalEntry> JournalEntries { get; }
    DbSet<JournalEntryLine> JournalEntryLines { get; }
    DbSet<TaxRate> TaxRates { get; }
    DbSet<CommissionRate> CommissionRates { get; }
    DbSet<BusinessPartner> BusinessPartners { get; }
    DbSet<BankAccount> BankAccounts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
