using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RTSErp.Domain.Entities.Operational;
using RTSErp.Domain.Enums;

namespace RTSErp.Infrastructure.Persistence.Configurations.Operational;

// ── Product ───────────────────────────────────────────────────────────────────

internal class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.SKU).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(p => p.SKU).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Category).HasMaxLength(100);
        builder.Property(p => p.Unit).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Barcode).HasMaxLength(50);
        builder.Property(p => p.PurchasePrice).HasPrecision(18, 4);
        builder.Property(p => p.SalesPrice).HasPrecision(18, 4);
        builder.Property(p => p.MinimumStock).HasPrecision(18, 4);

        builder.HasOne(p => p.Currency)
            .WithMany()
            .HasForeignKey(p => p.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.TaxRate)
            .WithMany()
            .HasForeignKey(p => p.TaxRateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.InventoryAccount)
            .WithMany()
            .HasForeignKey(p => p.InventoryAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.COGSAccount)
            .WithMany()
            .HasForeignKey(p => p.COGSAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SalesAccount)
            .WithMany()
            .HasForeignKey(p => p.SalesAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PurchaseAccount)
            .WithMany()
            .HasForeignKey(p => p.PurchaseAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

// ── Warehouse ─────────────────────────────────────────────────────────────────

internal class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(w => w.Id);
        builder.HasIndex(w => w.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(w => w.Code).HasMaxLength(20).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Location).HasMaxLength(500);
        builder.Property(w => w.Notes).HasMaxLength(1000);
        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}

// ── InventoryBalance ──────────────────────────────────────────────────────────

internal class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("InventoryBalances");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.ProductId, b.WarehouseId }).IsUnique();

        builder.Property(b => b.Quantity).HasPrecision(18, 4);
        builder.Property(b => b.ReservedQuantity).HasPrecision(18, 4);
        builder.Property(b => b.AverageCost).HasPrecision(18, 4);

        builder.HasOne(b => b.Product)
            .WithMany(p => p.InventoryBalances)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Warehouse)
            .WithMany(w => w.InventoryBalances)
            .HasForeignKey(b => b.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // InventoryBalance has no IsDeleted — always present once created
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}

// ── InventoryMovement ─────────────────────────────────────────────────────────

internal class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => m.MovementDate);
        builder.HasIndex(m => m.MovementType);
        builder.HasIndex(m => new { m.ReferenceType, m.ReferenceId });

        builder.Property(m => m.Quantity).HasPrecision(18, 4);
        builder.Property(m => m.UnitCost).HasPrecision(18, 4);
        builder.Property(m => m.TotalCost).HasPrecision(18, 4);
        builder.Property(m => m.MovementType).HasConversion<int>();
        builder.Property(m => m.ReferenceType).HasMaxLength(50);
        builder.Property(m => m.ReferenceNumber).HasMaxLength(50);
        builder.Property(m => m.Notes).HasMaxLength(500);

        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Warehouse)
            .WithMany(w => w.Movements)
            .HasForeignKey(m => m.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}

// ── PurchaseOrder ─────────────────────────────────────────────────────────────

internal class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.PONumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(o => o.PONumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.Status).HasConversion<int>();
        builder.Property(o => o.ExchangeRate).HasPrecision(18, 6);
        builder.Property(o => o.SubTotal).HasPrecision(18, 4);
        builder.Property(o => o.TaxAmount).HasPrecision(18, 4);
        builder.Property(o => o.TotalAmount).HasPrecision(18, 4);

        builder.HasOne(o => o.Supplier)
            .WithMany()
            .HasForeignKey(o => o.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Currency)
            .WithMany()
            .HasForeignKey(o => o.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Warehouse)
            .WithMany()
            .HasForeignKey(o => o.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}

internal class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.PurchaseOrderId);

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.ReceivedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.DiscountPercent).HasPrecision(8, 4);
        builder.Property(l => l.DiscountAmount).HasPrecision(18, 4);
        builder.Property(l => l.TaxRate).HasPrecision(8, 4);
        builder.Property(l => l.TaxAmount).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.Property(l => l.NetAmount).HasPrecision(18, 4);

        builder.HasOne(l => l.PurchaseOrder)
            .WithMany(o => o.Lines)
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Product)
            .WithMany(p => p.PurchaseOrderLines)
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}

