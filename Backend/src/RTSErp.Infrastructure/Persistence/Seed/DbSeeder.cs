using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
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
        // Step 1 — Admin user is the critical path.
        // It seeds the Admin role inline so it never depends on SeedRoles completing first.
        await RunStep("EnsureAdminUser", () => EnsureAdminUserAsync(db, userManager, roleManager, logger), logger);

        // Steps 2-4 are best-effort — failures are logged but don't block login.
        await RunStep("SeedPermissions",  () => SeedPermissionsAsync(db, logger), logger);
        await RunStep("SeedRoles",        () => SeedRolesAsync(roleManager, db, logger), logger);
        await RunStep("AccountingSeed",   () => AccountingSeeder.SeedAsync(db, logger), logger);
        await RunStep("DemoEmployees",    () => SeedDemoEmployeesAsync(db, userManager, logger), logger);
    }

    private static async Task RunStep(string name, Func<Task> step, ILogger logger)
    {
        try { await step(); }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Seed] Step '{Step}' failed: {Msg}", name, ex.Message);
        }
    }

    // ── Admin user — completely self-contained, no external dependencies ───────
    // Seeds the Admin role inline if it doesn't exist. Never depends on
    // SeedPermissions or SeedRoles having run first.

    private static async Task EnsureAdminUserAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        // Ensure Admin role exists (self-contained — no permission dependency)
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var roleResult = await roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
            if (roleResult.Succeeded)
                logger.LogInformation("[Seed] Admin role created.");
            else
                logger.LogWarning("[Seed] Could not create Admin role: {Errors}",
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        var existing = await userManager.FindByEmailAsync(AdminEmail);

        if (existing is null)
        {
            // Create the employee record first
            var employee = new Employee
            {
                FullName   = $"{AdminFirstName} {AdminLastName}",
                JobTitle   = "System Administrator",
                Department = "IT",
                HireDate   = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName       = AdminEmail,
                Email          = AdminEmail,
                NormalizedEmail       = AdminEmail.ToUpperInvariant(),
                NormalizedUserName    = AdminEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                FirstName      = AdminFirstName,
                LastName       = AdminLastName,
                IsActive       = true,
                EmployeeId     = employee.Id,
                SecurityStamp  = Guid.NewGuid().ToString()
            };

            var createResult = await userManager.CreateAsync(user, AdminPassword);
            if (!createResult.Succeeded)
            {
                logger.LogError("[Seed] Failed to create admin user: {Errors}",
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            // Link employee back to user
            employee.UserId = user.Id;
            await db.SaveChangesAsync();

            var roleAdd = await userManager.AddToRoleAsync(user, "Admin");
            if (!roleAdd.Succeeded)
                logger.LogWarning("[Seed] Could not add admin to Admin role: {Errors}",
                    string.Join("; ", roleAdd.Errors.Select(e => e.Description)));

            logger.LogInformation("[Seed] Admin user created: {Email}", AdminEmail);
        }
        else
        {
            // User exists — ensure they're active and have the Admin role
            var changed = false;
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }
            if (changed) await userManager.UpdateAsync(existing);

            if (!await userManager.IsInRoleAsync(existing, "Admin"))
                await userManager.AddToRoleAsync(existing, "Admin");

            // Always reset the password so it stays in sync with the constant above
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(existing);
            var resetResult = await userManager.ResetPasswordAsync(existing, resetToken, AdminPassword);
            if (!resetResult.Succeeded)
                logger.LogWarning("[Seed] Could not reset admin password: {Errors}",
                    string.Join("; ", resetResult.Errors.Select(e => e.Description)));

            logger.LogInformation("[Seed] Admin user verified: {Email}", AdminEmail);
        }
    }

    // ── Permissions — single bulk insert ─────────────────────────────────────

    private static async Task SeedPermissionsAsync(ApplicationDbContext db, ILogger logger)
    {
        if (await db.Permissions.AnyAsync()) return;

        string[] codes =
        [
            "crm.customers.read",  "crm.customers.write",  "crm.customers.delete",
            "crm.contacts.read",   "crm.contacts.write",   "crm.contacts.delete",
            "crm.leads.read",      "crm.leads.write",      "crm.leads.delete",      "crm.leads.convert",
            "quotations.read",     "quotations.write",     "quotations.delete",     "quotations.send",  "quotations.approve",
            "projects.read",       "projects.write",       "projects.delete",       "projects.manage-members",
            "tasks.read",          "tasks.write",          "tasks.delete",          "tasks.assign",
            "helpdesk.read",       "helpdesk.write",       "helpdesk.delete",       "helpdesk.assign",
            "inventory.products.read",  "inventory.products.write",  "inventory.products.delete",
            "inventory.licenses.read",  "inventory.licenses.write",  "inventory.licenses.delete",
            "inventory.hardware.read",  "inventory.hardware.write",  "inventory.hardware.delete",
            "inventory.suppliers.read", "inventory.suppliers.write", "inventory.suppliers.delete",
            "invoices.read", "invoices.write", "invoices.delete", "invoices.record-payment",
            "reports.view",
            "users.read",     "users.write",    "users.manage-roles",
            "settings.read",  "settings.write"
        ];

        db.Permissions.AddRange(codes.Select(code => new Permission
        {
            Code        = code,
            Module      = code.Split('.')[0],
            Description = $"Permission: {code}"
        }));

        await db.SaveChangesAsync();
        logger.LogInformation("[Seed] Seeded {Count} permissions.", codes.Length);
    }

    // ── Roles with permissions ────────────────────────────────────────────────

    private static async Task SeedRolesAsync(
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext db,
        ILogger logger)
    {
        var allPerms = await db.Permissions.ToListAsync();
        if (!allPerms.Any()) return;

        var roleDefinitions = new Dictionary<string, Func<Permission, bool>>
        {
            ["Admin"]        = _ => true,
            ["Manager"]      = _ => true,
            ["Employee"]     = p => p.Code.EndsWith(".read") || p.Code is "tasks.write" or "helpdesk.write" or "quotations.write",
            ["SupportAgent"] = p => p.Module is "helpdesk" or "crm" || p.Code == "reports.view",
            ["ReadOnly"]     = p => p.Code.EndsWith(".read") || p.Code == "reports.view",
        };

        foreach (var (roleName, filter) in roleDefinitions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new ApplicationRole { Name = roleName };
                await roleManager.CreateAsync(role);
            }

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

        logger.LogInformation("[Seed] Roles with permissions seeded.");
    }

    // ── Demo employees — skipped if data exists ───────────────────────────────

    private static async Task SeedDemoEmployeesAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        if (await db.Employees.CountAsync() > 1) return;

        string[] firstNames  = ["Sara", "Omar", "Layla", "Ahmed", "Mona", "Youssef", "Nour", "Karim"];
        string[] lastNames   = ["Hassan", "Fahmy", "Nasser", "Aziz", "Saleh", "Kamal", "Nabil", "Farouk"];
        string[] departments = ["Sales", "Delivery", "Support", "Operations"];
        string[] roleCycle   = ["Manager", "Employee", "Employee", "SupportAgent"];
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
                UserName = email, Email = email, EmailConfirmed = true,
                FirstName = fn, LastName = ln, IsActive = true, EmployeeId = emp.Id,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await userManager.CreateAsync(user, demoPassword);
            if (!result.Succeeded) continue;

            emp.UserId = user.Id;
            await userManager.AddToRoleAsync(user, roleCycle[i % roleCycle.Length]);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("[Seed] Demo employees seeded.");
    }
}
