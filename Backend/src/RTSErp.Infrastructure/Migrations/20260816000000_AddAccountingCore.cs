using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RTSErp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Currencies ────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id            = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code          = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name          = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol        = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExchangeRate  = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsBaseCurrency= table.Column<bool>(type: "bit", nullable: false),
                    IsActive      = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt     = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy     = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt    = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy    = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted     = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Currencies", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_Currencies_Code", table: "Currencies", column: "Code", unique: true);

            // ── Accounts (Chart of Accounts) ──────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id          = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code        = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name        = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr      = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    IsGroup     = table.Column<bool>(type: "bit", nullable: false),
                    IsActive    = table.Column<bool>(type: "bit", nullable: false),
                    ParentId    = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrencyId  = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt   = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy   = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt  = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy  = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted   = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(name: "FK_Accounts_Accounts_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Accounts", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_Accounts_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_Accounts_Code", table: "Accounts", column: "Code",
                unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex(name: "IX_Accounts_ParentId",  table: "Accounts", column: "ParentId");
            migrationBuilder.CreateIndex(name: "IX_Accounts_CurrencyId", table: "Accounts", column: "CurrencyId");

            // ── FiscalPeriods ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "FiscalPeriods",
                columns: table => new
                {
                    Id        = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name      = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate   = table.Column<DateOnly>(type: "date", nullable: false),
                    Status    = table.Column<int>(type: "int", nullable: false),
                    CreatedAt  = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy  = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted  = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_FiscalPeriods", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_FiscalPeriods_StartDate_EndDate",
                table: "FiscalPeriods", columns: new[] { "StartDate", "EndDate" });

            // ── JournalEntries ────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id                 = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryNumber        = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EntryDate          = table.Column<DateOnly>(type: "date", nullable: false),
                    Description        = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReferenceType      = table.Column<int>(type: "int", nullable: false),
                    ReferenceId        = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReferenceNumber    = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status             = table.Column<int>(type: "int", nullable: false),
                    CurrencyId         = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExchangeRate       = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FiscalPeriodId     = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReversedByEntryId  = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversesEntryId    = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt          = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy          = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt         = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy         = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted          = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(name: "FK_JournalEntries_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_JournalEntries_FiscalPeriods_FiscalPeriodId",
                        column: x => x.FiscalPeriodId,
                        principalTable: "FiscalPeriods", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_JournalEntries_JournalEntries_ReversedByEntryId",
                        column: x => x.ReversedByEntryId,
                        principalTable: "JournalEntries", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_JournalEntries_JournalEntries_ReversesEntryId",
                        column: x => x.ReversesEntryId,
                        principalTable: "JournalEntries", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_JournalEntries_EntryNumber", table: "JournalEntries",
                column: "EntryNumber", unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex(name: "IX_JournalEntries_EntryDate",       table: "JournalEntries", column: "EntryDate");
            migrationBuilder.CreateIndex(name: "IX_JournalEntries_Status",          table: "JournalEntries", column: "Status");
            migrationBuilder.CreateIndex(name: "IX_JournalEntries_CurrencyId",      table: "JournalEntries", column: "CurrencyId");
            migrationBuilder.CreateIndex(name: "IX_JournalEntries_FiscalPeriodId",  table: "JournalEntries", column: "FiscalPeriodId");
            migrationBuilder.CreateIndex(name: "IX_JournalEntries_ReversedByEntryId",  table: "JournalEntries", column: "ReversedByEntryId");
            migrationBuilder.CreateIndex(name: "IX_JournalEntries_ReversesEntryId",    table: "JournalEntries", column: "ReversesEntryId");
            migrationBuilder.CreateIndex(name: "IX_JournalEntries_ReferenceType_ReferenceId",
                table: "JournalEntries", columns: new[] { "ReferenceType", "ReferenceId" });

            // ── JournalEntryLines ─────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                columns: table => new
                {
                    Id             = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId      = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Debit          = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Credit         = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DebitBase      = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreditBase     = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CurrencyId     = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExchangeRate   = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Description    = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder      = table.Column<int>(type: "int", nullable: false),
                    CreatedAt      = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy      = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt     = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy     = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted      = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.ForeignKey(name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries", principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(name: "FK_JournalEntryLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_JournalEntryLines_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_JournalEntryLines_JournalEntryId", table: "JournalEntryLines", column: "JournalEntryId");
            migrationBuilder.CreateIndex(name: "IX_JournalEntryLines_AccountId",      table: "JournalEntryLines", column: "AccountId");
            migrationBuilder.CreateIndex(name: "IX_JournalEntryLines_CurrencyId",     table: "JournalEntryLines", column: "CurrencyId");

            // ── TaxRates ──────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "TaxRates",
                columns: table => new
                {
                    Id                   = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code                 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name                 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rate                 = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    IsActive             = table.Column<bool>(type: "bit", nullable: false),
                    InputTaxAccountId    = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutputTaxAccountId   = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt            = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy            = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt           = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy           = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted            = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                    table.ForeignKey(name: "FK_TaxRates_Accounts_InputTaxAccountId",
                        column: x => x.InputTaxAccountId,
                        principalTable: "Accounts", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_TaxRates_Accounts_OutputTaxAccountId",
                        column: x => x.OutputTaxAccountId,
                        principalTable: "Accounts", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_TaxRates_Code", table: "TaxRates", column: "Code",
                unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex(name: "IX_TaxRates_InputTaxAccountId",  table: "TaxRates", column: "InputTaxAccountId");
            migrationBuilder.CreateIndex(name: "IX_TaxRates_OutputTaxAccountId", table: "TaxRates", column: "OutputTaxAccountId");

            // ── CommissionRates ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "CommissionRates",
                columns: table => new
                {
                    Id                          = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name                        = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rate                        = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    IsDefault                   = table.Column<bool>(type: "bit", nullable: false),
                    IsActive                    = table.Column<bool>(type: "bit", nullable: false),
                    CommissionExpenseAccountId  = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommissionPayableAccountId  = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt                   = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy                   = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt                  = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy                  = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted                   = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRates", x => x.Id);
                    table.ForeignKey(name: "FK_CommissionRates_Accounts_CommissionExpenseAccountId",
                        column: x => x.CommissionExpenseAccountId,
                        principalTable: "Accounts", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_CommissionRates_Accounts_CommissionPayableAccountId",
                        column: x => x.CommissionPayableAccountId,
                        principalTable: "Accounts", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_CommissionRates_CommissionExpenseAccountId", table: "CommissionRates", column: "CommissionExpenseAccountId");
            migrationBuilder.CreateIndex(name: "IX_CommissionRates_CommissionPayableAccountId", table: "CommissionRates", column: "CommissionPayableAccountId");

            // ── BusinessPartners ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "BusinessPartners",
                columns: table => new
                {
                    Id                   = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code                 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name                 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr               = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PartnerType          = table.Column<int>(type: "int", nullable: false),
                    IsActive             = table.Column<bool>(type: "bit", nullable: false),
                    ReceivableAccountId  = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayableAccountId     = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrencyId           = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreditLimit          = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CreatedAt            = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy            = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt           = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy           = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted            = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessPartners", x => x.Id);
                    table.ForeignKey(name: "FK_BusinessPartners_Accounts_ReceivableAccountId",
                        column: x => x.ReceivableAccountId,
                        principalTable: "Accounts", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_BusinessPartners_Accounts_PayableAccountId",
                        column: x => x.PayableAccountId,
                        principalTable: "Accounts", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_BusinessPartners_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_BusinessPartners_Code", table: "BusinessPartners", column: "Code",
                unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex(name: "IX_BusinessPartners_ReceivableAccountId", table: "BusinessPartners", column: "ReceivableAccountId");
            migrationBuilder.CreateIndex(name: "IX_BusinessPartners_PayableAccountId",    table: "BusinessPartners", column: "PayableAccountId");
            migrationBuilder.CreateIndex(name: "IX_BusinessPartners_CurrencyId",          table: "BusinessPartners", column: "CurrencyId");

            // ── BankAccounts ──────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id            = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code          = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name          = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountType   = table.Column<int>(type: "int", nullable: false),
                    BankName      = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IBAN          = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    IsActive      = table.Column<bool>(type: "bit", nullable: false),
                    GlAccountId   = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId    = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt     = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy     = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt    = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy    = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted     = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.ForeignKey(name: "FK_BankAccounts_Accounts_GlAccountId",
                        column: x => x.GlAccountId,
                        principalTable: "Accounts", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_BankAccounts_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies", principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_BankAccounts_Code", table: "BankAccounts", column: "Code",
                unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex(name: "IX_BankAccounts_GlAccountId",  table: "BankAccounts", column: "GlAccountId");
            migrationBuilder.CreateIndex(name: "IX_BankAccounts_CurrencyId",   table: "BankAccounts", column: "CurrencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BankAccounts");
            migrationBuilder.DropTable(name: "BusinessPartners");
            migrationBuilder.DropTable(name: "CommissionRates");
            migrationBuilder.DropTable(name: "TaxRates");
            migrationBuilder.DropTable(name: "JournalEntryLines");
            migrationBuilder.DropTable(name: "JournalEntries");
            migrationBuilder.DropTable(name: "FiscalPeriods");
            migrationBuilder.DropTable(name: "Accounts");
            migrationBuilder.DropTable(name: "Currencies");
        }
    }
}