// ── PurchaseReceipt ───────────────────────────────────────────────────────────

internal class PurchaseReceiptConfiguration : IEntityTypeConfiguration<PurchaseReceipt>
{
    public void Configure(EntityTypeBuilder<PurchaseReceipt> builder)
    {
        builder.ToTable("PurchaseReceipts");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.ReceiptNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(r => r.ReceiptNumber).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.Property(r => r.ExchangeRate).HasPrecision(18, 6);
        builder.Property(r => r.TotalAmount).HasPrecision(18, 4);

        builder.HasOne(r => r.PurchaseOrder)
            .WithMany(o => o.Receipts)
            .HasForeignKey(r => r.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Supplier)
            .WithMany()
            .HasForeignKey(r => r.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Currency)
            .WithMany()
            .HasForeignKey(r => r.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

internal class PurchaseReceiptLineConfiguration : IEntityTypeConfiguration<PurchaseReceiptLine>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiptLine> builder)
    {
        builder.ToTable("PurchaseReceiptLines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.PurchaseReceiptId);

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.TotalCost).HasPrecision(18, 4);

        builder.HasOne(l => l.PurchaseReceipt)
            .WithMany(r => r.Lines)
            .HasForeignKey(l => l.PurchaseReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.PurchaseOrderLine)
            .WithMany()
            .HasForeignKey(l => l.PurchaseOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}

// ── SupplierInvoice ───────────────────────────────────────────────────────────

internal class SupplierInvoiceConfiguration : IEntityTypeConfiguration<SupplierInvoice>
{
    public void Configure(EntityTypeBuilder<SupplierInvoice> builder)
    {
        builder.ToTable("SupplierInvoices");
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(i => i.InvoiceNumber).HasMaxLength(30).IsRequired();
        builder.Property(i => i.SupplierInvoiceNumber).HasMaxLength(50);
        builder.Property(i => i.Status).HasConversion<int>();
        builder.Property(i => i.ExchangeRate).HasPrecision(18, 6);
        builder.Property(i => i.SubTotal).HasPrecision(18, 4);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 4);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 4);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 4);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 4);
        builder.Property(i => i.EInvoiceUUID).HasMaxLength(100);
        builder.Property(i => i.EInvoiceStatus).HasConversion<int>();

        // BalanceDue is computed, not stored
        builder.Ignore(i => i.BalanceDue);

        builder.HasOne(i => i.Supplier)
            .WithMany()
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Currency)
            .WithMany()
            .HasForeignKey(i => i.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.PurchaseReceipt)
            .WithMany()
            .HasForeignKey(i => i.PurchaseReceiptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}

