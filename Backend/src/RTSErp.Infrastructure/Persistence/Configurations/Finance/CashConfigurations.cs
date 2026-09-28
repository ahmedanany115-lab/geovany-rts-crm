using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTSErp.Domain.Entities.Finance;

namespace RTSErp.Infrastructure.Persistence.Configurations.Finance;

// ── CashReceipt ───────────────────────────────────────────────────────────────

internal class CashReceiptConfiguration : IEntityTypeConfiguration<CashReceipt>
{
    public void Configure(EntityTypeBuilder<CashReceipt> builder)
    {
        builder.ToTable("CashReceipts");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReceiptNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ReceivedFrom).HasMaxLength(300);
        builder.Property(e => e.Amount).HasPrecision(18, 4);
        builder.Property(e => e.ExchangeRate).HasPrecision(18, 6);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ReferenceNumber).HasMaxLength(100);
        builder.Property(e => e.PreparedBy).HasMaxLength(200);
        builder.Property(e => e.ApprovedBy).HasMaxLength(200);

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CashAccount)
            .WithMany()
            .HasForeignKey(e => e.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ContraAccount)
            .WithMany()
            .HasForeignKey(e => e.ContraAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── CashPayment ───────────────────────────────────────────────────────────────

internal class CashPaymentConfiguration : IEntityTypeConfiguration<CashPayment>
{
    public void Configure(EntityTypeBuilder<CashPayment> builder)
    {
        builder.ToTable("CashPayments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PaymentNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.PaidTo).HasMaxLength(300);
        builder.Property(e => e.Amount).HasPrecision(18, 4);
        builder.Property(e => e.ExchangeRate).HasPrecision(18, 6);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ReferenceNumber).HasMaxLength(100);
        builder.Property(e => e.PreparedBy).HasMaxLength(200);
        builder.Property(e => e.ApprovedBy).HasMaxLength(200);

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CashAccount)
            .WithMany()
            .HasForeignKey(e => e.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ExpenseAccount)
            .WithMany()
            .HasForeignKey(e => e.ExpenseAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
