using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RTSErp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Identity ──────────────────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table => table.PrimaryKey("PK_AspNetRoles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    FirstName = table.Column<string>(maxLength: 100, nullable: false),
                    LastName = table.Column<string>(maxLength: 100, nullable: false),
                    AvatarUrl = table.Column<string>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    EmployeeId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                    UserName = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_AspNetUsers", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Code = table.Column<string>(maxLength: 100, nullable: false),
                    Module = table.Column<string>(maxLength: 50, nullable: false),
                    Description = table.Column<string>(maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table => table.PrimaryKey("PK_Permissions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    FullName = table.Column<string>(maxLength: 200, nullable: false),
                    JobTitle = table.Column<string>(maxLength: 100, nullable: true),
                    Department = table.Column<string>(maxLength: 100, nullable: true),
                    HireDate = table.Column<DateOnly>(nullable: false),
                    UserId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey("FK_Employees_AspNetUsers_UserId", x => x.UserId,
                        "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    RoleId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey("FK_AspNetUserRoles_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_AspNetUserRoles_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey("FK_AspNetUserClaims_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey("FK_AspNetUserLogins_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey("FK_AspNetUserTokens_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey("FK_AspNetRoleClaims_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(nullable: false),
                    PermissionId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey("FK_RolePermissions_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_RolePermissions_Permissions_PermissionId", x => x.PermissionId, "Permissions", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Token = table.Column<string>(maxLength: 500, nullable: false),
                    UserId = table.Column<Guid>(nullable: false),
                    ExpiresAt = table.Column<DateTime>(nullable: false),
                    CreatedByIp = table.Column<string>(maxLength: 50, nullable: true),
                    RevokedAt = table.Column<DateTime>(nullable: true),
                    RevokedByIp = table.Column<string>(maxLength: 50, nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey("FK_RefreshTokens_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_RefreshTokens_RefreshTokens_ReplacedByTokenId", x => x.ReplacedByTokenId, "RefreshTokens", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── Accounting ────────────────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Code = table.Column<string>(maxLength: 5, nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(maxLength: 5, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    IsBaseCurrency = table.Column<bool>(nullable: false),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table => table.PrimaryKey("PK_Currencies", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FiscalPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(nullable: false),
                    EndDate = table.Column<DateOnly>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    ClosedAt = table.Column<DateTime>(nullable: true),
                    ClosedBy = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table => table.PrimaryKey("PK_FiscalPeriods", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Code = table.Column<string>(maxLength: 20, nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(maxLength: 200, nullable: true),
                    AccountType = table.Column<int>(nullable: false),
                    IsGroup = table.Column<bool>(nullable: false),
                    ParentId = table.Column<Guid>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey("FK_Accounts_Accounts_ParentId", x => x.ParentId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Code = table.Column<string>(maxLength: 20, nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    InputTaxAccountId = table.Column<Guid>(nullable: true),
                    OutputTaxAccountId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                    table.ForeignKey("FK_TaxRates_Accounts_InputTaxAccountId", x => x.InputTaxAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaxRates_Accounts_OutputTaxAccountId", x => x.OutputTaxAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    IsDefault = table.Column<bool>(nullable: false),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    CommissionExpenseAccountId = table.Column<Guid>(nullable: true),
                    CommissionPayableAccountId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRates", x => x.Id);
                    table.ForeignKey("FK_CommissionRates_Accounts_CommissionExpenseAccountId", x => x.CommissionExpenseAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CommissionRates_Accounts_CommissionPayableAccountId", x => x.CommissionPayableAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessPartners",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Code = table.Column<string>(maxLength: 30, nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(maxLength: 200, nullable: true),
                    PartnerType = table.Column<int>(nullable: false),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    TaxNumber = table.Column<string>(maxLength: 50, nullable: true),
                    Phone = table.Column<string>(maxLength: 30, nullable: true),
                    Email = table.Column<string>(maxLength: 200, nullable: true),
                    Address = table.Column<string>(maxLength: 500, nullable: true),
                    Notes = table.Column<string>(maxLength: 1000, nullable: true),
                    ReceivableAccountId = table.Column<Guid>(nullable: true),
                    PayableAccountId = table.Column<Guid>(nullable: true),
                    CurrencyId = table.Column<Guid>(nullable: true),
                    CreditLimit = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessPartners", x => x.Id);
                    table.ForeignKey("FK_BusinessPartners_Accounts_ReceivableAccountId", x => x.ReceivableAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_BusinessPartners_Accounts_PayableAccountId", x => x.PayableAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_BusinessPartners_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Code = table.Column<string>(maxLength: 30, nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    AccountType = table.Column<int>(nullable: false),
                    BankName = table.Column<string>(maxLength: 200, nullable: true),
                    AccountNumber = table.Column<string>(maxLength: 50, nullable: true),
                    IBAN = table.Column<string>(maxLength: 34, nullable: true),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    OpeningBalance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    GlAccountId = table.Column<Guid>(nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.ForeignKey("FK_BankAccounts_Accounts_GlAccountId", x => x.GlAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_BankAccounts_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EntryNumber = table.Column<string>(maxLength: 30, nullable: false),
                    EntryDate = table.Column<DateOnly>(nullable: false),
                    Description = table.Column<string>(maxLength: 1000, nullable: false),
                    Status = table.Column<int>(nullable: false),
                    ReferenceType = table.Column<int>(nullable: false),
                    ReferenceId = table.Column<Guid>(nullable: true),
                    ReferenceNumber = table.Column<string>(maxLength: 50, nullable: true),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    TotalDebit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalCredit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PostedAt = table.Column<DateTime>(nullable: true),
                    PostedBy = table.Column<Guid>(nullable: true),
                    ReversedEntryId = table.Column<Guid>(nullable: true),
                    FiscalPeriodId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey("FK_JournalEntries_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_JournalEntries_FiscalPeriods_FiscalPeriodId", x => x.FiscalPeriodId, "FiscalPeriods", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    JournalEntryId = table.Column<Guid>(nullable: false),
                    AccountId = table.Column<Guid>(nullable: false),
                    Description = table.Column<string>(maxLength: 500, nullable: true),
                    Debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.ForeignKey("FK_JournalEntryLines_JournalEntries_JournalEntryId", x => x.JournalEntryId, "JournalEntries", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_JournalEntryLines_Accounts_AccountId", x => x.AccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── Operational ───────────────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Code = table.Column<string>(maxLength: 20, nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    Location = table.Column<string>(maxLength: 500, nullable: true),
                    Notes = table.Column<string>(maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table => table.PrimaryKey("PK_Warehouses", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SKU = table.Column<string>(maxLength: 50, nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    Description = table.Column<string>(maxLength: 1000, nullable: true),
                    Category = table.Column<string>(maxLength: 100, nullable: true),
                    Unit = table.Column<string>(maxLength: 30, nullable: false),
                    Barcode = table.Column<string>(maxLength: 50, nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SalesPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    TaxRateId = table.Column<Guid>(nullable: true),
                    InventoryAccountId = table.Column<Guid>(nullable: true),
                    COGSAccountId = table.Column<Guid>(nullable: true),
                    SalesAccountId = table.Column<Guid>(nullable: true),
                    PurchaseAccountId = table.Column<Guid>(nullable: true),
                    MinimumStock = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey("FK_Products_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Products_TaxRates_TaxRateId", x => x.TaxRateId, "TaxRates", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Products_Accounts_InventoryAccountId", x => x.InventoryAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Products_Accounts_COGSAccountId", x => x.COGSAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Products_Accounts_SalesAccountId", x => x.SalesAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Products_Accounts_PurchaseAccountId", x => x.PurchaseAccountId, "Accounts", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProductId = table.Column<Guid>(nullable: false),
                    WarehouseId = table.Column<Guid>(nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AverageCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBalances", x => x.Id);
                    table.ForeignKey("FK_InventoryBalances_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_InventoryBalances_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProductId = table.Column<Guid>(nullable: false),
                    WarehouseId = table.Column<Guid>(nullable: false),
                    MovementType = table.Column<int>(nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MovementDate = table.Column<DateOnly>(nullable: false),
                    Notes = table.Column<string>(maxLength: 500, nullable: true),
                    ReferenceType = table.Column<string>(maxLength: 50, nullable: true),
                    ReferenceId = table.Column<Guid>(nullable: true),
                    ReferenceNumber = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    table.ForeignKey("FK_InventoryMovements_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_InventoryMovements_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                });

            // Purchase cycle tables
            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PONumber = table.Column<string>(maxLength: 30, nullable: false),
                    SupplierId = table.Column<Guid>(nullable: false),
                    OrderDate = table.Column<DateOnly>(nullable: false),
                    Notes = table.Column<string>(maxLength: 1000, nullable: true),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    WarehouseId = table.Column<Guid>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey("FK_PurchaseOrders_BusinessPartners_SupplierId", x => x.SupplierId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PurchaseOrders_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PurchaseOrders_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PurchaseOrderId = table.Column<Guid>(nullable: false),
                    ProductId = table.Column<Guid>(nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.ForeignKey("FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId", x => x.PurchaseOrderId, "PurchaseOrders", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_PurchaseOrderLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ReceiptNumber = table.Column<string>(maxLength: 30, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(nullable: false),
                    SupplierId = table.Column<Guid>(nullable: false),
                    WarehouseId = table.Column<Guid>(nullable: false),
                    ReceiptDate = table.Column<DateOnly>(nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Notes = table.Column<string>(maxLength: 1000, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    JournalEntryId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceipts", x => x.Id);
                    table.ForeignKey("FK_PurchaseReceipts_PurchaseOrders_PurchaseOrderId", x => x.PurchaseOrderId, "PurchaseOrders", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PurchaseReceipts_BusinessPartners_SupplierId", x => x.SupplierId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PurchaseReceipts_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PurchaseReceipts_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceiptLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PurchaseReceiptId = table.Column<Guid>(nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(nullable: false),
                    ProductId = table.Column<Guid>(nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceiptLines", x => x.Id);
                    table.ForeignKey("FK_PurchaseReceiptLines_PurchaseReceipts_PurchaseReceiptId", x => x.PurchaseReceiptId, "PurchaseReceipts", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_PurchaseReceiptLines_PurchaseOrderLines_PurchaseOrderLineId", x => x.PurchaseOrderLineId, "PurchaseOrderLines", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PurchaseReceiptLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    InvoiceNumber = table.Column<string>(maxLength: 30, nullable: false),
                    SupplierInvoiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                    SupplierId = table.Column<Guid>(nullable: false),
                    InvoiceDate = table.Column<DateOnly>(nullable: false),
                    DueDate = table.Column<DateOnly>(nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    PurchaseReceiptId = table.Column<Guid>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    JournalEntryId = table.Column<Guid>(nullable: true),
                    EInvoiceUUID = table.Column<string>(maxLength: 100, nullable: true),
                    EInvoiceStatus = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierInvoices", x => x.Id);
                    table.ForeignKey("FK_SupplierInvoices_BusinessPartners_SupplierId", x => x.SupplierId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SupplierInvoices_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SupplierInvoices_PurchaseReceipts_PurchaseReceiptId", x => x.PurchaseReceiptId, "PurchaseReceipts", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SupplierInvoiceId = table.Column<Guid>(nullable: false),
                    ProductId = table.Column<Guid>(nullable: false),
                    Description = table.Column<string>(maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierInvoiceLines", x => x.Id);
                    table.ForeignKey("FK_SupplierInvoiceLines_SupplierInvoices_SupplierInvoiceId", x => x.SupplierInvoiceId, "SupplierInvoices", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_SupplierInvoiceLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            // Sales cycle tables
            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SONumber = table.Column<string>(maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(nullable: false),
                    OrderDate = table.Column<DateOnly>(nullable: false),
                    Notes = table.Column<string>(maxLength: 1000, nullable: true),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    WarehouseId = table.Column<Guid>(nullable: false),
                    SalespersonId = table.Column<Guid>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey("FK_SalesOrders_BusinessPartners_CustomerId", x => x.CustomerId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SalesOrders_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SalesOrders_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SalesOrders_AspNetUsers_SalespersonId", x => x.SalespersonId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SalesOrderId = table.Column<Guid>(nullable: false),
                    ProductId = table.Column<Guid>(nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderLines", x => x.Id);
                    table.ForeignKey("FK_SalesOrderLines_SalesOrders_SalesOrderId", x => x.SalesOrderId, "SalesOrders", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_SalesOrderLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DeliveryNumber = table.Column<string>(maxLength: 30, nullable: false),
                    SalesOrderId = table.Column<Guid>(nullable: false),
                    CustomerId = table.Column<Guid>(nullable: false),
                    WarehouseId = table.Column<Guid>(nullable: false),
                    DeliveryDate = table.Column<DateOnly>(nullable: false),
                    Notes = table.Column<string>(maxLength: 1000, nullable: true),
                    TotalCOGS = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    JournalEntryId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesDeliveries", x => x.Id);
                    table.ForeignKey("FK_SalesDeliveries_SalesOrders_SalesOrderId", x => x.SalesOrderId, "SalesOrders", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SalesDeliveries_BusinessPartners_CustomerId", x => x.CustomerId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SalesDeliveries_Warehouses_WarehouseId", x => x.WarehouseId, "Warehouses", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesDeliveryLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SalesDeliveryId = table.Column<Guid>(nullable: false),
                    SalesOrderLineId = table.Column<Guid>(nullable: false),
                    ProductId = table.Column<Guid>(nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesDeliveryLines", x => x.Id);
                    table.ForeignKey("FK_SalesDeliveryLines_SalesDeliveries_SalesDeliveryId", x => x.SalesDeliveryId, "SalesDeliveries", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_SalesDeliveryLines_SalesOrderLines_SalesOrderLineId", x => x.SalesOrderLineId, "SalesOrderLines", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SalesDeliveryLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    InvoiceNumber = table.Column<string>(maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(nullable: false),
                    InvoiceDate = table.Column<DateOnly>(nullable: false),
                    DueDate = table.Column<DateOnly>(nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    SalespersonId = table.Column<Guid>(nullable: true),
                    SalesOrderId = table.Column<Guid>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    InvoiceType = table.Column<int>(nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    JournalEntryId = table.Column<Guid>(nullable: true),
                    TaxRegistrationNumber = table.Column<string>(maxLength: 50, nullable: true),
                    EInvoiceUUID = table.Column<string>(maxLength: 100, nullable: true),
                    EInvoiceStatus = table.Column<int>(nullable: false),
                    EInvoiceSubmissionDate = table.Column<DateTime>(nullable: true),
                    ExternalInvoiceId = table.Column<string>(maxLength: 100, nullable: true),
                    ExternalStatus = table.Column<string>(maxLength: 50, nullable: true),
                    QRCode = table.Column<string>(nullable: true),
                    CancellationStatus = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerInvoices", x => x.Id);
                    table.ForeignKey("FK_CustomerInvoices_BusinessPartners_CustomerId", x => x.CustomerId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CustomerInvoices_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CustomerInvoices_AspNetUsers_SalespersonId", x => x.SalespersonId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CustomerInvoices_SalesOrders_SalesOrderId", x => x.SalesOrderId, "SalesOrders", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CustomerInvoiceId = table.Column<Guid>(nullable: false),
                    ProductId = table.Column<Guid>(nullable: false),
                    Description = table.Column<string>(maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerInvoiceLines", x => x.Id);
                    table.ForeignKey("FK_CustomerInvoiceLines_CustomerInvoices_CustomerInvoiceId", x => x.CustomerInvoiceId, "CustomerInvoices", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_CustomerInvoiceLines_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                });

            // Cheques
            migrationBuilder.CreateTable(
                name: "Cheques",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ChequeNumber = table.Column<string>(maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(nullable: false),
                    BankName = table.Column<string>(maxLength: 200, nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AmountBase = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IssueDate = table.Column<DateOnly>(nullable: false),
                    DueDate = table.Column<DateOnly>(nullable: false),
                    ReceivedDate = table.Column<DateOnly>(nullable: false),
                    BankAccountId = table.Column<Guid>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    Notes = table.Column<string>(maxLength: 500, nullable: true),
                    ReceiptJournalEntryId = table.Column<Guid>(nullable: true),
                    DepositJournalEntryId = table.Column<Guid>(nullable: true),
                    BounceJournalEntryId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cheques", x => x.Id);
                    table.ForeignKey("FK_Cheques_BusinessPartners_CustomerId", x => x.CustomerId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Cheques_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Cheques_BankAccounts_BankAccountId", x => x.BankAccountId, "BankAccounts", "Id", onDelete: ReferentialAction.Restrict);
                });

            // Payments
            migrationBuilder.CreateTable(
                name: "CustomerPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PaymentNumber = table.Column<string>(maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(nullable: false),
                    PaymentDate = table.Column<DateOnly>(nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AmountBase = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PaymentMethod = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    BankAccountId = table.Column<Guid>(nullable: true),
                    ChequeId = table.Column<Guid>(nullable: true),
                    Notes = table.Column<string>(maxLength: 500, nullable: true),
                    JournalEntryId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPayments", x => x.Id);
                    table.ForeignKey("FK_CustomerPayments_BusinessPartners_CustomerId", x => x.CustomerId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CustomerPayments_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CustomerPayments_BankAccounts_BankAccountId", x => x.BankAccountId, "BankAccounts", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CustomerPayments_Cheques_ChequeId", x => x.ChequeId, "Cheques", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPaymentInvoices",
                columns: table => new
                {
                    CustomerPaymentId = table.Column<Guid>(nullable: false),
                    CustomerInvoiceId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPaymentInvoices", x => new { x.CustomerPaymentId, x.CustomerInvoiceId });
                    table.ForeignKey("FK_CustomerPaymentInvoices_CustomerPayments_CustomerPaymentId", x => x.CustomerPaymentId, "CustomerPayments", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_CustomerPaymentInvoices_CustomerInvoices_CustomerInvoiceId", x => x.CustomerInvoiceId, "CustomerInvoices", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PaymentNumber = table.Column<string>(maxLength: 30, nullable: false),
                    SupplierId = table.Column<Guid>(nullable: false),
                    PaymentDate = table.Column<DateOnly>(nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AmountBase = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PaymentMethod = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    BankAccountId = table.Column<Guid>(nullable: true),
                    Notes = table.Column<string>(maxLength: 500, nullable: true),
                    JournalEntryId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPayments", x => x.Id);
                    table.ForeignKey("FK_SupplierPayments_BusinessPartners_SupplierId", x => x.SupplierId, "BusinessPartners", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SupplierPayments_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SupplierPayments_BankAccounts_BankAccountId", x => x.BankAccountId, "BankAccounts", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPaymentInvoices",
                columns: table => new
                {
                    SupplierPaymentId = table.Column<Guid>(nullable: false),
                    SupplierInvoiceId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPaymentInvoices", x => new { x.SupplierPaymentId, x.SupplierInvoiceId });
                    table.ForeignKey("FK_SupplierPaymentInvoices_SupplierPayments_SupplierPaymentId", x => x.SupplierPaymentId, "SupplierPayments", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_SupplierPaymentInvoices_SupplierInvoices_SupplierInvoiceId", x => x.SupplierInvoiceId, "SupplierInvoices", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TransactionNumber = table.Column<string>(maxLength: 30, nullable: false),
                    BankAccountId = table.Column<Guid>(nullable: false),
                    TransactionType = table.Column<int>(nullable: false),
                    TransactionDate = table.Column<DateOnly>(nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AmountBase = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Description = table.Column<string>(maxLength: 500, nullable: true),
                    Reference = table.Column<string>(maxLength: 100, nullable: true),
                    DestinationBankAccountId = table.Column<Guid>(nullable: true),
                    JournalEntryId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransactions", x => x.Id);
                    table.ForeignKey("FK_BankTransactions_BankAccounts_BankAccountId", x => x.BankAccountId, "BankAccounts", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_BankTransactions_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_BankTransactions_BankAccounts_DestinationBankAccountId", x => x.DestinationBankAccountId, "BankAccounts", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesCommissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SalespersonId = table.Column<Guid>(nullable: true),
                    SalespersonName = table.Column<string>(maxLength: 200, nullable: false),
                    CommissionRateId = table.Column<Guid>(nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    BaseSalesAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Status = table.Column<int>(nullable: false),
                    SalesOrderId = table.Column<Guid>(nullable: true),
                    CustomerInvoiceId = table.Column<Guid>(nullable: true),
                    JournalEntryId = table.Column<Guid>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    ModifiedAt = table.Column<DateTime>(nullable: true),
                    ModifiedBy = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesCommissions", x => x.Id);
                    table.ForeignKey("FK_SalesCommissions_CommissionRates_CommissionRateId", x => x.CommissionRateId, "CommissionRates", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SalesCommissions_Currencies_CurrencyId", x => x.CurrencyId, "Currencies", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SalesCommissions_SalesOrders_SalesOrderId", x => x.SalesOrderId, "SalesOrders", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SalesCommissions_CustomerInvoices_CustomerInvoiceId", x => x.CustomerInvoiceId, "CustomerInvoices", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── Indexes ───────────────────────────────────────────────────────

            migrationBuilder.CreateIndex("IX_AspNetRoles_NormalizedName", "AspNetRoles", "NormalizedName", unique: true);
            migrationBuilder.CreateIndex("IX_AspNetUsers_NormalizedEmail", "AspNetUsers", "NormalizedEmail");
            migrationBuilder.CreateIndex("IX_AspNetUsers_NormalizedUserName", "AspNetUsers", "NormalizedUserName", unique: true);
            migrationBuilder.CreateIndex("IX_AspNetUserRoles_RoleId", "AspNetUserRoles", "RoleId");
            migrationBuilder.CreateIndex("IX_Accounts_Code", "Accounts", "Code", unique: true);
            migrationBuilder.CreateIndex("IX_Currencies_Code", "Currencies", "Code", unique: true);
            migrationBuilder.CreateIndex("IX_JournalEntries_EntryNumber", "JournalEntries", "EntryNumber", unique: true);
            migrationBuilder.CreateIndex("IX_JournalEntries_EntryDate", "JournalEntries", "EntryDate");
            migrationBuilder.CreateIndex("IX_JournalEntryLines_JournalEntryId", "JournalEntryLines", "JournalEntryId");
            migrationBuilder.CreateIndex("IX_RefreshTokens_Token", "RefreshTokens", "Token");
            migrationBuilder.CreateIndex("IX_RefreshTokens_UserId_RevokedAt", "RefreshTokens", new[] { "UserId", "RevokedAt" });
            migrationBuilder.CreateIndex("IX_Employees_UserId", "Employees", "UserId");
            migrationBuilder.CreateIndex("IX_BusinessPartners_Code", "BusinessPartners", "Code");
            migrationBuilder.CreateIndex("IX_Products_SKU", "Products", "SKU",
                unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_Warehouses_Code", "Warehouses", "Code",
                unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_InventoryBalances_ProductId_WarehouseId", "InventoryBalances", new[] { "ProductId", "WarehouseId" }, unique: true);
            migrationBuilder.CreateIndex("IX_InventoryMovements_ProductId", "InventoryMovements", "ProductId");
            migrationBuilder.CreateIndex("IX_InventoryMovements_WarehouseId", "InventoryMovements", "WarehouseId");
            migrationBuilder.CreateIndex("IX_PurchaseOrders_PONumber", "PurchaseOrders", "PONumber",
                unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_PurchaseReceipts_ReceiptNumber", "PurchaseReceipts", "ReceiptNumber",
                unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_SupplierInvoices_InvoiceNumber", "SupplierInvoices", "InvoiceNumber",
                unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_SalesOrders_SONumber", "SalesOrders", "SONumber",
                unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_SalesDeliveries_DeliveryNumber", "SalesDeliveries", "DeliveryNumber",
                unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_CustomerInvoices_InvoiceNumber", "CustomerInvoices", "InvoiceNumber",
                unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_CustomerPayments_PaymentNumber", "CustomerPayments", "PaymentNumber",
                unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_SupplierPayments_PaymentNumber", "SupplierPayments", "PaymentNumber",
                unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_BankTransactions_TransactionNumber", "BankTransactions", "TransactionNumber",
                unique: true, filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop in reverse dependency order
            migrationBuilder.DropTable("SalesCommissions");
            migrationBuilder.DropTable("BankTransactions");
            migrationBuilder.DropTable("SupplierPaymentInvoices");
            migrationBuilder.DropTable("CustomerPaymentInvoices");
            migrationBuilder.DropTable("SupplierPayments");
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
            migrationBuilder.DropTable("Products");
            migrationBuilder.DropTable("Warehouses");
            migrationBuilder.DropTable("JournalEntryLines");
            migrationBuilder.DropTable("JournalEntries");
            migrationBuilder.DropTable("BankAccounts");
            migrationBuilder.DropTable("BusinessPartners");
            migrationBuilder.DropTable("CommissionRates");
            migrationBuilder.DropTable("TaxRates");
            migrationBuilder.DropTable("Accounts");
            migrationBuilder.DropTable("FiscalPeriods");
            migrationBuilder.DropTable("Currencies");
            migrationBuilder.DropTable("RolePermissions");
            migrationBuilder.DropTable("RefreshTokens");
            migrationBuilder.DropTable("AspNetUserTokens");
            migrationBuilder.DropTable("AspNetUserLogins");
            migrationBuilder.DropTable("AspNetUserClaims");
            migrationBuilder.DropTable("AspNetRoleClaims");
            migrationBuilder.DropTable("AspNetUserRoles");
            migrationBuilder.DropTable("Employees");
            migrationBuilder.DropTable("Permissions");
            migrationBuilder.DropTable("AspNetUsers");
            migrationBuilder.DropTable("AspNetRoles");
        }
    }
}