internal class SupplierInvoiceLineConfiguration : IEntityTypeConfiguration<SupplierInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SupplierInvoiceLine> builder)
    {
        builder.ToTable("SupplierInvoiceLines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.SupplierInvoiceId);
        builder.Property(l => l.Description).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.DiscountPercent).HasPrecision(8, 4);
        builder.Property(l => l.DiscountAmount).HasPrecision(18, 4);
        builder.Property(l => l.TaxRate).HasPrecision(8, 4);
        builder.Property(l => l.TaxAmount).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.Property(l => l.NetAmount).HasPrecision(18, 4);

        builder.HasOne(l => l.SupplierInvoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(l => l.SupplierInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}

// ── SalesOrder ────────────────────────────────────────────────────────────────

internal class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders");
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.SONumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(o => o.SONumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.Status).HasConversion<int>();
        builder.Property(o => o.ExchangeRate).HasPrecision(18, 6);
        builder.Property(o => o.SubTotal).HasPrecision(18, 4);
        builder.Property(o => o.TaxAmount).HasPrecision(18, 4);
        builder.Property(o => o.TotalAmount).HasPrecision(18, 4);

        builder.HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Currency)
            .WithMany()
            .HasForeignKey(o => o.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Warehouse)
            .WithMany()
            .HasForeignKey(o => o.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Salesperson)
            .WithMany()
            .HasForeignKey(o => o.SalespersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}

internal class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("SalesOrderLines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.SalesOrderId);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.DeliveredQuantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.DiscountPercent).HasPrecision(8, 4);
        builder.Property(l => l.DiscountAmount).HasPrecision(18, 4);
        builder.Property(l => l.TaxRate).HasPrecision(8, 4);
        builder.Property(l => l.TaxAmount).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.Property(l => l.NetAmount).HasPrecision(18, 4);

        builder.HasOne(l => l.SalesOrder)
            .WithMany(o => o.Lines)
            .HasForeignKey(l => l.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Product)
            .WithMany(p => p.SalesOrderLines)
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}

// ── SalesDelivery ─────────────────────────────────────────────────────────────

internal class SalesDeliveryConfiguration : IEntityTypeConfiguration<SalesDelivery>
{
    public void Configure(EntityTypeBuilder<SalesDelivery> builder)
    {
        builder.ToTable("SalesDeliveries");
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.DeliveryNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(d => d.DeliveryNumber).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Notes).HasMaxLength(1000);
        builder.Property(d => d.TotalCOGS).HasPrecision(18, 4);

        builder.HasOne(d => d.SalesOrder)
            .WithMany(o => o.Deliveries)
            .HasForeignKey(d => d.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Customer)
            .WithMany()
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Warehouse)
            .WithMany()
            .HasForeignKey(d => d.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}

internal class SalesDeliveryLineConfiguration : IEntityTypeConfiguration<SalesDeliveryLine>
{
    public void Configure(EntityTypeBuilder<SalesDeliveryLine> builder)
    {
        builder.ToTable("SalesDeliveryLines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.SalesDeliveryId);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.TotalCost).HasPrecision(18, 4);

        builder.HasOne(l => l.SalesDelivery)
            .WithMany(d => d.Lines)
            .HasForeignKey(l => l.SalesDeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.SalesOrderLine)
            .WithMany()
            .HasForeignKey(l => l.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}

// ── CustomerInvoice ───────────────────────────────────────────────────────────

internal class CustomerInvoiceConfiguration : IEntityTypeConfiguration<CustomerInvoice>
{
    public void Configure(EntityTypeBuilder<CustomerInvoice> builder)
    {
        builder.ToTable("CustomerInvoices");
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(i => i.InvoiceNumber).HasMaxLength(30).IsRequired();
        builder.Property(i => i.Status).HasConversion<int>();
        builder.Property(i => i.InvoiceType).HasConversion<int>();
        builder.Property(i => i.ExchangeRate).HasPrecision(18, 6);
        builder.Property(i => i.SubTotal).HasPrecision(18, 4);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 4);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 4);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 4);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 4);
        builder.Property(i => i.TaxRegistrationNumber).HasMaxLength(50);
        builder.Property(i => i.EInvoiceUUID).HasMaxLength(100);
        builder.Property(i => i.EInvoiceStatus).HasConversion<int>();
        builder.Property(i => i.ExternalInvoiceId).HasMaxLength(100);
        builder.Property(i => i.ExternalStatus).HasMaxLength(50);
        builder.Property(i => i.CancellationStatus).HasMaxLength(50);

        builder.Ignore(i => i.BalanceDue);

        builder.HasOne(i => i.Customer)
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Currency)
            .WithMany()
            .HasForeignKey(i => i.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Salesperson)
            .WithMany()
            .HasForeignKey(i => i.SalespersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.SalesOrder)
            .WithMany()
            .HasForeignKey(i => i.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}

internal class CustomerInvoiceLineConfiguration : IEntityTypeConfiguration<CustomerInvoiceLine>
{
    public void Configure(EntityTypeBuilder<CustomerInvoiceLine> builder)
    {
        builder.ToTable("CustomerInvoiceLines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.CustomerInvoiceId);
        builder.Property(l => l.Description).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.DiscountPercent).HasPrecision(8, 4);
        builder.Property(l => l.DiscountAmount).HasPrecision(18, 4);
        builder.Property(l => l.TaxRate).HasPrecision(8, 4);
        builder.Property(l => l.TaxAmount).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.Property(l => l.NetAmount).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.TotalCost).HasPrecision(18, 4);

        builder.HasOne(l => l.CustomerInvoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(l => l.CustomerInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}

// ── CustomerPayment ───────────────────────────────────────────────────────────

internal class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.ToTable("CustomerPayments");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.PaymentNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(p => p.PaymentNumber).HasMaxLength(30).IsRequired();
        builder.Property(p => p.PaymentMethod).HasConversion<int>();
        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.ExchangeRate).HasPrecision(18, 6);
        builder.Property(p => p.Amount).HasPrecision(18, 4);
        builder.Property(p => p.AmountBase).HasPrecision(18, 4);
        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Currency)
            .WithMany()
            .HasForeignKey(p => p.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.BankAccount)
            .WithMany()
            .HasForeignKey(p => p.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Cheque)
            .WithMany()
            .HasForeignKey(p => p.ChequeId)
            .OnDelete(DeleteBehavior.Restrict);

        // M:N to CustomerInvoice via join table
        builder.HasMany(p => p.Invoices)
            .WithMany(i => i.Payments)
            .UsingEntity(j => j.ToTable("CustomerPaymentInvoices"));

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

// ── SupplierPayment ───────────────────────────────────────────────────────────

internal class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
    public void Configure(EntityTypeBuilder<SupplierPayment> builder)
    {
        builder.ToTable("SupplierPayments");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.PaymentNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(p => p.PaymentNumber).HasMaxLength(30).IsRequired();
        builder.Property(p => p.PaymentMethod).HasConversion<int>();
        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.ExchangeRate).HasPrecision(18, 6);
        builder.Property(p => p.Amount).HasPrecision(18, 4);
        builder.Property(p => p.AmountBase).HasPrecision(18, 4);
        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Currency)
            .WithMany()
            .HasForeignKey(p => p.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.BankAccount)
            .WithMany()
            .HasForeignKey(p => p.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Invoices)
            .WithMany(i => i.Payments)
            .UsingEntity(j => j.ToTable("SupplierPaymentInvoices"));

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

