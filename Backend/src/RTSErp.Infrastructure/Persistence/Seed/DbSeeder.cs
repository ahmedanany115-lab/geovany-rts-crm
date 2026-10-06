using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RTSErp.Domain.Entities.Accounting;
using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    // ── Production accounts ───────────────────────────────────────────────────

    private record SeedUser(
        string Email, string Password,
        string FirstName, string LastName,
        string JobTitle, string Department,
        string Role);

    private static readonly SeedUser[] ProductionUsers =
    [
        // ── Admins ────────────────────────────────────────────────────────────
        new("geovany.hany@rtegy.com",          "Geovany@153",  "Geovany",     "Hany",       "System Administrator",    "IT",          "Admin"),
        new("dr.mohamed@rtegy.com",            "Ceo@123",      "Mohamed",     "",           "Chief Executive Officer", "Management",  "Admin"),

        // ── Finance ───────────────────────────────────────────────────────────
        new("Moataz@rtegy.com",                "Moataz@123",   "Moataz",      "",           "Accountant",              "Finance",     "Accountant"),
        new("fatma@rtegy.com",                 "Fatma@123",    "Fatma",       "",           "Accountant",              "Finance",     "Accountant"),
        new("ahmed.rekaby@rtegy.com",          "Rekaby@123",   "Ahmed",       "Rekaby",     "Senior Accountant",       "Finance",     "Accountant"),

        // ── Sales ─────────────────────────────────────────────────────────────
        new("Mrim@rtegy.com",                  "257993",       "Mrim",        "",           "Sales Representative",    "Sales",       "Sales"),
        new("hanem.omar@rtegy.com",            "Hanem@123",    "Hanem",       "Omar",       "Sales Representative",    "Sales",       "Sales"),
        new("Abdelrahman.Abdullah@rtegy.com",  "Abdo@123",     "Abdelrahman", "Abdullah",   "Sales Representative",    "Sales",       "Sales"),

        // ── Sales Manager ─────────────────────────────────────────────────────
        new("Ahmed.Anany@rtegy.com",           "Anany@123",    "Ahmed",       "Anany",      "Sales Manager",           "Sales",       "SalesManager"),

        // ── Purchasing ────────────────────────────────────────────────────────
        new("khaled.taleb@rtegy.com",          "Taleb@123",    "Khaled",      "Taleb",      "Purchasing Officer",       "Purchasing",  "Purchasing"),

        // ── Maintenance / Support ─────────────────────────────────────────────
        new("Ahmed.Mostafa@rtegy.com",         "Mostafa@123",  "Ahmed",       "Mostafa",    "Support Engineer",        "Maintenance", "SupportAgent"),
        new("Hossam@rtegy.com",                "Hossam@123",   "Hossam",      "",           "Support Engineer",        "Maintenance", "SupportAgent"),
        new("Mostafa@rtegy.com",               "Mosta@123",    "Mostafa",     "",           "Support Engineer",        "Maintenance", "SupportAgent"),
        new("youssef.mounir@rtegy.com",        "Mounir@123",   "Youssef",     "Mounir",     "Support Engineer",        "Maintenance", "SupportAgent"),
        new("youssef.mohamed@rtegy.com",       "Youssef@123",  "Youssef",     "Mohamed",    "Support Engineer",        "Maintenance", "SupportAgent"),
        new("mahmoud.amr@rtegy.com",           "Mahmoud@123",  "Mahmoud",     "Amr",        "Support Engineer",        "Maintenance", "SupportAgent"),
        new("karim.mahmoud@rtegy.com",         "Karim@123",    "Karim",       "Mahmoud",    "Support Engineer",        "Maintenance", "SupportAgent"),

        // ── Delivery / Representatives ────────────────────────────────────────
        new("mahmoud.nasrallah@rtegy.com",     "Nasrallah@123","Mahmoud",     "Nasrallah",  "Delivery Representative", "Operations",  "Delivery"),
        new("hany.mahmoud@rtegy.com",          "Hany@123",     "Hany",        "Mahmoud",    "Delivery Representative", "Operations",  "Delivery"),
        new("ahmed.reda@rtegy.com",            "Reda@123",     "Ahmed",       "Reda",       "Delivery Representative", "Operations",  "Delivery"),

        // ── Marketing ─────────────────────────────────────────────────────────
        new("dina.reda@rtegy.com",             "Dina@123",     "Dina",        "Reda",       "Marketing Specialist",    "Marketing",   "Marketing"),

        // ── Sales (outdoor) ───────────────────────────────────────────────────
        new("Ahmed.khaled@rtegy.com",          "Akhaled@123",  "Ahmed",       "Khaled",     "Sales Outdoor",           "Sales",       "Sales"),

        // ── No title ─────────────────────────────────────────────────────────
        new("mhy@rtegy.com",                   "Mahy@123",     "Mahy",        "",           "",                        "",            "ReadOnly"),

        // ── ReadOnly viewers ─────────────────────────────────────────────────
        new("kfahim@rtegy.com",                "Kfahim@123",   "Kfahim",      "",           "",                        "",            "ReadOnly"),
        new("Dgeorge@rtegy.com",               "Daniel@123",   "Daniel",      "George",     "",                        "",            "ReadOnly"),

        // ── Office staff ─────────────────────────────────────────────────────
        new("randa@rtegy.com",                 "Randa@123",    "Randa",       "El Beheiry", "Office Girl",             "Office",      "Office"),

        // ── Additional ReadOnly ───────────────────────────────────────────────
        new("Farah@rtegy.com",                 "Farouha@123",  "Farah",       "El Anany",   "",                        "",            "ReadOnly"),
    ];

    // ── Role definitions ──────────────────────────────────────────────────────
    public static readonly Dictionary<string, Func<Permission, bool>> RolePermissions = new()
    {
        ["Admin"]        = _ => true,
        ["Manager"]      = p => !p.Code.StartsWith("users.") && p.Code != "settings.write",

        // Sales Manager — full CRM + Sales + reports, no finance
        ["SalesManager"] = p => p.Module is "crm" or "quotations" or "tasks" or "reports"
                             || p.Code.StartsWith("inventory.products.read")
                             || p.Code.StartsWith("inventory.hardware.read"),

        ["Accountant"]   = p => p.Module is "invoices" or "reports"
                             || p.Code.StartsWith("inventory.products.read")
                             || p.Code.StartsWith("inventory.suppliers"),

        ["Sales"]        = p => p.Module is "crm" or "quotations" or "tasks" or "reports"
                             || p.Code.StartsWith("inventory.products.read")
                             || p.Code.StartsWith("inventory.hardware.read"),

        // Purchasing — suppliers, purchase orders, inventory read, reports
        ["Purchasing"]   = p => p.Code.StartsWith("inventory.suppliers")
                             || p.Module == "reports"
                             || p.Code.StartsWith("inventory.products.read"),

        ["SupportAgent"] = p => p.Module is "helpdesk" or "crm" || p.Code == "reports.view",

        // Delivery — read-only on sales orders/customers/products, no finance
        ["Delivery"]     = p => p.Code is "crm.customers.read"
                             || p.Code.StartsWith("inventory.products.read")
                             || p.Code == "reports.view",

        ["ReadOnly"]     = p => p.Code.EndsWith(".read") || p.Code == "reports.view",

        // Marketing — CRM read, reports, quotations read
        ["Marketing"]    = p => p.Code is "crm.customers.read" or "crm.leads.read" or "crm.contacts.read"
                             || p.Code is "quotations.read" or "reports.view",

        // Office — internal staff (e.g. office assistants): read-only + company documents
        ["Office"]       = p => p.Code.EndsWith(".read") || p.Code == "reports.view"
                             || p.Code is "documents.view" or "documents.download",
    };

    // ── Entry point ───────────────────────────────────────────────────────────

    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        // Roles and permissions first — production users depend on them
        await RunStep("SeedPermissions", () => SeedPermissionsAsync(db, logger), logger);
        await RunStep("SeedRoles",       () => SeedRolesAsync(roleManager, db, logger), logger);

        // ── One-time email renames ──────────────────────────────────────────
        await RunStep("RenameModetaz", () => RenameUserEmailAsync(
            userManager, "Moetaz@rtegy.com", "Moataz@rtegy.com", logger), logger);

        // Production users — each is self-contained and retried independently
        foreach (var u in ProductionUsers)
            await RunStep($"EnsureUser:{u.Email}", () => EnsureUserAsync(db, userManager, u, logger), logger);

        // Accounting reference data
        await RunStep("AccountingSeed", () => AccountingSeeder.SeedAsync(db, logger), logger);

        // Seed historical customers from اكواد.xlsx (idempotent — check by Code)
        await RunStep("SeedCustomers", () => SeedCustomersAsync(db, logger), logger);
    }

    /// <summary>
    /// Renames a user's email/username in-place. Idempotent — skips if old email
    /// doesn't exist or new email already taken.
    /// </summary>
    private static async Task RenameUserEmailAsync(
        UserManager<ApplicationUser> userManager,
        string oldEmail, string newEmail, ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(oldEmail);
        if (existing is null) return; // already renamed or never existed

        if (await userManager.FindByEmailAsync(newEmail) is not null)
        {
            logger.LogInformation("[Seed] Email rename skipped — {New} already exists.", newEmail);
            return;
        }

        existing.Email              = newEmail;
        existing.NormalizedEmail    = newEmail.ToUpperInvariant();
        existing.UserName           = newEmail;
        existing.NormalizedUserName = newEmail.ToUpperInvariant();

        var result = await userManager.UpdateAsync(existing);
        if (result.Succeeded)
            logger.LogInformation("[Seed] Renamed {Old} → {New}.", oldEmail, newEmail);
        else
            logger.LogWarning("[Seed] Could not rename {Old}: {Errors}", oldEmail,
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private static async Task RunStep(string name, Func<Task> step, ILogger logger)
    {
        try { await step(); }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Seed] Step '{Step}' failed: {Msg}", name, ex.Message);
        }
    }

    // ── Permissions ───────────────────────────────────────────────────────────

    private static async Task SeedPermissionsAsync(ApplicationDbContext db, ILogger logger)
    {
        string[] codes =
        [
            "crm.customers.read",  "crm.customers.write",  "crm.customers.delete",
            "crm.contacts.read",   "crm.contacts.write",   "crm.contacts.delete",
            "crm.leads.read",      "crm.leads.write",      "crm.leads.delete",   "crm.leads.convert",
            "quotations.read",     "quotations.write",     "quotations.delete",  "quotations.send", "quotations.approve",
            "projects.read",       "projects.write",       "projects.delete",    "projects.manage-members",
            "tasks.read",          "tasks.write",          "tasks.delete",       "tasks.assign",
            "helpdesk.read",       "helpdesk.write",       "helpdesk.delete",    "helpdesk.assign",
            "inventory.products.read",  "inventory.products.write",  "inventory.products.delete",
            "inventory.licenses.read",  "inventory.licenses.write",  "inventory.licenses.delete",
            "inventory.hardware.read",  "inventory.hardware.write",  "inventory.hardware.delete",
            "inventory.suppliers.read", "inventory.suppliers.write", "inventory.suppliers.delete",
            "invoices.read", "invoices.write", "invoices.delete", "invoices.record-payment",
            "reports.view",
            "users.read",  "users.write",  "users.manage-roles",
            "settings.read", "settings.write",
            "documents.view", "documents.download",
        ];

        // Additive: only insert codes that don't already exist in the database.
        // This ensures new permission codes are picked up on existing deployments
        // rather than being skipped by an early-exit guard.
        var existingCodes = await db.Permissions
            .Select(p => p.Code)
            .ToListAsync();

        var newCodes = codes.Where(c => !existingCodes.Contains(c)).ToList();
        if (!newCodes.Any())
        {
            logger.LogInformation("[Seed] All {Count} permissions already seeded.", codes.Length);
            return;
        }

        db.Permissions.AddRange(newCodes.Select(code => new Permission
        {
            Code        = code,
            Module      = code.Split('.')[0],
            Description = $"Permission: {code}",
        }));

        await db.SaveChangesAsync();
        logger.LogInformation("[Seed] Seeded {NewCount} new permission(s) (total defined: {Total}).", newCodes.Count, codes.Length);
    }

    // ── Roles ─────────────────────────────────────────────────────────────────

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext db, ILogger logger)
    {
        // Always ensure every role in the master dictionary exists, even if
        // permissions haven't been seeded yet (they can be linked later).
        foreach (var roleName in RolePermissions.Keys)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var r = await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
                if (r.Succeeded)
                    logger.LogInformation("[Seed] Role '{Role}' created.", roleName);
                else
                    logger.LogWarning("[Seed] Could not create role '{Role}': {Errors}",
                        roleName, string.Join("; ", r.Errors.Select(e => e.Description)));
            }
        }

        // Wire up permissions only when the Permissions table has data
        var allPerms = await db.Permissions.ToListAsync();
        if (!allPerms.Any())
        {
            logger.LogInformation("[Seed] Skipping role→permission assignment — permissions not seeded yet.");
            return;
        }

        foreach (var (roleName, filter) in RolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var assigned = (await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync())
                .ToHashSet();

            var toAdd = allPerms
                .Where(filter)
                .Where(p => !assigned.Contains(p.Id))
                .Select(p => new RolePermission { RoleId = role.Id, PermissionId = p.Id })
                .ToList();

            if (toAdd.Count > 0)
            {
                db.RolePermissions.AddRange(toAdd);
                await db.SaveChangesAsync();
            }
        }

        logger.LogInformation("[Seed] Role→permission assignment complete.");
    }

    // ── Individual user upsert ────────────────────────────────────────────────
    // Completely explicit — every failure is logged with the full error list.
    // Never silently drops a user. Always resets the password so the stored
    // hash matches the constant in ProductionUsers, regardless of what
    // previous seed runs may have written.

    // ── Historical customers from اكواد.xlsx ─────────────────────────────────
    // Phone and Email are deliberately left null — finance/admins will assign.
    // Sales rep assignment is also left null — done manually via the UI.
    // Idempotent: checks by Code; skips any code already in the database.

    private static readonly (string Code, string Name, string? TaxNumber)[] HistoricalCustomers =
    [
        ("1", "لاغي", null),
        ("2", "المقاولون العرب", "100394965"),
        ("3", "شركة مياه الشرب بالقاهرة الكبرى", "212543504"),
        ("4", "ايليت انتجراتيد سليوشن لمستلزمات الكمبيوتر", "662879902"),
        ("5", "شركه القناه لتوزيع الكهرباء", "100473199"),
        ("6", "ديــــــــوان عـــــام مـحافظة كـفر الشيـخ", "545799619"),
        ("7", "هيئة المجتمعات العمرانية", "530703343"),
        ("8", "مركز التعليم المفتوح", "726665703"),
        ("9", "مديرية التنظيم والادارة بالقاهرة", "100719635"),
        ("10", "شركة سى ام ايه سى جى ام لتوكيلات النقل البحرى", "376659300"),
        ("11", "هيئه قناه السويس", "100456774"),
        ("12", "كليه الصيدله جامعه القاهره--وزاره التعليم العالي", "100635423"),
        ("13", "جامعه دمنهور التعليميه", "726358347"),
        ("14", "مجلس مدينة المنزله", "547734433"),
        ("15", "جهاز تنظيم النقل البري الداخلي والدولي", "616506244"),
        ("16", "الشركة الهندسية لخدمات الحاسب الذكية", "724361952"),
        ("17", "المجموعه العالميه لتكنولوجيا المعلومات ايه تي اي", "290808650"),
        ("18", "ايجك بيت خبرة هندسى", "200202308"),
        ("19", "دوترونيك للتجارة", "415889634"),
        ("20", "لاند كونسلت للاستشارات الهندسية", "239321855"),
        ("21", "جامعة بدر", "468891153"),
        ("22", "شركه موت ماكدونالد", "579808114"),
        ("23", "مركز معلومات شبكات مرافق المنوفية", "554959747"),
        ("24", "ايدج برو لنظم المعلومات", "208478639"),
        ("25", "ديوان عام محافظه بورسعيد", "574650962"),
        ("26", "الشركة العامة للمقاولات", null),
        ("27", "راية للشبكات", "205075215"),
        ("28", "بدرالدين للبترول بايتبكو", "200035282"),
        ("29", "شركه اكمي ساعيكو للنظم الهندسيه المتكامله", "379075547"),
        ("30", "بيان", null),
        ("31", "الهيئه العامه لتنفيذ المشروعات الصناعيه والتعدينيه", "455077347"),
        ("32", "الجمعيه المصريه للتامين التعاوني", "100692311"),
        ("33", "مديرية التموين", "100687334"),
        ("34", "معهد التخطيط القومى وزارة التنميه الاقتصاديه", "100742386"),
        ("35", "اكسا آي بى ان", null),
        ("36", "حسين محمد محمد الرشيدى وشركاه جامعة مصر الدولية", "310445736"),
        ("37", "الوحدة المحلية لمجلس مدينة المحمودية", "726292923"),
        ("38", "مأمون السباعي", null),
        ("39", "بيمن معتمد", null),
        ("40", "إسلام محمد محمد عزت السعيد", "352352337"),
        ("41", "ليزر تك", null),
        ("42", "حلول لانظمة الحسابات", null),
        ("43", "توب اليكترونيك سيستم", null),
        ("44", "شركه مياه الشرب والصرف الصحى بسوهاج", "310424763"),
        ("45", "سمارت", null),
        ("46", "الجهاز التنفيذى للمنطقه الحره", "552611964"),
        ("47", "معهد بحوث الالكترونيات", "530834855"),
        ("48", "شركة الصرف الصحي -المرج الجديدة", null),
        ("49", "دايركت للحلول التكنولوجيه", "537824367"),
        ("50", "البنك العقاري المصري العربي", "200008382"),
        ("51", "شركه ابتليكوم", "379293382"),
        ("52", "ثري اس ريدي ميكس للخرسانه الجاهزه ثرى اس ريدى ميكس للخرسانة الجاهزة", "509166288"),
        ("53", "وليد حكم محمد ابوعلى (PC Egypt)", "444509275"),
        ("54", "شركه مجموعه الخان للتكنولوجى", "481729402"),
        ("55", "بنك مصر", "200005316"),
        ("56", "الشركه العامه للبترول", "100358055"),
        ("57", "لوجو برنت", null),
        ("58", "فالكون", "704950057"),
        ("59", "الفا للارضيات المتخصصه", "770655343"),
        ("60", "شركة فاليو تيك", "552287520"),
        ("61", "مركز الدراسات والبحوث المتكامله ك البنات ج عين شمس", "100724507"),
        ("62", "شركة الخدمات الطبية", "588660248"),
        ("63", "شركة ميديوس", null),
        ("64", "سرفيس تك عادل مصطفى ابراهيم وشريكته", "445491167"),
        ("65", "شركه مصرالحجاز", "204950139"),
        ("66", "4 tech", null),
        ("67", "الشركة القابضة للطرق والكباري", "200586025"),
        ("68", "ام او تى للاستثمار والمشروعات - MOT", "591832496"),
        ("69", "يوسف منير", null),
        ("70", "ادفانسيس للمشروعات المتكامله", "367131366"),
        ("71", "شركه الصرف الصحي بالقاهره الكبري", "325377480"),
        ("72", "كلية الزراعة جامعة القاهرة", null),
        ("73", "نجوي تكنولوجيز", "493935355"),
        ("74", "شركه رايه للالكترونيات", "200196375"),
        ("75", "احمد حسن", null),
        ("76", "شركة فريش للاجهزة المنزلية", null),
        ("77", "مطبعة دورما", null),
        ("78", "النصر العامه للمقاولات حسن محمد علام", "100030114"),
        ("79", "مؤسسة حياة كريمة", "684291134"),
        ("80", "الشركة القابضة للصوامع والتخزين", "210176504"),
        ("81", "احمد صقر", null),
        ("82", "شركه قها للصناعات الكيماويه و الحربيه و المدنيه", "100136206"),
        ("83", "اجيتك للاجهزه العلميه المتكامله", "537592520"),
        ("84", "شركه فريش اليكتريك للاجهزه المنزليه", "200301950"),
        ("85", "تى ام للاسلاك الكهربائية", "728941805"),
        ("86", "جلاس اكسبرتس للزجاج", "759808627"),
        ("87", "الشركة الأهلية للأحهزة المنزلية فريش شوب", "735909008"),
        ("88", "مدرسة فيسكون", null),
        ("89", "اوراسكوم للانشاءات", "229988806"),
        ("90", "شركه ريدكون للتعمير", "200133454"),
        ("91", "اخناتون للتجاره والتوزيع", "484380486"),
        ("92", "شركه شرق الدلتا لانتاج الكهرباء", "200208470"),
        ("93", "سوفى باك", "208104143"),
        ("94", "جامعه فاروس الاسكندريه", "257069879"),
        ("95", "نانو للتوريدات العمومية", null),
        ("96", "التراث للتنميه السياحيه", "542606054"),
        ("97", "شركة مصر لصناعة الكباسات", "100334377"),
        ("98", "شركه ايفا فارما للادويه والمستلزمات الطبيه", "204963389"),
        ("99", "ايفا فارما للصناعات الدوائية", "200911899"),
        ("100", "مصر كمبيوتر", null),
        ("101", "المكتب الاستشارى للتخطيط والتنميه العمرانيه", "349703736"),
        ("102", "معهد بحوث ادارة المياه وطرق الرى", "543028437"),
        ("103", "محب سمير وشريكته انتراكت تكنولوجي سوليوشنز", "259463906"),
        ("104", "سيريو بلاست ايجيبت", "463105060"),
        ("105", "سي ار ام لتكنولوجيا المعلومات", "227954955"),
        ("106", "انتركوم انتربرايزيس للحلول التكنولوجيه", "728352052"),
        ("107", "المتحدة الدولية للتجهيزات الطبية اليد", "100396291"),
        ("108", "علاءالدين عادل الازهرى وشركاه كونتشتال جروب", "100058426"),
        ("109", "الشركة المصرية لنقل الكهرباء", "200217720"),
        ("110", "القاهره لتكرير البترول", "100344011"),
        ("111", "ايمان محمد كامل زعطوط", "438319982"),
        ("112", "شركة مصر للبترول", "100264794"),
        ("113", "مديريه الشئون الصحيه بدمياط", "584588739"),
        ("114", "شركه صن ام جي كي", "766411907"),
    ];

    private static async Task SeedCustomersAsync(ApplicationDbContext db, ILogger logger)
    {
        // Load existing customer codes to avoid duplicates
        var existingCodes = await db.BusinessPartners
            .Where(bp => bp.PartnerType == BusinessPartnerType.Customer)
            .Select(bp => bp.Code)
            .ToListAsync();

        var toInsert = HistoricalCustomers
            .Where(c => !existingCodes.Contains(c.Code))
            .ToList();

        if (!toInsert.Any())
        {
            logger.LogInformation("[Seed] All {Count} historical customers already seeded.", HistoricalCustomers.Length);
            return;
        }

        db.BusinessPartners.AddRange(toInsert.Select(c => new BusinessPartner
        {
            Code        = c.Code,
            Name        = c.Name,
            TaxNumber   = c.TaxNumber,
            PartnerType = BusinessPartnerType.Customer,
            IsActive    = true,
            // Phone, Email, Address deliberately null — finance/admins will fill via UI
        }));

        await db.SaveChangesAsync();
        logger.LogInformation(
            "[Seed] Seeded {New} new historical customer(s) (total defined: {Total}).",
            toInsert.Count, HistoricalCustomers.Length);
    }

    private static async Task EnsureUserAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        SeedUser spec,
        ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(spec.Email);

        if (existing is null)
        {
            logger.LogInformation("[Seed] Creating user {Email} (role: {Role})...", spec.Email, spec.Role);

            var employee = new Employee
            {
                FullName   = $"{spec.FirstName} {spec.LastName}".Trim(),
                JobTitle   = spec.JobTitle,
                Department = spec.Department,
                HireDate   = DateOnly.FromDateTime(DateTime.UtcNow),
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName           = spec.Email,
                Email              = spec.Email,
                NormalizedEmail    = spec.Email.ToUpperInvariant(),
                NormalizedUserName = spec.Email.ToUpperInvariant(),
                EmailConfirmed     = true,
                FirstName          = spec.FirstName,
                LastName           = spec.LastName,
                IsActive           = true,
                EmployeeId         = employee.Id,
                SecurityStamp      = Guid.NewGuid().ToString(),
            };

            var created = await userManager.CreateAsync(user, spec.Password);
            if (!created.Succeeded)
            {
                logger.LogError("[Seed] FAILED to create {Email}. Errors: {Errors}",
                    spec.Email, string.Join(" | ", created.Errors.Select(e => $"{e.Code}: {e.Description}")));
                // Remove the orphaned employee row so we can retry cleanly
                db.Employees.Remove(employee);
                await db.SaveChangesAsync();
                return;
            }

            employee.UserId = user.Id;
            await db.SaveChangesAsync();

            var roleAdd = await userManager.AddToRoleAsync(user, spec.Role);
            if (!roleAdd.Succeeded)
                logger.LogWarning("[Seed] Could not assign role '{Role}' to {Email}: {Errors}",
                    spec.Role, spec.Email,
                    string.Join(" | ", roleAdd.Errors.Select(e => e.Description)));

            logger.LogInformation("[Seed] ✓ Created {Email} with role '{Role}'.", spec.Email, spec.Role);
        }
        else
        {
            logger.LogInformation("[Seed] User {Email} exists — verifying role and password...", spec.Email);

            // Ensure active
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                await userManager.UpdateAsync(existing);
            }

            // Ensure correct role
            if (!await userManager.IsInRoleAsync(existing, spec.Role))
            {
                var roleAdd = await userManager.AddToRoleAsync(existing, spec.Role);
                if (!roleAdd.Succeeded)
                    logger.LogWarning("[Seed] Could not assign role '{Role}' to {Email}: {Errors}",
                        spec.Role, spec.Email,
                        string.Join(" | ", roleAdd.Errors.Select(e => e.Description)));
            }

            // Always reset password — ensures hash matches current constant
            // even if a previous run stored it under stricter validation rules
            var token = await userManager.GeneratePasswordResetTokenAsync(existing);
            var reset = await userManager.ResetPasswordAsync(existing, token, spec.Password);
            if (!reset.Succeeded)
                logger.LogWarning("[Seed] Could not reset password for {Email}: {Errors}",
                    spec.Email, string.Join(" | ", reset.Errors.Select(e => $"{e.Code}: {e.Description}")));

            logger.LogInformation("[Seed] ✓ Verified {Email} (role: {Role}).", spec.Email, spec.Role);
        }
    }
}
