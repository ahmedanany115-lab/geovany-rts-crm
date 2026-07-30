using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    private static readonly string[] Departments = ["Sales", "Delivery", "Support", "Operations", "Engineering"];
    private static readonly string[] FirstNames =
        ["Sara", "Omar", "Layla", "Ahmed", "Mona", "Youssef", "Nour", "Karim", "Hana", "Tarek",
         "Dina", "Amr", "Salma", "Hassan", "Rania", "Mostafa", "Yasmin", "Khaled", "Farida", "Sherif",
         "Aya", "Ziad", "Mariam", "Adel", "Reem"];
    private static readonly string[] LastNames =
        ["Hassan", "Fahmy", "Nasser", "Aziz", "Saleh", "Kamal", "Nabil", "Farouk", "Adel", "Gamal",
         "Rady", "Shawky", "Fouad", "Mounir", "Sabry", "Wahba", "Zaki", "Rashad", "Helmy", "Anwar",
         "Bakr", "Tawfik", "Aref", "Younis", "Mahmoud"];

    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        await SeedPermissionsAsync(db, logger);
        await SeedRolesAsync(roleManager, db, logger);
        await SeedEmployeesAndUsersAsync(db, userManager, logger);
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext db, ILogger logger)
    {
        if (db.Permissions.Any()) return;

        string[] codes =
        [
            "crm.customers.read", "crm.customers.write", "crm.customers.delete",
            "crm.contacts.read", "crm.contacts.write", "crm.contacts.delete",
            "crm.leads.read", "crm.leads.write", "crm.leads.delete", "crm.leads.convert",
            "quotations.read", "quotations.write", "quotations.delete", "quotations.send", "quotations.approve",
            "projects.read", "projects.write", "projects.delete", "projects.manage-members",
            "tasks.read", "tasks.write", "tasks.delete", "tasks.assign",
            "helpdesk.read", "helpdesk.write", "helpdesk.delete", "helpdesk.assign",
            "inventory.products.read", "inventory.products.write", "inventory.products.delete",
            "inventory.licenses.read", "inventory.licenses.write", "inventory.licenses.delete",
            "inventory.hardware.read", "inventory.hardware.write", "inventory.hardware.delete",
            "inventory.suppliers.read", "inventory.suppliers.write", "inventory.suppliers.delete",
            "invoices.read", "invoices.write", "invoices.delete", "invoices.record-payment",
            "reports.view",
            "users.read", "users.write", "users.manage-roles",
            "settings.read", "settings.write"
        ];

        foreach (var code in codes)
        {
            var module = code.Split('.')[0];
            db.Permissions.Add(new Permission
            {
                Code = code,
                Module = module,
                Description = $"Permission: {code}"
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} permissions.", codes.Length);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ApplicationDbContext db, ILogger logger)
    {
        var allPermissions = db.Permissions.ToList();

        var roleDefinitions = new Dictionary<string, Func<Permission, bool>>
        {
            ["Admin"] = _ => true,
            ["Manager"] = p => true, // full module access, refined post-seed via the Roles & Permissions UI
            ["Employee"] = p => p.Code.EndsWith(".read") || p.Code is "tasks.write" or "helpdesk.write" or "quotations.write",
            ["SupportAgent"] = p => p.Module is "helpdesk" or "crm" || p.Code == "reports.view",
            ["ReadOnly"] = p => p.Code.EndsWith(".read") || p.Code == "reports.view",
        };

        foreach (var (roleName, permissionFilter) in roleDefinitions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new ApplicationRole { Name = roleName };
                await roleManager.CreateAsync(role);
            }

            var alreadyAssigned = db.RolePermissions.Where(rp => rp.RoleId == role.Id).Select(rp => rp.PermissionId).ToHashSet();
            foreach (var permission in allPermissions.Where(permissionFilter))
            {
                if (!alreadyAssigned.Contains(permission.Id))
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
                }
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded roles: {Roles}.", string.Join(", ", roleDefinitions.Keys));
    }

    private static async Task SeedEmployeesAndUsersAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger logger)
    {
        if (db.Employees.Any()) return;

        // First seeded user is the demo admin with a known password; the rest get a shared demo password too
        // since this is a sales-demo dataset, not a real tenant — never do this outside a demo seeder.
        const string demoPassword = "Demo@12345!";

        var roleCycle = new[] { "Manager", "Employee", "Employee", "Employee", "SupportAgent" };

        for (var i = 0; i < FirstNames.Length; i++)
        {
            var firstName = FirstNames[i];
            var lastName = LastNames[i];
            var department = Departments[i % Departments.Length];
            var email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@rts-erp.demo";

            var employee = new Employee
            {
                FullName = $"{firstName} {lastName}",
                JobTitle = department switch
                {
                    "Sales" => "Account Executive",
                    "Delivery" => "Project Manager",
                    "Support" => "Support Engineer",
                    "Engineering" => "Software Engineer",
                    _ => "Operations Analyst"
                },
                Department = department,
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-365 * (i % 5 + 1)))
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                EmployeeId = employee.Id
            };

            var createResult = await userManager.CreateAsync(user, i == 0 ? "Admin@12345!" : demoPassword);
            if (!createResult.Succeeded)
            {
                logger.LogWarning("Failed to seed user {Email}: {Errors}", email,
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
                continue;
            }

            var role = i == 0 ? "Admin" : roleCycle[i % roleCycle.Length];
            await userManager.AddToRoleAsync(user, role);

            employee.UserId = user.Id;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} employees/users. Admin login: {Email} / Admin@12345!",
            FirstNames.Length, $"{FirstNames[0].ToLowerInvariant()}.{LastNames[0].ToLowerInvariant()}@rts-erp.demo");
    }
}
