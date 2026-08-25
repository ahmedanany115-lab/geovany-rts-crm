using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    // ── Primary admin — always upserted, regardless of other seed data ─────────
    private const string AdminEmail     = "geovany.hany@rtegy.com";
    private const string AdminPassword  = "Geovany@153";
    private const string AdminFirstName = "Geovany";
    private const string AdminLastName  = "Hany";

    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        await SeedPermissionsAsync(db, logger);
        await SeedRolesAsync(roleManager, db, logger);
        await EnsureAdminUserAsync(db, userManager, logger);    // always runs
        await SeedDemoEmployeesAsync(db, userManager, logger);  // skipped if data exists
        await AccountingSeeder.SeedAsync(db, logger);
    }

    // ── Permissions ───────────────────────────────────────────────────────────

    private static async Task SeedPermissionsAsync(ApplicationDbContext db, ILogger logger)
    {
        if (db.Permissions.Any()) return;

        string[] codes =
        [
            "crm.customers.read", "crm.customers.write", "crm.customers.delete",
            "crm.contacts.read",  "crm.contacts.write",  "crm.contacts.delete",
            "crm.leads.read",     "crm.leads.write",     "crm.leads.delete", "crm.leads.convert",
            "quotations.read",    "quotations.write",    "quotations.delete", "quotations.send", "quotations.approve",
            "projects.read",      "projects.write",      "projects.delete",   "projects.manage-members",
            "tasks.read",         "tasks.write",         "tasks.delete",      "tasks.assign",
            "helpdesk.read",      "helpdesk.write",      "helpdesk.delete",   "helpdesk.assign",
            "inventory.products.read",  "inventory.products.write",  "inventory.products.delete",
            "inventory.licenses.read",  "inventory.licenses.write",  "inventory.licenses.delete",
            "inventory.hardware.read",  "inventory.hardware.write",  "inventory.hardware.delete",
            "inventory.suppliers.read", "inventory.suppliers.write", "inventory.suppliers.delete",
            "invoices.read", "invoices.write", "invoices.delete", "invoices.record-payment",
            "reports.view",
            "users.read", "users.write", "users.manage-roles",
            "settings.read", "settings.write"
        ];

        foreach (var code in codes)
        {
            db.Permissions.Add(new Permission
            {
                Code        = code,
                Module      = code.Split('.')[0],
                Description = $"Permission: {code}"
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} permissions.", codes.Length);
    }

    // ── Roles ─────────────────────────────────────────────────────────────────

    private static async Task SeedRolesAsync(
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext db,
        ILogger logger)
    {
        var allPermissions = db.Permissions.ToList();

        var roleDefinitions = new Dictionary<string, Func<Permission, bool>>
        {
            ["Admin"]        = _ => true,
            ["Manager"]      = _ => true,
            ["Employee"]     = p => p.Code.EndsWith(".read") || p.Code is "tasks.write" or "helpdesk.write" or "quotations.write",
            ["SupportAgent"] = p => p.Module is "helpdesk" or "crm" || p.Code == "reports.view",
            ["ReadOnly"]     = p => p.Code.EndsWith(".read") || p.Code == "reports.view",
        };

        foreach (var (roleName, permFilter) in roleDefinitions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new ApplicationRole { Name = roleName };
                await roleManager.CreateAsync(role);
            }

            var assigned = db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToHashSet();

            foreach (var perm in allPermissions.Where(permFilter))
            {
                if (!assigned.Contains(perm.Id))
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded roles.");
    }

    // ── Primary admin user — always upserted ─────────────────────────────────

    private static async Task EnsureAdminUserAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(AdminEmail);

        if (existing is null)
        {
            // Create employee record
            var employee = new Employee
            {
                FullName   = $"{AdminFirstName} {AdminLastName}",
                JobTitle   = "System Administrator",
                Department = "IT",
                HireDate   = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();

            // Create user
            var user = new ApplicationUser
            {
                UserName       = AdminEmail,
                Email          = AdminEmail,
                EmailConfirmed = true,
                FirstName      = AdminFirstName,
                LastName       = AdminLastName,
                IsActive       = true,
                EmployeeId     = employee.Id
            };

            var result = await userManager.CreateAsync(user, AdminPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create admin user {Email}: {Errors}",
                    AdminEmail, string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            employee.UserId = user.Id;
            await db.SaveChangesAsync();

            await userManager.AddToRoleAsync(user, "Admin");
            logger.LogInformation("Created admin user: {Email}", AdminEmail);
        }
        else
        {
            // Ensure password is current and account is active
            existing.IsActive = true;
            await userManager.UpdateAsync(existing);

            var token = await userManager.GeneratePasswordResetTokenAsync(existing);
            var resetResult = await userManager.ResetPasswordAsync(existing, token, AdminPassword);
            if (!resetResult.Succeeded)
                logger.LogWarning("Could not reset admin password: {Errors}",
                    string.Join("; ", resetResult.Errors.Select(e => e.Description)));

            if (!await userManager.IsInRoleAsync(existing, "Admin"))
                await userManager.AddToRoleAsync(existing, "Admin");

            logger.LogInformation("Admin user verified: {Email}", AdminEmail);
        }
    }

    // ── Demo employees (skipped if any employees already exist) ───────────────

    private static async Task SeedDemoEmployeesAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // Skip demo data if employees already seeded (production scenario)
        if (db.Employees.Count() > 1) return;

        string[] firstNames = ["Sara", "Omar", "Layla", "Ahmed", "Mona", "Youssef", "Nour", "Karim"];
        string[] lastNames  = ["Hassan", "Fahmy", "Nasser", "Aziz", "Saleh", "Kamal", "Nabil", "Farouk"];
        string[] departments = ["Sales", "Delivery", "Support", "Operations"];
        string[] roleCycle  = ["Manager", "Employee", "Employee", "SupportAgent"];
        const string demoPassword = "Demo@12345!";

        for (var i = 0; i < firstNames.Length; i++)
        {
            var fn    = firstNames[i];
            var ln    = lastNames[i];
            var email = $"{fn.ToLowerInvariant()}.{ln.ToLowerInvariant()}@rts-erp.demo";

            if (await userManager.FindByEmailAsync(email) is not null) continue;

            var dept = departments[i % departments.Length];
            var emp  = new Employee
            {
                FullName   = $"{fn} {ln}",
                JobTitle   = dept == "Sales" ? "Account Executive" : dept == "Delivery" ? "Project Manager" : "Analyst",
                Department = dept,
                HireDate   = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-365 * (i % 3 + 1)))
            };
            db.Employees.Add(emp);
            await db.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName       = email,
                Email          = email,
                EmailConfirmed = true,
                FirstName      = fn,
                LastName       = ln,
                IsActive       = true,
                EmployeeId     = emp.Id
            };

            var result = await userManager.CreateAsync(user, demoPassword);
            if (!result.Succeeded) continue;

            emp.UserId = user.Id;
            await userManager.AddToRoleAsync(user, roleCycle[i % roleCycle.Length]);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Demo employees seeded.");
    }
}
