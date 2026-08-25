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
        // Each step is independently try-caught so one failure doesn't block the rest.
        // The app starts successfully regardless — seed can be retried on next deploy.

        await RunStep("SeedPermissions",    () => SeedPermissionsAsync(db, logger),          logger);
        await RunStep("SeedRoles",          () => SeedRolesAsync(roleManager, db, logger),    logger);
        await RunStep("EnsureAdminUser",    () => EnsureAdminUserAsync(db, userManager, logger), logger);
        await RunStep("AccountingSeed",     () => AccountingSeeder.SeedAsync(db, logger),     logger);
        await RunStep("DemoEmployees",      () => SeedDemoEmployeesAsync(db, userManager, logger), logger);
    }

    private static async Task RunStep(string name, Func<Task> step, ILogger logger)
    {
        try
        {
            await step();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seed step '{Step}' failed — app will continue. Error: {Msg}", name, ex.Message);
        }
    }

    // ── Permissions — single bulk insert ─────────────────────────────────────

    private static async Task SeedPermissionsAsync(ApplicationDbContext db, ILogger logger)
    {
        // One query to check existence — no sequential loop
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

        // AddRange + single SaveChanges = one round-trip
        db.Permissions.AddRange(codes.Select(code => new Permission
        {
            Code        = code,
            Module      = code.Split('.')[0],
            Description = $"Permission: {code}"
        }));

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} permissions.", codes.Length);
    }

    // ── Roles — one round-trip per role ──────────────────────────────────────

    private static async Task SeedRolesAsync(
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext db,
        ILogger logger)
    {
        // Load all permissions in one query
        var allPerms = await db.Permissions.ToListAsync();
        if (!allPerms.Any()) return; // permissions not seeded yet — will retry next restart

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

            // One query: which permissions are already assigned to this role?
            var assigned = await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToHashSetAsync();

            // Bulk-add missing ones
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

        logger.LogInformation("Roles seeded.");
    }

    // ── Admin user — upsert in minimal queries ────────────────────────────────

    private static async Task EnsureAdminUserAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(AdminEmail);

        if (existing is null)
        {
            // Create employee + user in two round-trips total
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
                EmailConfirmed = true,
                FirstName      = AdminFirstName,
                LastName       = AdminLastName,
                IsActive       = true,
                EmployeeId     = employee.Id
            };

            var createResult = await userManager.CreateAsync(user, AdminPassword);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create admin: {Errors}",
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            employee.UserId = user.Id;
            await db.SaveChangesAsync();
            await userManager.AddToRoleAsync(user, "Admin");
            logger.LogInformation("Admin user created: {Email}", AdminEmail);
        }
        else
        {
            // Ensure active + in Admin role — two queries max
            if (!existing.IsActive) { existing.IsActive = true; await userManager.UpdateAsync(existing); }

            if (!await userManager.IsInRoleAsync(existing, "Admin"))
                await userManager.AddToRoleAsync(existing, "Admin");

            // Reset password every deploy so it stays current
            var token = await userManager.GeneratePasswordResetTokenAsync(existing);
            await userManager.ResetPasswordAsync(existing, token, AdminPassword);

            logger.LogInformation("Admin user verified: {Email}", AdminEmail);
        }
    }

    // ── Demo employees — skipped if data exists ───────────────────────────────

    private static async Task SeedDemoEmployeesAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // One query — if more than 1 employee, demo data already seeded
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
                FirstName = fn, LastName = ln, IsActive = true, EmployeeId = emp.Id
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