// ── BankTransaction ───────────────────────────────────────────────────────────

internal class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.ToTable("BankTransactions");
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TransactionNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Property(t => t.TransactionNumber).HasMaxLength(30).IsRequired();
        builder.Property(t => t.TransactionType).HasConversion<int>();
        builder.Property(t => t.ExchangeRate).HasPrecision(18, 6);
        builder.Property(t => t.Amount).HasPrecision(18, 4);
        builder.Property(t => t.AmountBase).HasPrecision(18, 4);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.Reference).HasMaxLength(100);

        builder.HasOne(t => t.BankAccount)
            .WithMany()
            .HasForeignKey(t => t.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Currency)
            .WithMany()
            .HasForeignKey(t => t.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.DestinationBankAccount)
            .WithMany()
            .HasForeignKey(t => t.DestinationBankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

// ── Cheque ────────────────────────────────────────────────────────────────────

internal class ChequeConfiguration : IEntityTypeConfiguration<Cheque>
{
    public void Configure(EntityTypeBuilder<Cheque> builder)
    {
        builder.ToTable("Cheques");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.ChequeNumber);
        builder.Property(c => c.ChequeNumber).HasMaxLength(50).IsRequired();
        builder.Property(c => c.BankName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Status).HasConversion<int>();
        builder.Property(c => c.Amount).HasPrecision(18, 4);
        builder.Property(c => c.AmountBase).HasPrecision(18, 4);
        builder.Property(c => c.Notes).HasMaxLength(500);

        builder.HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Currency)
            .WithMany()
            .HasForeignKey(c => c.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.BankAccount)
            .WithMany()
            .HasForeignKey(c => c.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

// ── SalesCommission ───────────────────────────────────────────────────────────

internal class SalesCommissionConfiguration : IEntityTypeConfiguration<SalesCommission>
{
    public void Configure(EntityTypeBuilder<SalesCommission> builder)
    {
        builder.ToTable("SalesCommissions");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.SalespersonName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Rate).HasPrecision(8, 4);
        builder.Property(c => c.BaseSalesAmount).HasPrecision(18, 4);
        builder.Property(c => c.CommissionAmount).HasPrecision(18, 4);
        builder.Property(c => c.ExchangeRate).HasPrecision(18, 6);
        builder.Property(c => c.Status).HasConversion<int>();

        builder.HasOne(c => c.CommissionRate)
            .WithMany()
            .HasForeignKey(c => c.CommissionRateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Currency)
            .WithMany()
            .HasForeignKey(c => c.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.SalesOrder)
            .WithMany(o => o.Commissions)
            .HasForeignKey(c => c.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CustomerInvoice)
            .WithMany(i => i.Commissions)
            .HasForeignKey(c => c.CustomerInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
