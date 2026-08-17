using Microsoft.Extensions.Logging;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Enums;

namespace RTSErp.Infrastructure.Persistence.Seed;

public static class AccountingSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        await SeedCurrenciesAsync(db, logger);
        await SeedChartOfAccountsAsync(db, logger);
        await SeedFiscalPeriodsAsync(db, logger);
        await SeedTaxRatesAsync(db, logger);
        await SeedCommissionRatesAsync(db, logger);
        await SeedBankAccountsAsync(db, logger);
    }

    // ── Currencies ─────────────────────────────────────────────────────────────

    private static async Task SeedCurrenciesAsync(ApplicationDbContext db, ILogger logger)
    {
        if (db.Currencies.Any()) return;

        db.Currencies.AddRange(
            new Currency { Code = "EGP", Name = "Egyptian Pound",  Symbol = "ج.م", ExchangeRate = 1m,     IsBaseCurrency = true,  IsActive = true },
            new Currency { Code = "USD", Name = "US Dollar",       Symbol = "$",   ExchangeRate = 48.75m, IsBaseCurrency = false, IsActive = true }
        );

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded currencies: EGP, USD.");
    }

    // ── Chart of Accounts ──────────────────────────────────────────────────────

    private static async Task SeedChartOfAccountsAsync(ApplicationDbContext db, ILogger logger)
    {
        if (db.Accounts.Any()) return;

        // Helper: create and immediately save so Id is available for children
        async Task<Account> Add(string code, string name, string nameAr, AccountType type, bool isGroup, Guid? parentId = null)
        {
            var a = new Account
            {
                Code = code, Name = name, NameAr = nameAr,
                AccountType = type, IsGroup = isGroup,
                ParentId = parentId, IsActive = true
            };
            db.Accounts.Add(a);
            await db.SaveChangesAsync();
            return a;
        }

        // ── 1000 ASSETS ────────────────────────────────────────────────────────
        var assets = await Add("1000", "Assets", "الأصول", AccountType.Asset, true);

        var cash = await Add("1100", "Cash & Cash Equivalents", "النقدية وما يعادلها", AccountType.Asset, true, assets.Id);
        await Add("1101", "Petty Cash",     "الصندوق",           AccountType.Asset, false, cash.Id);
        await Add("1102", "Cash on Hand",   "نقد باليد",          AccountType.Asset, false, cash.Id);

        var banks = await Add("1200", "Banks", "البنوك", AccountType.Asset, true, assets.Id);
        await Add("1201", "Bank Account – EGP", "حساب بنكي – جنيه", AccountType.Asset, false, banks.Id);
        await Add("1202", "Bank Account – USD", "حساب بنكي – دولار", AccountType.Asset, false, banks.Id);

        var ar = await Add("1300", "Accounts Receivable", "ذمم مدينة", AccountType.Asset, true, assets.Id);
        await Add("1301", "Trade Receivables", "مستحقات تجارية", AccountType.Asset, false, ar.Id);
        await Add("1302", "Cheques Receivable", "أوراق القبض", AccountType.Asset, false, ar.Id);

        var vatInput = await Add("1400", "VAT Receivable (Input)", "ضريبة القيمة المضافة المدخلات", AccountType.Asset, false, assets.Id);

        var inventory = await Add("1500", "Inventory", "المخزون", AccountType.Asset, true, assets.Id);
        await Add("1501", "Raw Materials",      "المواد الخام",       AccountType.Asset, false, inventory.Id);
        await Add("1502", "Finished Goods",     "بضاعة تامة الصنع",   AccountType.Asset, false, inventory.Id);

        var prepaid = await Add("1600", "Prepaid Expenses", "مصروفات مدفوعة مقدماً", AccountType.Asset, false, assets.Id);
        var fixedAssets = await Add("1700", "Fixed Assets", "الأصول الثابتة", AccountType.Asset, true, assets.Id);
        await Add("1701", "Equipment",       "معدات",    AccountType.Asset, false, fixedAssets.Id);
        await Add("1702", "Furniture",       "أثاث",     AccountType.Asset, false, fixedAssets.Id);
        await Add("1703", "Vehicles",        "سيارات",   AccountType.Asset, false, fixedAssets.Id);
        await Add("1790", "Acc. Depreciation", "مجمع الاستهلاك", AccountType.Asset, false, fixedAssets.Id);

        // ── 2000 LIABILITIES ───────────────────────────────────────────────────
        var liabilities = await Add("2000", "Liabilities", "الخصوم", AccountType.Liability, true);

        var ap = await Add("2100", "Accounts Payable", "ذمم دائنة", AccountType.Liability, true, liabilities.Id);
        await Add("2101", "Trade Payables", "مستحقات موردين", AccountType.Liability, false, ap.Id);

        var vatOutput = await Add("2200", "VAT Payable (Output)", "ضريبة القيمة المضافة المخرجات", AccountType.Liability, false, liabilities.Id);

        var commPayable = await Add("2300", "Commission Payable", "عمولات مستحقة الدفع", AccountType.Liability, false, liabilities.Id);

        await Add("2400", "Accrued Expenses",  "مستحقات مصروفات",     AccountType.Liability, false, liabilities.Id);
        await Add("2500", "Deferred Revenue",  "إيراد مؤجل",           AccountType.Liability, false, liabilities.Id);
        await Add("2600", "Loans Payable",     "قروض مستحقة الدفع",   AccountType.Liability, false, liabilities.Id);

        // ── 3000 EQUITY ────────────────────────────────────────────────────────
        var equity = await Add("3000", "Equity", "حقوق الملكية", AccountType.Equity, true);
        await Add("3100", "Share Capital",         "رأس المال",          AccountType.Equity, false, equity.Id);
        await Add("3200", "Retained Earnings",     "الأرباح المحتجزة",    AccountType.Equity, false, equity.Id);
        await Add("3300", "Current Year Earnings", "أرباح السنة الحالية", AccountType.Equity, false, equity.Id);

        // ── 4000 REVENUE ───────────────────────────────────────────────────────
        var revenue = await Add("4000", "Revenue", "الإيرادات", AccountType.Revenue, true);
        await Add("4100", "Sales Revenue",    "إيرادات المبيعات",   AccountType.Revenue, false, revenue.Id);
        await Add("4200", "Service Revenue",  "إيرادات الخدمات",   AccountType.Revenue, false, revenue.Id);
        await Add("4900", "Other Income",     "إيرادات أخرى",       AccountType.Revenue, false, revenue.Id);

        // ── 5000 COST OF SALES ─────────────────────────────────────────────────
        var cos = await Add("5000", "Cost of Sales", "تكلفة المبيعات", AccountType.CostOfSales, true);
        await Add("5100", "Cost of Goods Sold",  "تكلفة البضاعة المباعة", AccountType.CostOfSales, false, cos.Id);
        await Add("5200", "Cost of Services",    "تكلفة الخدمات",          AccountType.CostOfSales, false, cos.Id);

        // ── 6000 EXPENSES ──────────────────────────────────────────────────────
        var expenses = await Add("6000", "Expenses", "المصروفات", AccountType.Expense, true);
        await Add("6100", "Sales Commissions",   "عمولات المبيعات",   AccountType.Expense, false, expenses.Id);
        await Add("6200", "Salaries & Wages",    "الرواتب والأجور",    AccountType.Expense, false, expenses.Id);
        await Add("6300", "Rent Expense",        "مصروف الإيجار",      AccountType.Expense, false, expenses.Id);
        await Add("6400", "Utilities",           "المرافق",             AccountType.Expense, false, expenses.Id);
        await Add("6500", "Depreciation",        "استهلاك",             AccountType.Expense, false, expenses.Id);
        await Add("6600", "General & Admin",     "مصروفات عمومية",     AccountType.Expense, false, expenses.Id);
        await Add("6700", "Bank Charges",        "عمولات بنكية",        AccountType.Expense, false, expenses.Id);
        await Add("6800", "Marketing & Advertising", "تسويق وإعلان",   AccountType.Expense, false, expenses.Id);

        logger.LogInformation("Seeded Chart of Accounts.");
    }

    // ── Fiscal Periods ─────────────────────────────────────────────────────────

    private static async Task SeedFiscalPeriodsAsync(ApplicationDbContext db, ILogger logger)
    {
        if (db.FiscalPeriods.Any()) return;

        var year = DateTime.UtcNow.Year;
        db.FiscalPeriods.Add(new FiscalPeriod
        {
            Name = $"FY{year}",
            StartDate = new DateOnly(year, 1, 1),
            EndDate = new DateOnly(year, 12, 31),
            Status = FiscalPeriodStatus.Open
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded fiscal period FY{Year}.", year);
    }

    // ── Tax Rates ──────────────────────────────────────────────────────────────

    private static async Task SeedTaxRatesAsync(ApplicationDbContext db, ILogger logger)
    {
        if (db.TaxRates.Any()) return;

        // Look up accounts by code
        var vatInput  = db.Accounts.FirstOrDefault(a => a.Code == "1400");
        var vatOutput = db.Accounts.FirstOrDefault(a => a.Code == "2200");

        db.TaxRates.Add(new TaxRate
        {
            Code = "VAT14",
            Name = "VAT 14%",
            Rate = 0.14m,
            IsActive = true,
            InputTaxAccountId  = vatInput?.Id,
            OutputTaxAccountId = vatOutput?.Id
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded VAT 14% tax rate.");
    }

    // ── Commission Rates ───────────────────────────────────────────────────────

    private static async Task SeedCommissionRatesAsync(ApplicationDbContext db, ILogger logger)
    {
        if (db.CommissionRates.Any()) return;

        var expenseAcc  = db.Accounts.FirstOrDefault(a => a.Code == "6100");
        var payableAcc  = db.Accounts.FirstOrDefault(a => a.Code == "2300");

        db.CommissionRates.Add(new CommissionRate
        {
            Name = "Default Commission",
            Rate = 0.015m,   // 1.5%
            IsDefault = true,
            IsActive = true,
            CommissionExpenseAccountId = expenseAcc?.Id,
            CommissionPayableAccountId = payableAcc?.Id
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded default commission rate (1.5%).");
    }

    // ── Bank Accounts ──────────────────────────────────────────────────────────

    private static async Task SeedBankAccountsAsync(ApplicationDbContext db, ILogger logger)
    {
        if (db.BankAccounts.Any()) return;

        var egp = db.Currencies.FirstOrDefault(c => c.Code == "EGP");
        var usd = db.Currencies.FirstOrDefault(c => c.Code == "USD");
        var cashAcc     = db.Accounts.FirstOrDefault(a => a.Code == "1101");
        var bankEgpAcc  = db.Accounts.FirstOrDefault(a => a.Code == "1201");
        var bankUsdAcc  = db.Accounts.FirstOrDefault(a => a.Code == "1202");

        if (egp is not null && cashAcc is not null)
        {
            db.BankAccounts.Add(new BankAccount
            {
                Code = "CASH-EGP",
                Name = "Petty Cash EGP",
                AccountType = BankAccountType.Cash,
                GlAccountId = cashAcc.Id,
                CurrencyId = egp.Id,
                IsActive = true
            });
        }

        if (egp is not null && bankEgpAcc is not null)
        {
            db.BankAccounts.Add(new BankAccount
            {
                Code = "BANK-A-EGP",
                Name = "Bank A – EGP Account",
                AccountType = BankAccountType.Bank,
                BankName = "National Bank of Egypt",
                GlAccountId = bankEgpAcc.Id,
                CurrencyId = egp.Id,
                IsActive = true
            });
        }

        if (usd is not null && bankUsdAcc is not null)
        {
            db.BankAccounts.Add(new BankAccount
            {
                Code = "BANK-B-USD",
                Name = "Bank B – USD Account",
                AccountType = BankAccountType.Bank,
                BankName = "CIB Egypt",
                GlAccountId = bankUsdAcc.Id,
                CurrencyId = usd.Id,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded bank/cash accounts.");
    }
}
