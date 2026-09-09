using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        if (await db.Permissions.AnyAsync()) return;

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
        ];

        db.Permissions.AddRange(codes.Select(code => new Permission
        {
            Code        = code,
            Module      = code.Split('.')[0],
            Description = $"Permission: {code}",
        }));

        await db.SaveChangesAsync();
        logger.LogInformation("[Seed] Seeded {Count} permissions.", codes.Length);
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
