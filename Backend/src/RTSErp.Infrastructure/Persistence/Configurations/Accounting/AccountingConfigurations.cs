using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Infrastructure.Persistence.Configurations.Accounting;

internal class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Code).HasMaxLength(10).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Symbol).HasMaxLength(10).IsRequired();
        builder.Property(c => c.ExchangeRate).HasPrecision(18, 6);
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

internal class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.Code).IsUnique()
               .HasFilter("\"IsDeleted\" = false");

        builder.Property(a => a.Code).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.NameAr).HasMaxLength(200);
        builder.Property(a => a.AccountType).HasConversion<int>();

        builder.HasOne(a => a.Parent)
               .WithMany(a => a.Children)
               .HasForeignKey(a => a.ParentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Currency)
               .WithMany(c => c.Accounts)
               .HasForeignKey(a => a.CurrencyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

internal class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> builder)
    {
        builder.ToTable("FiscalPeriods");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Status).HasConversion<int>();
        builder.HasIndex(p => new { p.StartDate, p.EndDate });
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

internal class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.EntryNumber).IsUnique()
               .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(e => e.EntryDate);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.ReferenceType, e.ReferenceId });

        builder.Property(e => e.EntryNumber).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.ReferenceNumber).HasMaxLength(100);
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.ReferenceType).HasConversion<int>();
        builder.Property(e => e.ExchangeRate).HasPrecision(18, 6);

        builder.HasOne(e => e.Currency)
               .WithMany(c => c.JournalEntries)
               .HasForeignKey(e => e.CurrencyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.FiscalPeriod)
               .WithMany(p => p.JournalEntries)
               .HasForeignKey(e => e.FiscalPeriodId)
               .OnDelete(DeleteBehavior.Restrict);

        // Self-referential reversal
        builder.HasOne(e => e.ReversedByEntry)
               .WithMany()
               .HasForeignKey(e => e.ReversedByEntryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReversesEntry)
               .WithMany()
               .HasForeignKey(e => e.ReversesEntryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

internal class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("JournalEntryLines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.JournalEntryId);
        builder.HasIndex(l => l.AccountId);

        builder.Property(l => l.Debit).HasPrecision(18, 4);
        builder.Property(l => l.Credit).HasPrecision(18, 4);
        builder.Property(l => l.DebitBase).HasPrecision(18, 4);
        builder.Property(l => l.CreditBase).HasPrecision(18, 4);
        builder.Property(l => l.ExchangeRate).HasPrecision(18, 6);
        builder.Property(l => l.Description).HasMaxLength(300);

        builder.HasOne(l => l.JournalEntry)
               .WithMany(e => e.Lines)
               .HasForeignKey(l => l.JournalEntryId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Account)
               .WithMany(a => a.JournalEntryLines)
               .HasForeignKey(l => l.AccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Currency)
               .WithMany(c => c.JournalEntryLines)
               .HasForeignKey(l => l.CurrencyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}

internal class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        builder.ToTable("TaxRates");
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Property(t => t.Code).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Rate).HasPrecision(8, 4);

        builder.HasOne(t => t.InputTaxAccount)
               .WithMany()
               .HasForeignKey(t => t.InputTaxAccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.OutputTaxAccount)
               .WithMany()
               .HasForeignKey(t => t.OutputTaxAccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

internal class CommissionRateConfiguration : IEntityTypeConfiguration<CommissionRate>
{
    public void Configure(EntityTypeBuilder<CommissionRate> builder)
    {
        builder.ToTable("CommissionRates");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Rate).HasPrecision(8, 4);

        builder.HasOne(c => c.CommissionExpenseAccount)
               .WithMany()
               .HasForeignKey(c => c.CommissionExpenseAccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CommissionPayableAccount)
               .WithMany()
               .HasForeignKey(c => c.CommissionPayableAccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

internal class BusinessPartnerConfiguration : IEntityTypeConfiguration<BusinessPartner>
{
    public void Configure(EntityTypeBuilder<BusinessPartner> builder)
    {
        builder.ToTable("BusinessPartners");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => b.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Property(b => b.Code).HasMaxLength(30).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.NameAr).HasMaxLength(200);
        builder.Property(b => b.PartnerType).HasConversion<int>();
        builder.Property(b => b.CreditLimit).HasPrecision(18, 4);

        builder.HasOne(b => b.ReceivableAccount)
               .WithMany()
               .HasForeignKey(b => b.ReceivableAccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.PayableAccount)
               .WithMany()
               .HasForeignKey(b => b.PayableAccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Currency)
               .WithMany()
               .HasForeignKey(b => b.CurrencyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}

internal class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => b.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.Property(b => b.Code).HasMaxLength(30).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.BankName).HasMaxLength(200);
        builder.Property(b => b.AccountNumber).HasMaxLength(50);
        builder.Property(b => b.IBAN).HasMaxLength(34);
        builder.Property(b => b.AccountType).HasConversion<int>();

        builder.HasOne(b => b.GlAccount)
               .WithMany()
               .HasForeignKey(b => b.GlAccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Currency)
               .WithMany()
               .HasForeignKey(b => b.CurrencyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
