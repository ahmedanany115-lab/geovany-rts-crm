using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using RTSErp.Domain.Entities.Identity;
using RTSErp.Infrastructure.Persistence;
using RTSErp.Infrastructure.Persistence.Seed;

namespace RTSErp.Infrastructure.Services;

/// <summary>
/// Runs after the app starts listening. Creates all tables via raw NpgsqlConnection
/// (bypassing EF entirely for DDL so EF model state never interferes), then seeds
/// the admin user via ASP.NET Identity. Retries every 30 s on any failure.
/// </summary>
public sealed class DatabaseSeedingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseSeedingService> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseSeedingService(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseSeedingService> logger,
        IConfiguration configuration)
    {
        _scopeFactory   = scopeFactory;
        _logger         = logger;
        _configuration  = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Let the app finish starting so Railway health-check passes immediately
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var cs = GetConnectionString();
                _logger.LogInformation("[Seed] Opening direct Npgsql connection for DDL...");
                await CreateSchemaDirectAsync(cs, ct);
                _logger.LogInformation("[Seed] Schema OK.");

                // Step 2: Seed data using EF / ASP.NET Identity
                using var scope = _scopeFactory.CreateScope();
                var db   = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var umgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var rmgr = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

                _logger.LogInformation("[Seed] Running DbSeeder...");
                await DbSeeder.SeedAsync(db, umgr, rmgr, _logger);
                _logger.LogInformation("[Seed] Seeding complete.");
                return; // success — stop retrying
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Seed] Failed: {Msg} — retrying in 30 s.", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
        }
    }

    private string GetConnectionString()
    {
        var raw = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        return DependencyInjection.NormalizePostgresConnectionString(raw);
    }

    /// <summary>
    /// Opens a plain NpgsqlConnection and runs each CREATE TABLE IF NOT EXISTS
    /// individually. No EF involved — immune to model state, query filters,
    /// and interceptor interference.
    /// </summary>
    private async Task CreateSchemaDirectAsync(string connectionString, CancellationToken ct)
    {
        // First check if AspNetUsers already exists — if so, skip DDL entirely
        await using var checkConn = new NpgsqlConnection(connectionString);
        await checkConn.OpenAsync(ct);

        await using var checkCmd = checkConn.CreateCommand();
        checkCmd.CommandText = """
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema = 'public'
            AND table_name = 'AspNetUsers'
            """;
        var exists = Convert.ToInt64(await checkCmd.ExecuteScalarAsync(ct)) > 0;

        if (exists)
        {
            _logger.LogInformation("[Seed] Tables already exist — skipping DDL.");
            return;
        }

        _logger.LogInformation("[Seed] Tables not found — running CREATE TABLE IF NOT EXISTS for all tables...");

        // Use a single open connection for all DDL — one statement at a time
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var statements = SchemaStatements().ToList();
        _logger.LogInformation("[Seed] Running {Count} DDL statements.", statements.Count);

        for (var i = 0; i < statements.Count; i++)
        {
            var sql = statements[i];
            var label = sql.Length > 80 ? sql[..80].Replace('\n', ' ').Trim() : sql.Replace('\n', ' ').Trim();
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText    = sql;
                cmd.CommandTimeout = 60;
                await cmd.ExecuteNonQueryAsync(ct);
                _logger.LogDebug("[Schema] {N}/{Total} OK: {Label}", i + 1, statements.Count, label);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Schema] FAILED statement {N}/{Total}: {Label}", i + 1, statements.Count, label);
                throw;
            }
        }

        _logger.LogInformation("[Seed] All DDL statements completed successfully.");
    }

    private static IEnumerable<string> SchemaStatements()
    {
        // ── Identity ──────────────────────────────────────────────────────────
        yield return """
            CREATE TABLE IF NOT EXISTS "AspNetRoles" (
                "Id"               uuid         NOT NULL DEFAULT gen_random_uuid(),
                "Name"             varchar(256),
                "NormalizedName"   varchar(256),
                "ConcurrencyStamp" text,
                "Description"      text,
                "IsActive"         boolean      NOT NULL DEFAULT true,
                "CreatedAt"        timestamptz  NOT NULL DEFAULT NOW(),
                "CreatedBy"        uuid,
                "ModifiedAt"       timestamptz,
                "ModifiedBy"       uuid,
                "IsDeleted"        boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
            )
            """;

        yield return """CREATE UNIQUE INDEX IF NOT EXISTS "IX_AspNetRoles_NormalizedName" ON "AspNetRoles" ("NormalizedName") WHERE "NormalizedName" IS NOT NULL""";

        yield return """
            CREATE TABLE IF NOT EXISTS "AspNetUsers" (
                "Id"                   uuid         NOT NULL DEFAULT gen_random_uuid(),
                "FirstName"            varchar(100) NOT NULL DEFAULT '',
                "LastName"             varchar(100) NOT NULL DEFAULT '',
                "AvatarUrl"            text,
                "IsActive"             boolean      NOT NULL DEFAULT true,
                "EmployeeId"           uuid,
                "CreatedAt"            timestamptz  NOT NULL DEFAULT NOW(),
                "IsDeleted"            boolean      NOT NULL DEFAULT false,
                "UserName"             varchar(256),
                "NormalizedUserName"   varchar(256),
                "Email"                varchar(256),
                "NormalizedEmail"      varchar(256),
                "EmailConfirmed"       boolean      NOT NULL DEFAULT false,
                "PasswordHash"         text,
                "SecurityStamp"        text,
                "ConcurrencyStamp"     text,
                "PhoneNumber"          text,
                "PhoneNumberConfirmed" boolean      NOT NULL DEFAULT false,
                "TwoFactorEnabled"     boolean      NOT NULL DEFAULT false,
                "LockoutEnd"           timestamptz,
                "LockoutEnabled"       boolean      NOT NULL DEFAULT false,
                "AccessFailedCount"    integer      NOT NULL DEFAULT 0,
                CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
            )
            """;

        yield return """CREATE UNIQUE INDEX IF NOT EXISTS "IX_AspNetUsers_NormalizedUserName" ON "AspNetUsers" ("NormalizedUserName") WHERE "NormalizedUserName" IS NOT NULL""";
        yield return """CREATE INDEX IF NOT EXISTS "IX_AspNetUsers_NormalizedEmail" ON "AspNetUsers" ("NormalizedEmail")""";

        yield return """
            CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
                "Id"         serial NOT NULL,
                "RoleId"     uuid   NOT NULL,
                "ClaimType"  text,
                "ClaimValue" text,
                CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId"
                    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
                "UserId" uuid NOT NULL,
                "RoleId" uuid NOT NULL,
                CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
                CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId"
                    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
                "Id"         serial NOT NULL,
                "UserId"     uuid   NOT NULL,
                "ClaimType"  text,
                "ClaimValue" text,
                CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
                "LoginProvider"       text NOT NULL,
                "ProviderKey"         text NOT NULL,
                "ProviderDisplayName" text,
                "UserId"              uuid NOT NULL,
                CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
                CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
                "UserId"        uuid NOT NULL,
                "LoginProvider" text NOT NULL,
                "Name"          text NOT NULL,
                "Value"         text,
                CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
                CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "Employees" (
                "Id"         uuid         NOT NULL DEFAULT gen_random_uuid(),
                "UserId"     uuid,
                "FullName"   varchar(200) NOT NULL DEFAULT '',
                "JobTitle"   varchar(100) NOT NULL DEFAULT '',
                "Department" varchar(100) NOT NULL DEFAULT '',
                "HireDate"   date         NOT NULL DEFAULT CURRENT_DATE,
                "ManagerId"  uuid,
                "CreatedAt"  timestamptz  NOT NULL DEFAULT NOW(),
                "CreatedBy"  uuid,
                "ModifiedAt" timestamptz,
                "ModifiedBy" uuid,
                "IsDeleted"  boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_Employees" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_Employees_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "Permissions" (
                "Id"          uuid         NOT NULL DEFAULT gen_random_uuid(),
                "Code"        varchar(100) NOT NULL DEFAULT '',
                "Module"      varchar(50)  NOT NULL DEFAULT '',
                "Description" text,
                "CreatedAt"   timestamptz  NOT NULL DEFAULT NOW(),
                "CreatedBy"   uuid,
                "ModifiedAt"  timestamptz,
                "ModifiedBy"  uuid,
                "IsDeleted"   boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_Permissions" PRIMARY KEY ("Id")
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "RolePermissions" (
                "RoleId"       uuid NOT NULL,
                "PermissionId" uuid NOT NULL,
                CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("RoleId", "PermissionId"),
                CONSTRAINT "FK_RolePermissions_AspNetRoles_RoleId"
                    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_RolePermissions_Permissions_PermissionId"
                    FOREIGN KEY ("PermissionId") REFERENCES "Permissions"("Id") ON DELETE CASCADE
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "RefreshTokens" (
                "Id"                uuid         NOT NULL DEFAULT gen_random_uuid(),
                "Token"             varchar(500),
                "TokenHash"         varchar(500),
                "UserId"            uuid         NOT NULL,
                "ExpiresAt"         timestamptz  NOT NULL,
                "CreatedByIp"       varchar(50),
                "RevokedAt"         timestamptz,
                "RevokedByIp"       varchar(50),
                "ReplacedByTokenId" uuid,
                "CreatedAt"         timestamptz  NOT NULL DEFAULT NOW(),
                "IsDeleted"         boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_RefreshTokens_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
            )
            """;

        yield return """CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId_RevokedAt" ON "RefreshTokens"("UserId","RevokedAt")""";

        // ── Accounting ────────────────────────────────────────────────────────
        yield return """
            CREATE TABLE IF NOT EXISTS "Currencies" (
                "Id"             uuid         NOT NULL DEFAULT gen_random_uuid(),
                "Code"           varchar(5)   NOT NULL,
                "Name"           varchar(100) NOT NULL,
                "Symbol"         varchar(5)   NOT NULL,
                "ExchangeRate"   numeric(18,6) NOT NULL DEFAULT 1,
                "IsBaseCurrency" boolean      NOT NULL DEFAULT false,
                "IsActive"       boolean      NOT NULL DEFAULT true,
                "CreatedAt"      timestamptz  NOT NULL DEFAULT NOW(),
                "CreatedBy"      uuid,
                "ModifiedAt"     timestamptz,
                "ModifiedBy"     uuid,
                "IsDeleted"      boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_Currencies" PRIMARY KEY ("Id")
            )
            """;
        yield return """CREATE UNIQUE INDEX IF NOT EXISTS "IX_Currencies_Code" ON "Currencies"("Code")""";

        yield return """
            CREATE TABLE IF NOT EXISTS "FiscalPeriods" (
                "Id"         uuid         NOT NULL DEFAULT gen_random_uuid(),
                "Name"       varchar(100) NOT NULL,
                "StartDate"  date         NOT NULL,
                "EndDate"    date         NOT NULL,
                "Status"     integer      NOT NULL DEFAULT 1,
                "ClosedAt"   timestamptz,
                "ClosedBy"   uuid,
                "CreatedAt"  timestamptz  NOT NULL DEFAULT NOW(),
                "CreatedBy"  uuid,
                "ModifiedAt" timestamptz,
                "ModifiedBy" uuid,
                "IsDeleted"  boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_FiscalPeriods" PRIMARY KEY ("Id")
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "Accounts" (
                "Id"          uuid         NOT NULL DEFAULT gen_random_uuid(),
                "Code"        varchar(20)  NOT NULL,
                "Name"        varchar(200) NOT NULL,
                "NameAr"      varchar(200),
                "AccountType" integer      NOT NULL DEFAULT 1,
                "IsGroup"     boolean      NOT NULL DEFAULT false,
                "ParentId"    uuid,
                "IsActive"    boolean      NOT NULL DEFAULT true,
                "CreatedAt"   timestamptz  NOT NULL DEFAULT NOW(),
                "CreatedBy"   uuid,
                "ModifiedAt"  timestamptz,
                "ModifiedBy"  uuid,
                "IsDeleted"   boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_Accounts" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_Accounts_Accounts_ParentId"
                    FOREIGN KEY ("ParentId") REFERENCES "Accounts"("Id") ON DELETE RESTRICT
            )
            """;
        yield return """CREATE UNIQUE INDEX IF NOT EXISTS "IX_Accounts_Code" ON "Accounts"("Code")""";

        yield return """
            CREATE TABLE IF NOT EXISTS "TaxRates" (
                "Id"                 uuid         NOT NULL DEFAULT gen_random_uuid(),
                "Code"               varchar(20)  NOT NULL,
                "Name"               varchar(100) NOT NULL,
                "Rate"               numeric(8,4) NOT NULL DEFAULT 0,
                "IsActive"           boolean      NOT NULL DEFAULT true,
                "InputTaxAccountId"  uuid,
                "OutputTaxAccountId" uuid,
                "CreatedAt"          timestamptz  NOT NULL DEFAULT NOW(),
                "CreatedBy"          uuid,
                "ModifiedAt"         timestamptz,
                "ModifiedBy"         uuid,
                "IsDeleted"          boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_TaxRates" PRIMARY KEY ("Id")
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "CommissionRates" (
                "Id"                         uuid         NOT NULL DEFAULT gen_random_uuid(),
                "Name"                       varchar(100) NOT NULL,
                "Rate"                       numeric(8,4) NOT NULL DEFAULT 0,
                "IsDefault"                  boolean      NOT NULL DEFAULT false,
                "IsActive"                   boolean      NOT NULL DEFAULT true,
                "CommissionExpenseAccountId" uuid,
                "CommissionPayableAccountId" uuid,
                "CreatedAt"                  timestamptz  NOT NULL DEFAULT NOW(),
                "CreatedBy"                  uuid,
                "ModifiedAt"                 timestamptz,
                "ModifiedBy"                 uuid,
                "IsDeleted"                  boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_CommissionRates" PRIMARY KEY ("Id")
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "BusinessPartners" (
                "Id"                  uuid         NOT NULL DEFAULT gen_random_uuid(),
                "Code"                varchar(30)  NOT NULL,
                "Name"                varchar(200) NOT NULL,
                "NameAr"              varchar(200),
                "PartnerType"         integer      NOT NULL DEFAULT 1,
                "IsActive"            boolean      NOT NULL DEFAULT true,
                "TaxNumber"           varchar(50),
                "Phone"               varchar(30),
                "Email"               varchar(200),
                "Address"             varchar(500),
                "Notes"               text,
                "ReceivableAccountId" uuid,
                "PayableAccountId"    uuid,
                "CurrencyId"          uuid,
                "CreditLimit"         numeric(18,4),
                "CreatedAt"           timestamptz  NOT NULL DEFAULT NOW(),
                "CreatedBy"           uuid,
                "ModifiedAt"          timestamptz,
                "ModifiedBy"          uuid,
                "IsDeleted"           boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_BusinessPartners" PRIMARY KEY ("Id")
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "BankAccounts" (
                "Id"             uuid         NOT NULL DEFAULT gen_random_uuid(),
                "Code"           varchar(30)  NOT NULL,
                "Name"           varchar(200) NOT NULL,
                "AccountType"    integer      NOT NULL DEFAULT 2,
                "BankName"       varchar(200),
                "AccountNumber"  varchar(50),
                "IBAN"           varchar(34),
                "IsActive"       boolean      NOT NULL DEFAULT true,
                "OpeningBalance" numeric(18,4) NOT NULL DEFAULT 0,
                "CurrentBalance" numeric(18,4) NOT NULL DEFAULT 0,
                "GlAccountId"    uuid         NOT NULL,
                "CurrencyId"     uuid         NOT NULL,
                "CreatedAt"      timestamptz  NOT NULL DEFAULT NOW(),
                "CreatedBy"      uuid,
                "ModifiedAt"     timestamptz,
                "ModifiedBy"     uuid,
                "IsDeleted"      boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_BankAccounts" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_BankAccounts_Accounts_GlAccountId"
                    FOREIGN KEY ("GlAccountId") REFERENCES "Accounts"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_BankAccounts_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "JournalEntries" (
                "Id"              uuid          NOT NULL DEFAULT gen_random_uuid(),
                "EntryNumber"     varchar(30)   NOT NULL,
                "EntryDate"       date          NOT NULL,
                "Description"     varchar(1000) NOT NULL DEFAULT '',
                "Status"          integer       NOT NULL DEFAULT 1,
                "ReferenceType"   integer       NOT NULL DEFAULT 0,
                "ReferenceId"     uuid,
                "ReferenceNumber" varchar(50),
                "CurrencyId"      uuid          NOT NULL,
                "ExchangeRate"    numeric(18,6) NOT NULL DEFAULT 1,
                "TotalDebit"      numeric(18,4) NOT NULL DEFAULT 0,
                "TotalCredit"     numeric(18,4) NOT NULL DEFAULT 0,
                "PostedAt"        timestamptz,
                "PostedBy"        uuid,
                "ReversedEntryId" uuid,
                "FiscalPeriodId"  uuid,
                "CreatedAt"       timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"       uuid,
                "ModifiedAt"      timestamptz,
                "ModifiedBy"      uuid,
                "IsDeleted"       boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_JournalEntries" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_JournalEntries_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT
            )
            """;
        yield return """CREATE UNIQUE INDEX IF NOT EXISTS "IX_JournalEntries_EntryNumber" ON "JournalEntries"("EntryNumber")""";

        yield return """
            CREATE TABLE IF NOT EXISTS "JournalEntryLines" (
                "Id"             uuid          NOT NULL DEFAULT gen_random_uuid(),
                "JournalEntryId" uuid          NOT NULL,
                "AccountId"      uuid          NOT NULL,
                "Description"    varchar(500),
                "Debit"          numeric(18,4) NOT NULL DEFAULT 0,
                "Credit"         numeric(18,4) NOT NULL DEFAULT 0,
                "SortOrder"      integer       NOT NULL DEFAULT 0,
                "CreatedAt"      timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"      uuid,
                "ModifiedAt"     timestamptz,
                "ModifiedBy"     uuid,
                "IsDeleted"      boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_JournalEntryLines" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_JournalEntryLines_JournalEntries_JournalEntryId"
                    FOREIGN KEY ("JournalEntryId") REFERENCES "JournalEntries"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_JournalEntryLines_Accounts_AccountId"
                    FOREIGN KEY ("AccountId") REFERENCES "Accounts"("Id") ON DELETE RESTRICT
            )
            """;

        // ── Operational ───────────────────────────────────────────────────────
        yield return """
            CREATE TABLE IF NOT EXISTS "Warehouses" (
                "Id"        uuid         NOT NULL DEFAULT gen_random_uuid(),
                "Code"      varchar(20)  NOT NULL,
                "Name"      varchar(200) NOT NULL,
                "Location"  varchar(500),
                "Notes"     text,
                "IsActive"  boolean      NOT NULL DEFAULT true,
                "CreatedAt" timestamptz  NOT NULL DEFAULT NOW(),
                "CreatedBy" uuid,
                "ModifiedAt" timestamptz,
                "ModifiedBy" uuid,
                "IsDeleted" boolean      NOT NULL DEFAULT false,
                CONSTRAINT "PK_Warehouses" PRIMARY KEY ("Id")
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "Products" (
                "Id"                 uuid         NOT NULL DEFAULT gen_random_uuid(),
                "SKU"                varchar(50)  NOT NULL,
                "Name"               varchar(200) NOT NULL,
                "Description"        text,
                "Category"           varchar(100),
                "Unit"               varchar(30)  NOT NULL DEFAULT 'Piece',
                "Barcode"            varchar(50),
                "PurchasePrice"      numeric(18,4) NOT NULL DEFAULT 0,
                "SalesPrice"         numeric(18,4) NOT NULL DEFAULT 0,
                "CurrencyId"         uuid          NOT NULL,
                "TaxRateId"          uuid,
                "InventoryAccountId" uuid,
                "COGSAccountId"      uuid,
                "SalesAccountId"     uuid,
                "PurchaseAccountId"  uuid,
                "MinimumStock"       numeric(18,4) NOT NULL DEFAULT 0,
                "IsActive"           boolean       NOT NULL DEFAULT true,
                "CreatedAt"          timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"          uuid,
                "ModifiedAt"         timestamptz,
                "ModifiedBy"         uuid,
                "IsDeleted"          boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_Products" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_Products_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "InventoryBalances" (
                "Id"               uuid          NOT NULL DEFAULT gen_random_uuid(),
                "ProductId"        uuid          NOT NULL,
                "WarehouseId"      uuid          NOT NULL,
                "Quantity"         numeric(18,4) NOT NULL DEFAULT 0,
                "ReservedQuantity" numeric(18,4) NOT NULL DEFAULT 0,
                "AverageCost"      numeric(18,4) NOT NULL DEFAULT 0,
                "CreatedAt"        timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"        uuid,
                "ModifiedAt"       timestamptz,
                "ModifiedBy"       uuid,
                "IsDeleted"        boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_InventoryBalances" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_InventoryBalances_Products_ProductId"
                    FOREIGN KEY ("ProductId") REFERENCES "Products"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_InventoryBalances_Warehouses_WarehouseId"
                    FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses"("Id") ON DELETE RESTRICT
            )
            """;
        yield return """CREATE UNIQUE INDEX IF NOT EXISTS "IX_InventoryBalances_ProductId_WarehouseId" ON "InventoryBalances"("ProductId","WarehouseId")""";

        yield return """
            CREATE TABLE IF NOT EXISTS "InventoryMovements" (
                "Id"              uuid          NOT NULL DEFAULT gen_random_uuid(),
                "ProductId"       uuid          NOT NULL,
                "WarehouseId"     uuid          NOT NULL,
                "MovementType"    integer       NOT NULL DEFAULT 1,
                "Quantity"        numeric(18,4) NOT NULL DEFAULT 0,
                "UnitCost"        numeric(18,4) NOT NULL DEFAULT 0,
                "TotalCost"       numeric(18,4) NOT NULL DEFAULT 0,
                "MovementDate"    date          NOT NULL,
                "Notes"           text,
                "ReferenceType"   varchar(50),
                "ReferenceId"     uuid,
                "ReferenceNumber" varchar(50),
                "CreatedAt"       timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"       uuid,
                "ModifiedAt"      timestamptz,
                "ModifiedBy"      uuid,
                "IsDeleted"       boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_InventoryMovements" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_InventoryMovements_Products_ProductId"
                    FOREIGN KEY ("ProductId") REFERENCES "Products"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_InventoryMovements_Warehouses_WarehouseId"
                    FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "PurchaseOrders" (
                "Id"           uuid          NOT NULL DEFAULT gen_random_uuid(),
                "PONumber"     varchar(30)   NOT NULL,
                "SupplierId"   uuid          NOT NULL,
                "OrderDate"    date          NOT NULL,
                "Notes"        text,
                "CurrencyId"   uuid          NOT NULL,
                "ExchangeRate" numeric(18,6) NOT NULL DEFAULT 1,
                "WarehouseId"  uuid          NOT NULL,
                "Status"       integer       NOT NULL DEFAULT 1,
                "SubTotal"     numeric(18,4) NOT NULL DEFAULT 0,
                "TaxAmount"    numeric(18,4) NOT NULL DEFAULT 0,
                "TotalAmount"  numeric(18,4) NOT NULL DEFAULT 0,
                "CreatedAt"    timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"    uuid,
                "ModifiedAt"   timestamptz,
                "ModifiedBy"   uuid,
                "IsDeleted"    boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_PurchaseOrders" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_PurchaseOrders_BusinessPartners_SupplierId"
                    FOREIGN KEY ("SupplierId") REFERENCES "BusinessPartners"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_PurchaseOrders_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_PurchaseOrders_Warehouses_WarehouseId"
                    FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "PurchaseOrderLines" (
                "Id"               uuid          NOT NULL DEFAULT gen_random_uuid(),
                "PurchaseOrderId"  uuid          NOT NULL,
                "ProductId"        uuid          NOT NULL,
                "Quantity"         numeric(18,4) NOT NULL DEFAULT 0,
                "ReceivedQuantity" numeric(18,4) NOT NULL DEFAULT 0,
                "UnitPrice"        numeric(18,4) NOT NULL DEFAULT 0,
                "DiscountPercent"  numeric(8,4)  NOT NULL DEFAULT 0,
                "DiscountAmount"   numeric(18,4) NOT NULL DEFAULT 0,
                "TaxRate"          numeric(8,4)  NOT NULL DEFAULT 0,
                "TaxAmount"        numeric(18,4) NOT NULL DEFAULT 0,
                "LineTotal"        numeric(18,4) NOT NULL DEFAULT 0,
                "NetAmount"        numeric(18,4) NOT NULL DEFAULT 0,
                "SortOrder"        integer       NOT NULL DEFAULT 0,
                "CreatedAt"        timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"        uuid,
                "ModifiedAt"       timestamptz,
                "ModifiedBy"       uuid,
                "IsDeleted"        boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_PurchaseOrderLines" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId"
                    FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_PurchaseOrderLines_Products_ProductId"
                    FOREIGN KEY ("ProductId") REFERENCES "Products"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "PurchaseReceipts" (
                "Id"              uuid          NOT NULL DEFAULT gen_random_uuid(),
                "ReceiptNumber"   varchar(30)   NOT NULL,
                "PurchaseOrderId" uuid          NOT NULL,
                "SupplierId"      uuid          NOT NULL,
                "WarehouseId"     uuid          NOT NULL,
                "ReceiptDate"     date          NOT NULL,
                "CurrencyId"      uuid          NOT NULL,
                "ExchangeRate"    numeric(18,6) NOT NULL DEFAULT 1,
                "Notes"           text,
                "TotalAmount"     numeric(18,4) NOT NULL DEFAULT 0,
                "JournalEntryId"  uuid,
                "CreatedAt"       timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"       uuid,
                "ModifiedAt"      timestamptz,
                "ModifiedBy"      uuid,
                "IsDeleted"       boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_PurchaseReceipts" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_PurchaseReceipts_PurchaseOrders_PurchaseOrderId"
                    FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_PurchaseReceipts_BusinessPartners_SupplierId"
                    FOREIGN KEY ("SupplierId") REFERENCES "BusinessPartners"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_PurchaseReceipts_Warehouses_WarehouseId"
                    FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_PurchaseReceipts_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "PurchaseReceiptLines" (
                "Id"                  uuid          NOT NULL DEFAULT gen_random_uuid(),
                "PurchaseReceiptId"   uuid          NOT NULL,
                "PurchaseOrderLineId" uuid          NOT NULL,
                "ProductId"           uuid          NOT NULL,
                "Quantity"            numeric(18,4) NOT NULL DEFAULT 0,
                "UnitCost"            numeric(18,4) NOT NULL DEFAULT 0,
                "TotalCost"           numeric(18,4) NOT NULL DEFAULT 0,
                "CreatedAt"           timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"           uuid,
                "ModifiedAt"          timestamptz,
                "ModifiedBy"          uuid,
                "IsDeleted"           boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_PurchaseReceiptLines" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_PurchaseReceiptLines_PurchaseReceipts_PurchaseReceiptId"
                    FOREIGN KEY ("PurchaseReceiptId") REFERENCES "PurchaseReceipts"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_PurchaseReceiptLines_PurchaseOrderLines_PurchaseOrderLineId"
                    FOREIGN KEY ("PurchaseOrderLineId") REFERENCES "PurchaseOrderLines"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_PurchaseReceiptLines_Products_ProductId"
                    FOREIGN KEY ("ProductId") REFERENCES "Products"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "SupplierInvoices" (
                "Id"                    uuid          NOT NULL DEFAULT gen_random_uuid(),
                "InvoiceNumber"         varchar(30)   NOT NULL,
                "SupplierInvoiceNumber" varchar(50),
                "SupplierId"            uuid          NOT NULL,
                "InvoiceDate"           date          NOT NULL,
                "DueDate"               date          NOT NULL,
                "CurrencyId"            uuid          NOT NULL,
                "ExchangeRate"          numeric(18,6) NOT NULL DEFAULT 1,
                "PurchaseReceiptId"     uuid,
                "Status"                integer       NOT NULL DEFAULT 1,
                "SubTotal"              numeric(18,4) NOT NULL DEFAULT 0,
                "DiscountAmount"        numeric(18,4) NOT NULL DEFAULT 0,
                "TaxAmount"             numeric(18,4) NOT NULL DEFAULT 0,
                "TotalAmount"           numeric(18,4) NOT NULL DEFAULT 0,
                "PaidAmount"            numeric(18,4) NOT NULL DEFAULT 0,
                "JournalEntryId"        uuid,
                "EInvoiceUUID"          varchar(100),
                "EInvoiceStatus"        integer       NOT NULL DEFAULT 0,
                "CreatedAt"             timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"             uuid,
                "ModifiedAt"            timestamptz,
                "ModifiedBy"            uuid,
                "IsDeleted"             boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_SupplierInvoices" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_SupplierInvoices_BusinessPartners_SupplierId"
                    FOREIGN KEY ("SupplierId") REFERENCES "BusinessPartners"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_SupplierInvoices_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "SupplierInvoiceLines" (
                "Id"                uuid          NOT NULL DEFAULT gen_random_uuid(),
                "SupplierInvoiceId" uuid          NOT NULL,
                "ProductId"         uuid          NOT NULL,
                "Description"       varchar(500)  NOT NULL DEFAULT '',
                "Quantity"          numeric(18,4) NOT NULL DEFAULT 0,
                "UnitPrice"         numeric(18,4) NOT NULL DEFAULT 0,
                "DiscountPercent"   numeric(8,4)  NOT NULL DEFAULT 0,
                "DiscountAmount"    numeric(18,4) NOT NULL DEFAULT 0,
                "TaxRate"           numeric(8,4)  NOT NULL DEFAULT 0,
                "TaxAmount"         numeric(18,4) NOT NULL DEFAULT 0,
                "LineTotal"         numeric(18,4) NOT NULL DEFAULT 0,
                "NetAmount"         numeric(18,4) NOT NULL DEFAULT 0,
                "SortOrder"         integer       NOT NULL DEFAULT 0,
                "CreatedAt"         timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"         uuid,
                "ModifiedAt"        timestamptz,
                "ModifiedBy"        uuid,
                "IsDeleted"         boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_SupplierInvoiceLines" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_SupplierInvoiceLines_SupplierInvoices_SupplierInvoiceId"
                    FOREIGN KEY ("SupplierInvoiceId") REFERENCES "SupplierInvoices"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_SupplierInvoiceLines_Products_ProductId"
                    FOREIGN KEY ("ProductId") REFERENCES "Products"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "SalesOrders" (
                "Id"            uuid          NOT NULL DEFAULT gen_random_uuid(),
                "SONumber"      varchar(30)   NOT NULL,
                "CustomerId"    uuid          NOT NULL,
                "OrderDate"     date          NOT NULL,
                "Notes"         text,
                "CurrencyId"    uuid          NOT NULL,
                "ExchangeRate"  numeric(18,6) NOT NULL DEFAULT 1,
                "WarehouseId"   uuid          NOT NULL,
                "SalespersonId" uuid,
                "Status"        integer       NOT NULL DEFAULT 1,
                "SubTotal"      numeric(18,4) NOT NULL DEFAULT 0,
                "TaxAmount"     numeric(18,4) NOT NULL DEFAULT 0,
                "TotalAmount"   numeric(18,4) NOT NULL DEFAULT 0,
                "CreatedAt"     timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"     uuid,
                "ModifiedAt"    timestamptz,
                "ModifiedBy"    uuid,
                "IsDeleted"     boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_SalesOrders" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_SalesOrders_BusinessPartners_CustomerId"
                    FOREIGN KEY ("CustomerId") REFERENCES "BusinessPartners"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_SalesOrders_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_SalesOrders_Warehouses_WarehouseId"
                    FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "SalesOrderLines" (
                "Id"                uuid          NOT NULL DEFAULT gen_random_uuid(),
                "SalesOrderId"      uuid          NOT NULL,
                "ProductId"         uuid          NOT NULL,
                "Quantity"          numeric(18,4) NOT NULL DEFAULT 0,
                "DeliveredQuantity" numeric(18,4) NOT NULL DEFAULT 0,
                "UnitPrice"         numeric(18,4) NOT NULL DEFAULT 0,
                "DiscountPercent"   numeric(8,4)  NOT NULL DEFAULT 0,
                "DiscountAmount"    numeric(18,4) NOT NULL DEFAULT 0,
                "TaxRate"           numeric(8,4)  NOT NULL DEFAULT 0,
                "TaxAmount"         numeric(18,4) NOT NULL DEFAULT 0,
                "LineTotal"         numeric(18,4) NOT NULL DEFAULT 0,
                "NetAmount"         numeric(18,4) NOT NULL DEFAULT 0,
                "SortOrder"         integer       NOT NULL DEFAULT 0,
                "CreatedAt"         timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"         uuid,
                "ModifiedAt"        timestamptz,
                "ModifiedBy"        uuid,
                "IsDeleted"         boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_SalesOrderLines" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_SalesOrderLines_SalesOrders_SalesOrderId"
                    FOREIGN KEY ("SalesOrderId") REFERENCES "SalesOrders"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_SalesOrderLines_Products_ProductId"
                    FOREIGN KEY ("ProductId") REFERENCES "Products"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "SalesDeliveries" (
                "Id"             uuid          NOT NULL DEFAULT gen_random_uuid(),
                "DeliveryNumber" varchar(30)   NOT NULL,
                "SalesOrderId"   uuid          NOT NULL,
                "CustomerId"     uuid          NOT NULL,
                "WarehouseId"    uuid          NOT NULL,
                "DeliveryDate"   date          NOT NULL,
                "Notes"          text,
                "TotalCOGS"      numeric(18,4) NOT NULL DEFAULT 0,
                "JournalEntryId" uuid,
                "CreatedAt"      timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"      uuid,
                "ModifiedAt"     timestamptz,
                "ModifiedBy"     uuid,
                "IsDeleted"      boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_SalesDeliveries" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_SalesDeliveries_SalesOrders_SalesOrderId"
                    FOREIGN KEY ("SalesOrderId") REFERENCES "SalesOrders"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_SalesDeliveries_BusinessPartners_CustomerId"
                    FOREIGN KEY ("CustomerId") REFERENCES "BusinessPartners"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_SalesDeliveries_Warehouses_WarehouseId"
                    FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "SalesDeliveryLines" (
                "Id"               uuid          NOT NULL DEFAULT gen_random_uuid(),
                "SalesDeliveryId"  uuid          NOT NULL,
                "SalesOrderLineId" uuid          NOT NULL,
                "ProductId"        uuid          NOT NULL,
                "Quantity"         numeric(18,4) NOT NULL DEFAULT 0,
                "UnitCost"         numeric(18,4) NOT NULL DEFAULT 0,
                "TotalCost"        numeric(18,4) NOT NULL DEFAULT 0,
                "CreatedAt"        timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"        uuid,
                "ModifiedAt"       timestamptz,
                "ModifiedBy"       uuid,
                "IsDeleted"        boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_SalesDeliveryLines" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_SalesDeliveryLines_SalesDeliveries_SalesDeliveryId"
                    FOREIGN KEY ("SalesDeliveryId") REFERENCES "SalesDeliveries"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_SalesDeliveryLines_SalesOrderLines_SalesOrderLineId"
                    FOREIGN KEY ("SalesOrderLineId") REFERENCES "SalesOrderLines"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_SalesDeliveryLines_Products_ProductId"
                    FOREIGN KEY ("ProductId") REFERENCES "Products"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "CustomerInvoices" (
                "Id"                     uuid          NOT NULL DEFAULT gen_random_uuid(),
                "InvoiceNumber"          varchar(30)   NOT NULL,
                "CustomerId"             uuid          NOT NULL,
                "InvoiceDate"            date          NOT NULL,
                "DueDate"                date          NOT NULL,
                "CurrencyId"             uuid          NOT NULL,
                "ExchangeRate"           numeric(18,6) NOT NULL DEFAULT 1,
                "SalespersonId"          uuid,
                "SalesOrderId"           uuid,
                "Status"                 integer       NOT NULL DEFAULT 1,
                "InvoiceType"            integer       NOT NULL DEFAULT 1,
                "SubTotal"               numeric(18,4) NOT NULL DEFAULT 0,
                "DiscountAmount"         numeric(18,4) NOT NULL DEFAULT 0,
                "TaxAmount"              numeric(18,4) NOT NULL DEFAULT 0,
                "TotalAmount"            numeric(18,4) NOT NULL DEFAULT 0,
                "PaidAmount"             numeric(18,4) NOT NULL DEFAULT 0,
                "JournalEntryId"         uuid,
                "TaxRegistrationNumber"  varchar(50),
                "EInvoiceUUID"           varchar(100),
                "EInvoiceStatus"         integer       NOT NULL DEFAULT 0,
                "EInvoiceSubmissionDate" timestamptz,
                "ExternalInvoiceId"      varchar(100),
                "ExternalStatus"         varchar(50),
                "QRCode"                 text,
                "CancellationStatus"     varchar(50),
                "CreatedAt"              timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"              uuid,
                "ModifiedAt"             timestamptz,
                "ModifiedBy"             uuid,
                "IsDeleted"              boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_CustomerInvoices" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_CustomerInvoices_BusinessPartners_CustomerId"
                    FOREIGN KEY ("CustomerId") REFERENCES "BusinessPartners"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_CustomerInvoices_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "CustomerInvoiceLines" (
                "Id"                uuid          NOT NULL DEFAULT gen_random_uuid(),
                "CustomerInvoiceId" uuid          NOT NULL,
                "ProductId"         uuid          NOT NULL,
                "Description"       varchar(500)  NOT NULL DEFAULT '',
                "Quantity"          numeric(18,4) NOT NULL DEFAULT 0,
                "UnitPrice"         numeric(18,4) NOT NULL DEFAULT 0,
                "DiscountPercent"   numeric(8,4)  NOT NULL DEFAULT 0,
                "DiscountAmount"    numeric(18,4) NOT NULL DEFAULT 0,
                "TaxRate"           numeric(8,4)  NOT NULL DEFAULT 0,
                "TaxAmount"         numeric(18,4) NOT NULL DEFAULT 0,
                "LineTotal"         numeric(18,4) NOT NULL DEFAULT 0,
                "NetAmount"         numeric(18,4) NOT NULL DEFAULT 0,
                "UnitCost"          numeric(18,4) NOT NULL DEFAULT 0,
                "TotalCost"         numeric(18,4) NOT NULL DEFAULT 0,
                "SortOrder"         integer       NOT NULL DEFAULT 0,
                "CreatedAt"         timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"         uuid,
                "ModifiedAt"        timestamptz,
                "ModifiedBy"        uuid,
                "IsDeleted"         boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_CustomerInvoiceLines" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_CustomerInvoiceLines_CustomerInvoices_CustomerInvoiceId"
                    FOREIGN KEY ("CustomerInvoiceId") REFERENCES "CustomerInvoices"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_CustomerInvoiceLines_Products_ProductId"
                    FOREIGN KEY ("ProductId") REFERENCES "Products"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "Cheques" (
                "Id"                    uuid          NOT NULL DEFAULT gen_random_uuid(),
                "ChequeNumber"          varchar(50)   NOT NULL,
                "CustomerId"            uuid          NOT NULL,
                "BankName"              varchar(200)  NOT NULL,
                "CurrencyId"            uuid          NOT NULL,
                "Amount"                numeric(18,4) NOT NULL DEFAULT 0,
                "AmountBase"            numeric(18,4) NOT NULL DEFAULT 0,
                "IssueDate"             date          NOT NULL,
                "DueDate"               date          NOT NULL,
                "ReceivedDate"          date          NOT NULL,
                "BankAccountId"         uuid,
                "Status"                integer       NOT NULL DEFAULT 1,
                "Notes"                 text,
                "ReceiptJournalEntryId" uuid,
                "DepositJournalEntryId" uuid,
                "BounceJournalEntryId"  uuid,
                "CreatedAt"             timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"             uuid,
                "ModifiedAt"            timestamptz,
                "ModifiedBy"            uuid,
                "IsDeleted"             boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_Cheques" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_Cheques_BusinessPartners_CustomerId"
                    FOREIGN KEY ("CustomerId") REFERENCES "BusinessPartners"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_Cheques_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "CustomerPayments" (
                "Id"             uuid          NOT NULL DEFAULT gen_random_uuid(),
                "PaymentNumber"  varchar(30)   NOT NULL,
                "CustomerId"     uuid          NOT NULL,
                "PaymentDate"    date          NOT NULL,
                "CurrencyId"     uuid          NOT NULL,
                "ExchangeRate"   numeric(18,6) NOT NULL DEFAULT 1,
                "Amount"         numeric(18,4) NOT NULL DEFAULT 0,
                "AmountBase"     numeric(18,4) NOT NULL DEFAULT 0,
                "PaymentMethod"  integer       NOT NULL DEFAULT 1,
                "Status"         integer       NOT NULL DEFAULT 1,
                "BankAccountId"  uuid,
                "ChequeId"       uuid,
                "Notes"          text,
                "JournalEntryId" uuid,
                "CreatedAt"      timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"      uuid,
                "ModifiedAt"     timestamptz,
                "ModifiedBy"     uuid,
                "IsDeleted"      boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_CustomerPayments" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_CustomerPayments_BusinessPartners_CustomerId"
                    FOREIGN KEY ("CustomerId") REFERENCES "BusinessPartners"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_CustomerPayments_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "CustomerPaymentInvoices" (
                "CustomerPaymentId" uuid NOT NULL,
                "CustomerInvoiceId" uuid NOT NULL,
                CONSTRAINT "PK_CustomerPaymentInvoices" PRIMARY KEY ("CustomerPaymentId","CustomerInvoiceId"),
                CONSTRAINT "FK_CustomerPaymentInvoices_CustomerPayments"
                    FOREIGN KEY ("CustomerPaymentId") REFERENCES "CustomerPayments"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_CustomerPaymentInvoices_CustomerInvoices"
                    FOREIGN KEY ("CustomerInvoiceId") REFERENCES "CustomerInvoices"("Id") ON DELETE CASCADE
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "SupplierPayments" (
                "Id"             uuid          NOT NULL DEFAULT gen_random_uuid(),
                "PaymentNumber"  varchar(30)   NOT NULL,
                "SupplierId"     uuid          NOT NULL,
                "PaymentDate"    date          NOT NULL,
                "CurrencyId"     uuid          NOT NULL,
                "ExchangeRate"   numeric(18,6) NOT NULL DEFAULT 1,
                "Amount"         numeric(18,4) NOT NULL DEFAULT 0,
                "AmountBase"     numeric(18,4) NOT NULL DEFAULT 0,
                "PaymentMethod"  integer       NOT NULL DEFAULT 1,
                "Status"         integer       NOT NULL DEFAULT 1,
                "BankAccountId"  uuid,
                "Notes"          text,
                "JournalEntryId" uuid,
                "CreatedAt"      timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"      uuid,
                "ModifiedAt"     timestamptz,
                "ModifiedBy"     uuid,
                "IsDeleted"      boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_SupplierPayments" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_SupplierPayments_BusinessPartners_SupplierId"
                    FOREIGN KEY ("SupplierId") REFERENCES "BusinessPartners"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_SupplierPayments_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "SupplierPaymentInvoices" (
                "SupplierPaymentId" uuid NOT NULL,
                "SupplierInvoiceId" uuid NOT NULL,
                CONSTRAINT "PK_SupplierPaymentInvoices" PRIMARY KEY ("SupplierPaymentId","SupplierInvoiceId"),
                CONSTRAINT "FK_SupplierPaymentInvoices_SupplierPayments"
                    FOREIGN KEY ("SupplierPaymentId") REFERENCES "SupplierPayments"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_SupplierPaymentInvoices_SupplierInvoices"
                    FOREIGN KEY ("SupplierInvoiceId") REFERENCES "SupplierInvoices"("Id") ON DELETE CASCADE
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "BankTransactions" (
                "Id"                       uuid          NOT NULL DEFAULT gen_random_uuid(),
                "TransactionNumber"        varchar(30)   NOT NULL,
                "BankAccountId"            uuid          NOT NULL,
                "TransactionType"          integer       NOT NULL DEFAULT 1,
                "TransactionDate"          date          NOT NULL,
                "CurrencyId"               uuid          NOT NULL,
                "ExchangeRate"             numeric(18,6) NOT NULL DEFAULT 1,
                "Amount"                   numeric(18,4) NOT NULL DEFAULT 0,
                "AmountBase"               numeric(18,4) NOT NULL DEFAULT 0,
                "Description"              varchar(500),
                "Reference"                varchar(100),
                "DestinationBankAccountId" uuid,
                "JournalEntryId"           uuid,
                "CreatedAt"                timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"                uuid,
                "ModifiedAt"               timestamptz,
                "ModifiedBy"               uuid,
                "IsDeleted"                boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_BankTransactions" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_BankTransactions_BankAccounts_BankAccountId"
                    FOREIGN KEY ("BankAccountId") REFERENCES "BankAccounts"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_BankTransactions_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT
            )
            """;

        yield return """
            CREATE TABLE IF NOT EXISTS "SalesCommissions" (
                "Id"               uuid          NOT NULL DEFAULT gen_random_uuid(),
                "SalespersonId"    uuid,
                "SalespersonName"  varchar(200)  NOT NULL DEFAULT '',
                "CommissionRateId" uuid          NOT NULL,
                "Rate"             numeric(8,4)  NOT NULL DEFAULT 0,
                "BaseSalesAmount"  numeric(18,4) NOT NULL DEFAULT 0,
                "CommissionAmount" numeric(18,4) NOT NULL DEFAULT 0,
                "CurrencyId"       uuid          NOT NULL,
                "ExchangeRate"     numeric(18,6) NOT NULL DEFAULT 1,
                "Status"           integer       NOT NULL DEFAULT 1,
                "SalesOrderId"     uuid,
                "CustomerInvoiceId" uuid,
                "JournalEntryId"   uuid,
                "CreatedAt"        timestamptz   NOT NULL DEFAULT NOW(),
                "CreatedBy"        uuid,
                "ModifiedAt"       timestamptz,
                "ModifiedBy"       uuid,
                "IsDeleted"        boolean       NOT NULL DEFAULT false,
                CONSTRAINT "PK_SalesCommissions" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_SalesCommissions_CommissionRates_CommissionRateId"
                    FOREIGN KEY ("CommissionRateId") REFERENCES "CommissionRates"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_SalesCommissions_Currencies_CurrencyId"
                    FOREIGN KEY ("CurrencyId") REFERENCES "Currencies"("Id") ON DELETE RESTRICT
            )
            """;
    }
}
