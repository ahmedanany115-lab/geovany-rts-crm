using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RTSErp.Infrastructure.Migrations
{
    public partial class AddOperationalErpModules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Add fields to BusinessPartners ────────────────────────────────
            migrationBuilder.AddColumn<string>("TaxNumber",  "BusinessPartners", nullable: true, maxLength: 50);
            migrationBuilder.AddColumn<string>("Phone",      "BusinessPartners", nullable: true, maxLength: 30);
            migrationBuilder.AddColumn<string>("Email",      "BusinessPartners", nullable: true, maxLength: 200);
            migrationBuilder.AddColumn<string>("Address",    "BusinessPartners", nullable: true, maxLength: 500);
            migrationBuilder.AddColumn<string>("Notes",      "BusinessPartners", nullable: true, maxLength: 1000);

            // ── Add fields to BankAccounts ────────────────────────────────────
            migrationBuilder.AddColumn<decimal>("OpeningBalance", "BankAccounts",
                type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>("CurrentBalance", "BankAccounts",
                type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m);

            // ── Products ──────────────────────────────────────────────────────
            migrationBuilder.CreateTable("Products",
                columns: t => new
                {
                    Id              = t.Column<Guid>("uniqueidentifier"),
                    SKU             = t.Column<string>("nvarchar(50)", maxLength: 50),
                    Name            = t.Column<string>("nvarchar(200)", maxLength: 200),
                    Description     = t.Column<string>("nvarchar(1000)", nullable: true),
                    Category        = t.Column<string>("nvarchar(100)", nullable: true),
                    Unit            = t.Column<string>("nvarchar(30)", maxLength: 30, defaultValue: "Piece"),
                    Barcode         = t.Column<string>("nvarchar(50)", nullable: true),
                    PurchasePrice   = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    SalesPrice      = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    CurrencyId      = t.Column<Guid>("uniqueidentifier"),
                    TaxRateId       = t.Column<Guid>("uniqueidentifier", nullable: true),
                    InventoryAccountId = t.Column<Guid>("uniqueidentifier", nullable: true),
                    COGSAccountId   = t.Column<Guid>("uniqueidentifier", nullable: true),
                    SalesAccountId  = t.Column<Guid>("uniqueidentifier", nullable: true),
                    PurchaseAccountId = t.Column<Guid>("uniqueidentifier", nullable: true),
                    MinimumStock    = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    IsActive        = t.Column<bool>(defaultValue: true),
                    CreatedAt       = t.Column<DateTime>("datetime2"),
                    CreatedBy       = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt      = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy      = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted       = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_Products", x => x.Id);
                    t.ForeignKey("FK_Products_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_Products_TaxRates_TaxRateId", x => x.TaxRateId, "TaxRates", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_Products_Accounts_InventoryAccountId", x => x.InventoryAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_Products_Accounts_COGSAccountId", x => x.COGSAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_Products_Accounts_SalesAccountId", x => x.SalesAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_Products_Accounts_PurchaseAccountId", x => x.PurchaseAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_Products_SKU", "Products", "SKU", unique: true, filter: "[IsDeleted] = 0");

            // ── Warehouses ────────────────────────────────────────────────────
            migrationBuilder.CreateTable("Warehouses",
                columns: t => new
                {
                    Id        = t.Column<Guid>("uniqueidentifier"),
                    Code      = t.Column<string>("nvarchar(20)", maxLength: 20),
                    Name      = t.Column<string>("nvarchar(200)", maxLength: 200),
                    Location  = t.Column<string>("nvarchar(500)", nullable: true),
                    Notes     = t.Column<string>("nvarchar(1000)", nullable: true),
                    IsActive  = t.Column<bool>(defaultValue: true),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t => t.PrimaryKey("PK_Warehouses", x => x.Id));
            migrationBuilder.CreateIndex("IX_Warehouses_Code", "Warehouses", "Code", unique: true, filter: "[IsDeleted] = 0");

            // ── InventoryBalances ─────────────────────────────────────────────
            migrationBuilder.CreateTable("InventoryBalances",
                columns: t => new
                {
                    Id               = t.Column<Guid>("uniqueidentifier"),
                    ProductId        = t.Column<Guid>("uniqueidentifier"),
                    WarehouseId      = t.Column<Guid>("uniqueidentifier"),
                    Quantity         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    ReservedQuantity = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    AverageCost      = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_InventoryBalances", x => x.Id);
                    t.ForeignKey("FK_InventoryBalances_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_InventoryBalances_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_InventoryBalances_ProductId_WarehouseId", "InventoryBalances", new[] { "ProductId", "WarehouseId" }, unique: true);

            // ── InventoryMovements ────────────────────────────────────────────
            migrationBuilder.CreateTable("InventoryMovements",
                columns: t => new
                {
                    Id              = t.Column<Guid>("uniqueidentifier"),
                    ProductId       = t.Column<Guid>("uniqueidentifier"),
                    WarehouseId     = t.Column<Guid>("uniqueidentifier"),
                    MovementType    = t.Column<int>("int"),
                    Quantity        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    UnitCost        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    TotalCost       = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    MovementDate    = t.Column<DateOnly>("date"),
                    Notes           = t.Column<string>("nvarchar(500)", nullable: true),
                    ReferenceType   = t.Column<string>("nvarchar(50)", nullable: true),
                    ReferenceId     = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ReferenceNumber = t.Column<string>("nvarchar(50)", nullable: true),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    t.ForeignKey("FK_InventoryMovements_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_InventoryMovements_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_InventoryMovements_MovementDate", "InventoryMovements", "MovementDate");
            migrationBuilder.CreateIndex("IX_InventoryMovements_ProductId", "InventoryMovements", "ProductId");

            // ── PurchaseOrders ────────────────────────────────────────────────
            migrationBuilder.CreateTable("PurchaseOrders",
                columns: t => new
                {
                    Id           = t.Column<Guid>("uniqueidentifier"),
                    PONumber     = t.Column<string>("nvarchar(30)", maxLength: 30),
                    SupplierId   = t.Column<Guid>("uniqueidentifier"),
                    OrderDate    = t.Column<DateOnly>("date"),
                    Notes        = t.Column<string>("nvarchar(1000)", nullable: true),
                    CurrencyId   = t.Column<Guid>("uniqueidentifier"),
                    ExchangeRate = t.Column<decimal>("decimal(18,6)", precision: 18, scale: 6, defaultValue: 1m),
                    WarehouseId  = t.Column<Guid>("uniqueidentifier"),
                    Status       = t.Column<int>("int", defaultValue: 1),
                    SubTotal     = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TaxAmount    = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TotalAmount  = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    t.ForeignKey("FK_PurchaseOrders_BusinessPartners_SupplierId", x => x.SupplierId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_PurchaseOrders_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_PurchaseOrders_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_PurchaseOrders_PONumber", "PurchaseOrders", "PONumber", unique: true, filter: "[IsDeleted] = 0");

            // ── PurchaseOrderLines ────────────────────────────────────────────
            migrationBuilder.CreateTable("PurchaseOrderLines",
                columns: t => new
                {
                    Id               = t.Column<Guid>("uniqueidentifier"),
                    PurchaseOrderId  = t.Column<Guid>("uniqueidentifier"),
                    ProductId        = t.Column<Guid>("uniqueidentifier"),
                    Quantity         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    ReceivedQuantity = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    UnitPrice        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    DiscountPercent  = t.Column<decimal>("decimal(8,4)", precision: 8, scale: 4, defaultValue: 0m),
                    DiscountAmount   = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TaxRate          = t.Column<decimal>("decimal(8,4)", precision: 8, scale: 4, defaultValue: 0m),
                    TaxAmount        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    LineTotal        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    NetAmount        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    SortOrder        = t.Column<int>(defaultValue: 0),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    t.ForeignKey("FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId", x => x.PurchaseOrderId, "PurchaseOrders", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_PurchaseOrderLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── PurchaseReceipts ──────────────────────────────────────────────
            migrationBuilder.CreateTable("PurchaseReceipts",
                columns: t => new
                {
                    Id              = t.Column<Guid>("uniqueidentifier"),
                    ReceiptNumber   = t.Column<string>("nvarchar(30)", maxLength: 30),
                    PurchaseOrderId = t.Column<Guid>("uniqueidentifier"),
                    SupplierId      = t.Column<Guid>("uniqueidentifier"),
                    WarehouseId     = t.Column<Guid>("uniqueidentifier"),
                    ReceiptDate     = t.Column<DateOnly>("date"),
                    CurrencyId      = t.Column<Guid>("uniqueidentifier"),
                    ExchangeRate    = t.Column<decimal>("decimal(18,6)", precision: 18, scale: 6, defaultValue: 1m),
                    Notes           = t.Column<string>("nvarchar(1000)", nullable: true),
                    TotalAmount     = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    JournalEntryId  = t.Column<Guid>("uniqueidentifier", nullable: true),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_PurchaseReceipts", x => x.Id);
                    t.ForeignKey("FK_PurchaseReceipts_PurchaseOrders_PurchaseOrderId", x => x.PurchaseOrderId, "PurchaseOrders", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_PurchaseReceipts_BusinessPartners_SupplierId", x => x.SupplierId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_PurchaseReceipts_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_PurchaseReceipts_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_PurchaseReceipts_ReceiptNumber", "PurchaseReceipts", "ReceiptNumber", unique: true, filter: "[IsDeleted] = 0");

            // ── PurchaseReceiptLines ──────────────────────────────────────────
            migrationBuilder.CreateTable("PurchaseReceiptLines",
                columns: t => new
                {
                    Id                  = t.Column<Guid>("uniqueidentifier"),
                    PurchaseReceiptId   = t.Column<Guid>("uniqueidentifier"),
                    PurchaseOrderLineId = t.Column<Guid>("uniqueidentifier"),
                    ProductId           = t.Column<Guid>("uniqueidentifier"),
                    Quantity            = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    UnitCost            = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    TotalCost           = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_PurchaseReceiptLines", x => x.Id);
                    t.ForeignKey("FK_PurchaseReceiptLines_PurchaseReceipts_PurchaseReceiptId", x => x.PurchaseReceiptId, "PurchaseReceipts", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_PurchaseReceiptLines_PurchaseOrderLines_PurchaseOrderLineId", x => x.PurchaseOrderLineId, "PurchaseOrderLines", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_PurchaseReceiptLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── SupplierInvoices ──────────────────────────────────────────────
            migrationBuilder.CreateTable("SupplierInvoices",
                columns: t => new
                {
                    Id                    = t.Column<Guid>("uniqueidentifier"),
                    InvoiceNumber         = t.Column<string>("nvarchar(30)", maxLength: 30),
                    SupplierInvoiceNumber = t.Column<string>("nvarchar(50)", nullable: true),
                    SupplierId            = t.Column<Guid>("uniqueidentifier"),
                    InvoiceDate           = t.Column<DateOnly>("date"),
                    DueDate               = t.Column<DateOnly>("date"),
                    CurrencyId            = t.Column<Guid>("uniqueidentifier"),
                    ExchangeRate          = t.Column<decimal>("decimal(18,6)", precision: 18, scale: 6, defaultValue: 1m),
                    PurchaseReceiptId     = t.Column<Guid>("uniqueidentifier", nullable: true),
                    Status                = t.Column<int>("int", defaultValue: 1),
                    SubTotal              = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    DiscountAmount        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TaxAmount             = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TotalAmount           = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    PaidAmount            = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    JournalEntryId        = t.Column<Guid>("uniqueidentifier", nullable: true),
                    EInvoiceUUID          = t.Column<string>("nvarchar(100)", nullable: true),
                    EInvoiceStatus        = t.Column<int>("int", defaultValue: 0),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_SupplierInvoices", x => x.Id);
                    t.ForeignKey("FK_SupplierInvoices_BusinessPartners_SupplierId", x => x.SupplierId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SupplierInvoices_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SupplierInvoices_PurchaseReceipts_PurchaseReceiptId", x => x.PurchaseReceiptId, "PurchaseReceipts", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_SupplierInvoices_InvoiceNumber", "SupplierInvoices", "InvoiceNumber", unique: true, filter: "[IsDeleted] = 0");

            migrationBuilder.CreateTable("SupplierInvoiceLines",
                columns: t => new
                {
                    Id               = t.Column<Guid>("uniqueidentifier"),
                    SupplierInvoiceId = t.Column<Guid>("uniqueidentifier"),
                    ProductId        = t.Column<Guid>("uniqueidentifier"),
                    Description      = t.Column<string>("nvarchar(500)", maxLength: 500),
                    Quantity         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    UnitPrice        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    DiscountPercent  = t.Column<decimal>("decimal(8,4)", precision: 8, scale: 4, defaultValue: 0m),
                    DiscountAmount   = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TaxRate          = t.Column<decimal>("decimal(8,4)", precision: 8, scale: 4, defaultValue: 0m),
                    TaxAmount        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    LineTotal        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    NetAmount        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    SortOrder        = t.Column<int>(defaultValue: 0),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_SupplierInvoiceLines", x => x.Id);
                    t.ForeignKey("FK_SupplierInvoiceLines_SupplierInvoices_SupplierInvoiceId", x => x.SupplierInvoiceId, "SupplierInvoices", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_SupplierInvoiceLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── SalesOrders ───────────────────────────────────────────────────
            migrationBuilder.CreateTable("SalesOrders",
                columns: t => new
                {
                    Id           = t.Column<Guid>("uniqueidentifier"),
                    SONumber     = t.Column<string>("nvarchar(30)", maxLength: 30),
                    CustomerId   = t.Column<Guid>("uniqueidentifier"),
                    OrderDate    = t.Column<DateOnly>("date"),
                    Notes        = t.Column<string>("nvarchar(1000)", nullable: true),
                    CurrencyId   = t.Column<Guid>("uniqueidentifier"),
                    ExchangeRate = t.Column<decimal>("decimal(18,6)", precision: 18, scale: 6, defaultValue: 1m),
                    WarehouseId  = t.Column<Guid>("uniqueidentifier"),
                    SalespersonId = t.Column<Guid>("uniqueidentifier", nullable: true),
                    Status       = t.Column<int>("int", defaultValue: 1),
                    SubTotal     = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TaxAmount    = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TotalAmount  = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_SalesOrders", x => x.Id);
                    t.ForeignKey("FK_SalesOrders_BusinessPartners_CustomerId", x => x.CustomerId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SalesOrders_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SalesOrders_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SalesOrders_AspNetUsers_SalespersonId", x => x.SalespersonId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_SalesOrders_SONumber", "SalesOrders", "SONumber", unique: true, filter: "[IsDeleted] = 0");

            migrationBuilder.CreateTable("SalesOrderLines",
                columns: t => new
                {
                    Id                = t.Column<Guid>("uniqueidentifier"),
                    SalesOrderId      = t.Column<Guid>("uniqueidentifier"),
                    ProductId         = t.Column<Guid>("uniqueidentifier"),
                    Quantity          = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    DeliveredQuantity = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    UnitPrice         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    DiscountPercent   = t.Column<decimal>("decimal(8,4)", precision: 8, scale: 4, defaultValue: 0m),
                    DiscountAmount    = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TaxRate           = t.Column<decimal>("decimal(8,4)", precision: 8, scale: 4, defaultValue: 0m),
                    TaxAmount         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    LineTotal         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    NetAmount         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    SortOrder         = t.Column<int>(defaultValue: 0),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_SalesOrderLines", x => x.Id);
                    t.ForeignKey("FK_SalesOrderLines_SalesOrders_SalesOrderId", x => x.SalesOrderId, "SalesOrders", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_SalesOrderLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── SalesDeliveries ───────────────────────────────────────────────
            migrationBuilder.CreateTable("SalesDeliveries",
                columns: t => new
                {
                    Id             = t.Column<Guid>("uniqueidentifier"),
                    DeliveryNumber = t.Column<string>("nvarchar(30)", maxLength: 30),
                    SalesOrderId   = t.Column<Guid>("uniqueidentifier"),
                    CustomerId     = t.Column<Guid>("uniqueidentifier"),
                    WarehouseId    = t.Column<Guid>("uniqueidentifier"),
                    DeliveryDate   = t.Column<DateOnly>("date"),
                    Notes          = t.Column<string>("nvarchar(1000)", nullable: true),
                    TotalCOGS      = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    JournalEntryId = t.Column<Guid>("uniqueidentifier", nullable: true),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_SalesDeliveries", x => x.Id);
                    t.ForeignKey("FK_SalesDeliveries_SalesOrders_SalesOrderId", x => x.SalesOrderId, "SalesOrders", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SalesDeliveries_BusinessPartners_CustomerId", x => x.CustomerId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SalesDeliveries_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_SalesDeliveries_DeliveryNumber", "SalesDeliveries", "DeliveryNumber", unique: true, filter: "[IsDeleted] = 0");

            migrationBuilder.CreateTable("SalesDeliveryLines",
                columns: t => new
                {
                    Id               = t.Column<Guid>("uniqueidentifier"),
                    SalesDeliveryId  = t.Column<Guid>("uniqueidentifier"),
                    SalesOrderLineId = t.Column<Guid>("uniqueidentifier"),
                    ProductId        = t.Column<Guid>("uniqueidentifier"),
                    Quantity         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    UnitCost         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    TotalCost        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_SalesDeliveryLines", x => x.Id);
                    t.ForeignKey("FK_SalesDeliveryLines_SalesDeliveries_SalesDeliveryId", x => x.SalesDeliveryId, "SalesDeliveries", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_SalesDeliveryLines_SalesOrderLines_SalesOrderLineId", x => x.SalesOrderLineId, "SalesOrderLines", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SalesDeliveryLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── CustomerInvoices ──────────────────────────────────────────────
            migrationBuilder.CreateTable("CustomerInvoices",
                columns: t => new
                {
                    Id                     = t.Column<Guid>("uniqueidentifier"),
                    InvoiceNumber          = t.Column<string>("nvarchar(30)", maxLength: 30),
                    CustomerId             = t.Column<Guid>("uniqueidentifier"),
                    InvoiceDate            = t.Column<DateOnly>("date"),
                    DueDate                = t.Column<DateOnly>("date"),
                    CurrencyId             = t.Column<Guid>("uniqueidentifier"),
                    ExchangeRate           = t.Column<decimal>("decimal(18,6)", precision: 18, scale: 6, defaultValue: 1m),
                    SalespersonId          = t.Column<Guid>("uniqueidentifier", nullable: true),
                    SalesOrderId           = t.Column<Guid>("uniqueidentifier", nullable: true),
                    Status                 = t.Column<int>("int", defaultValue: 1),
                    InvoiceType            = t.Column<int>("int", defaultValue: 1),
                    SubTotal               = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    DiscountAmount         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TaxAmount              = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TotalAmount            = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    PaidAmount             = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    JournalEntryId         = t.Column<Guid>("uniqueidentifier", nullable: true),
                    TaxRegistrationNumber  = t.Column<string>("nvarchar(50)", nullable: true),
                    EInvoiceUUID           = t.Column<string>("nvarchar(100)", nullable: true),
                    EInvoiceStatus         = t.Column<int>("int", defaultValue: 0),
                    EInvoiceSubmissionDate = t.Column<DateTime>("datetime2", nullable: true),
                    ExternalInvoiceId      = t.Column<string>("nvarchar(100)", nullable: true),
                    ExternalStatus         = t.Column<string>("nvarchar(50)", nullable: true),
                    QRCode                 = t.Column<string>("nvarchar(max)", nullable: true),
                    CancellationStatus     = t.Column<string>("nvarchar(50)", nullable: true),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_CustomerInvoices", x => x.Id);
                    t.ForeignKey("FK_CustomerInvoices_BusinessPartners_CustomerId", x => x.CustomerId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_CustomerInvoices_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_CustomerInvoices_AspNetUsers_SalespersonId", x => x.SalespersonId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_CustomerInvoices_SalesOrders_SalesOrderId", x => x.SalesOrderId, "SalesOrders", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_CustomerInvoices_InvoiceNumber", "CustomerInvoices", "InvoiceNumber", unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex("IX_CustomerInvoices_CustomerId", "CustomerInvoices", "CustomerId");

            migrationBuilder.CreateTable("CustomerInvoiceLines",
                columns: t => new
                {
                    Id                = t.Column<Guid>("uniqueidentifier"),
                    CustomerInvoiceId = t.Column<Guid>("uniqueidentifier"),
                    ProductId         = t.Column<Guid>("uniqueidentifier"),
                    Description       = t.Column<string>("nvarchar(500)", maxLength: 500),
                    Quantity          = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    UnitPrice         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    DiscountPercent   = t.Column<decimal>("decimal(8,4)", precision: 8, scale: 4, defaultValue: 0m),
                    DiscountAmount    = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TaxRate           = t.Column<decimal>("decimal(8,4)", precision: 8, scale: 4, defaultValue: 0m),
                    TaxAmount         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    LineTotal         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    NetAmount         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    UnitCost          = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    TotalCost         = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4, defaultValue: 0m),
                    SortOrder         = t.Column<int>(defaultValue: 0),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_CustomerInvoiceLines", x => x.Id);
                    t.ForeignKey("FK_CustomerInvoiceLines_CustomerInvoices_CustomerInvoiceId", x => x.CustomerInvoiceId, "CustomerInvoices", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_CustomerInvoiceLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── Cheques ───────────────────────────────────────────────────────
            migrationBuilder.CreateTable("Cheques",
                columns: t => new
                {
                    Id                     = t.Column<Guid>("uniqueidentifier"),
                    ChequeNumber           = t.Column<string>("nvarchar(50)", maxLength: 50),
                    CustomerId             = t.Column<Guid>("uniqueidentifier"),
                    BankName               = t.Column<string>("nvarchar(200)", maxLength: 200),
                    CurrencyId             = t.Column<Guid>("uniqueidentifier"),
                    Amount                 = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    AmountBase             = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    IssueDate              = t.Column<DateOnly>("date"),
                    DueDate                = t.Column<DateOnly>("date"),
                    ReceivedDate           = t.Column<DateOnly>("date"),
                    BankAccountId          = t.Column<Guid>("uniqueidentifier", nullable: true),
                    Status                 = t.Column<int>("int", defaultValue: 1),
                    Notes                  = t.Column<string>("nvarchar(500)", nullable: true),
                    ReceiptJournalEntryId  = t.Column<Guid>("uniqueidentifier", nullable: true),
                    DepositJournalEntryId  = t.Column<Guid>("uniqueidentifier", nullable: true),
                    BounceJournalEntryId   = t.Column<Guid>("uniqueidentifier", nullable: true),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_Cheques", x => x.Id);
                    t.ForeignKey("FK_Cheques_BusinessPartners_CustomerId", x => x.CustomerId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_Cheques_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_Cheques_BankAccounts_BankAccountId", x => x.BankAccountId, "BankAccounts", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── CustomerPayments ──────────────────────────────────────────────
            migrationBuilder.CreateTable("CustomerPayments",
                columns: t => new
                {
                    Id              = t.Column<Guid>("uniqueidentifier"),
                    PaymentNumber   = t.Column<string>("nvarchar(30)", maxLength: 30),
                    CustomerId      = t.Column<Guid>("uniqueidentifier"),
                    PaymentDate     = t.Column<DateOnly>("date"),
                    CurrencyId      = t.Column<Guid>("uniqueidentifier"),
                    ExchangeRate    = t.Column<decimal>("decimal(18,6)", precision: 18, scale: 6, defaultValue: 1m),
                    Amount          = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    AmountBase      = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    PaymentMethod   = t.Column<int>("int", defaultValue: 1),
                    Status          = t.Column<int>("int", defaultValue: 1),
                    BankAccountId   = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ChequeId        = t.Column<Guid>("uniqueidentifier", nullable: true),
                    Notes           = t.Column<string>("nvarchar(500)", nullable: true),
                    JournalEntryId  = t.Column<Guid>("uniqueidentifier", nullable: true),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_CustomerPayments", x => x.Id);
                    t.ForeignKey("FK_CustomerPayments_BusinessPartners_CustomerId", x => x.CustomerId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_CustomerPayments_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_CustomerPayments_BankAccounts_BankAccountId", x => x.BankAccountId, "BankAccounts", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_CustomerPayments_Cheques_ChequeId", x => x.ChequeId, "Cheques", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_CustomerPayments_PaymentNumber", "CustomerPayments", "PaymentNumber", unique: true, filter: "[IsDeleted] = 0");

            migrationBuilder.CreateTable("CustomerPaymentInvoices",
                columns: t => new
                {
                    InvoicesId  = t.Column<Guid>("uniqueidentifier"),
                    PaymentsId  = t.Column<Guid>("uniqueidentifier")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_CustomerPaymentInvoices", x => new { x.InvoicesId, x.PaymentsId });
                    t.ForeignKey("FK_CustomerPaymentInvoices_CustomerInvoices_InvoicesId", x => x.InvoicesId, "CustomerInvoices", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_CustomerPaymentInvoices_CustomerPayments_PaymentsId", x => x.PaymentsId, "CustomerPayments", "Id", onDelete: ReferentialAction.Cascade);
                });

            // ── SupplierPayments ──────────────────────────────────────────────
            migrationBuilder.CreateTable("SupplierPayments",
                columns: t => new
                {
                    Id            = t.Column<Guid>("uniqueidentifier"),
                    PaymentNumber = t.Column<string>("nvarchar(30)", maxLength: 30),
                    SupplierId    = t.Column<Guid>("uniqueidentifier"),
                    PaymentDate   = t.Column<DateOnly>("date"),
                    CurrencyId    = t.Column<Guid>("uniqueidentifier"),
                    ExchangeRate  = t.Column<decimal>("decimal(18,6)", precision: 18, scale: 6, defaultValue: 1m),
                    Amount        = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    AmountBase    = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    PaymentMethod = t.Column<int>("int", defaultValue: 1),
                    Status        = t.Column<int>("int", defaultValue: 1),
                    BankAccountId = t.Column<Guid>("uniqueidentifier", nullable: true),
                    Notes         = t.Column<string>("nvarchar(500)", nullable: true),
                    JournalEntryId = t.Column<Guid>("uniqueidentifier", nullable: true),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_SupplierPayments", x => x.Id);
                    t.ForeignKey("FK_SupplierPayments_BusinessPartners_SupplierId", x => x.SupplierId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SupplierPayments_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SupplierPayments_BankAccounts_BankAccountId", x => x.BankAccountId, "BankAccounts", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_SupplierPayments_PaymentNumber", "SupplierPayments", "PaymentNumber", unique: true, filter: "[IsDeleted] = 0");

            migrationBuilder.CreateTable("SupplierPaymentInvoices",
                columns: t => new
                {
                    InvoicesId  = t.Column<Guid>("uniqueidentifier"),
                    PaymentsId  = t.Column<Guid>("uniqueidentifier")
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_SupplierPaymentInvoices", x => new { x.InvoicesId, x.PaymentsId });
                    t.ForeignKey("FK_SupplierPaymentInvoices_SupplierInvoices_InvoicesId", x => x.InvoicesId, "SupplierInvoices", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_SupplierPaymentInvoices_SupplierPayments_PaymentsId", x => x.PaymentsId, "SupplierPayments", "Id", onDelete: ReferentialAction.Cascade);
                });

            // ── BankTransactions ──────────────────────────────────────────────
            migrationBuilder.CreateTable("BankTransactions",
                columns: t => new
                {
                    Id                       = t.Column<Guid>("uniqueidentifier"),
                    TransactionNumber        = t.Column<string>("nvarchar(30)", maxLength: 30),
                    BankAccountId            = t.Column<Guid>("uniqueidentifier"),
                    TransactionType          = t.Column<int>("int"),
                    TransactionDate          = t.Column<DateOnly>("date"),
                    CurrencyId               = t.Column<Guid>("uniqueidentifier"),
                    ExchangeRate             = t.Column<decimal>("decimal(18,6)", precision: 18, scale: 6, defaultValue: 1m),
                    Amount                   = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    AmountBase               = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    Description              = t.Column<string>("nvarchar(500)", nullable: true),
                    Reference                = t.Column<string>("nvarchar(100)", nullable: true),
                    DestinationBankAccountId = t.Column<Guid>("uniqueidentifier", nullable: true),
                    JournalEntryId           = t.Column<Guid>("uniqueidentifier", nullable: true),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_BankTransactions", x => x.Id);
                    t.ForeignKey("FK_BankTransactions_BankAccounts_BankAccountId", x => x.BankAccountId, "BankAccounts", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_BankTransactions_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_BankTransactions_BankAccounts_DestinationBankAccountId", x => x.DestinationBankAccountId, "BankAccounts", "Id", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_BankTransactions_TransactionNumber", "BankTransactions", "TransactionNumber", unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex("IX_BankTransactions_TransactionDate", "BankTransactions", "TransactionDate");

            // ── SalesCommissions ──────────────────────────────────────────────
            migrationBuilder.CreateTable("SalesCommissions",
                columns: t => new
                {
                    Id                = t.Column<Guid>("uniqueidentifier"),
                    SalespersonId     = t.Column<Guid>("uniqueidentifier", nullable: true),
                    SalespersonName   = t.Column<string>("nvarchar(200)", maxLength: 200),
                    CommissionRateId  = t.Column<Guid>("uniqueidentifier"),
                    Rate              = t.Column<decimal>("decimal(8,4)", precision: 8, scale: 4),
                    BaseSalesAmount   = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    CommissionAmount  = t.Column<decimal>("decimal(18,4)", precision: 18, scale: 4),
                    CurrencyId        = t.Column<Guid>("uniqueidentifier"),
                    ExchangeRate      = t.Column<decimal>("decimal(18,6)", precision: 18, scale: 6, defaultValue: 1m),
                    Status            = t.Column<int>("int", defaultValue: 1),
                    SalesOrderId      = t.Column<Guid>("uniqueidentifier", nullable: true),
                    CustomerInvoiceId = t.Column<Guid>("uniqueidentifier", nullable: true),
                    JournalEntryId    = t.Column<Guid>("uniqueidentifier", nullable: true),
                    CreatedAt = t.Column<DateTime>("datetime2"),
                    CreatedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    ModifiedAt = t.Column<DateTime>("datetime2", nullable: true),
                    ModifiedBy = t.Column<Guid>("uniqueidentifier", nullable: true),
                    IsDeleted = t.Column<bool>(defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_SalesCommissions", x => x.Id);
                    t.ForeignKey("FK_SalesCommissions_CommissionRates_CommissionRateId", x => x.CommissionRateId, "CommissionRates", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SalesCommissions_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SalesCommissions_SalesOrders_SalesOrderId", x => x.SalesOrderId, "SalesOrders", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_SalesCommissions_CustomerInvoices_CustomerInvoiceId", x => x.CustomerInvoiceId, "CustomerInvoices", "Id", onDelete: ReferentialAction.Restrict);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("SalesCommissions");
            migrationBuilder.DropTable("BankTransactions");
            migrationBuilder.DropTable("SupplierPaymentInvoices");
            migrationBuilder.DropTable("SupplierPayments");
            migrationBuilder.DropTable("CustomerPaymentInvoices");
            migrationBuilder.DropTable("CustomerPayments");
            migrationBuilder.DropTable("Cheques");
            migrationBuilder.DropTable("CustomerInvoiceLines");
            migrationBuilder.DropTable("CustomerInvoices");
            migrationBuilder.DropTable("SalesDeliveryLines");
            migrationBuilder.DropTable("SalesDeliveries");
            migrationBuilder.DropTable("SalesOrderLines");
            migrationBuilder.DropTable("SalesOrders");
            migrationBuilder.DropTable("SupplierInvoiceLines");
            migrationBuilder.DropTable("SupplierInvoices");
            migrationBuilder.DropTable("PurchaseReceiptLines");
            migrationBuilder.DropTable("PurchaseReceipts");
            migrationBuilder.DropTable("PurchaseOrderLines");
            migrationBuilder.DropTable("PurchaseOrders");
            migrationBuilder.DropTable("InventoryMovements");
            migrationBuilder.DropTable("InventoryBalances");
            migrationBuilder.DropTable("Warehouses");
            migrationBuilder.DropTable("Products");
            migrationBuilder.DropColumn("OpeningBalance", "BankAccounts");
            migrationBuilder.DropColumn("CurrentBalance", "BankAccounts");
            migrationBuilder.DropColumn("Notes", "BusinessPartners");
            migrationBuilder.DropColumn("Address", "BusinessPartners");
            migrationBuilder.DropColumn("Email", "BusinessPartners");
            migrationBuilder.DropColumn("Phone", "BusinessPartners");
            migrationBuilder.DropColumn("TaxNumber", "BusinessPartners");
        }
    }
}
