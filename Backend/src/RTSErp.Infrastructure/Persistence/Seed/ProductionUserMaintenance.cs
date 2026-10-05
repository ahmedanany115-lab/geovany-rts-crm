using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RTSErp.Domain.Entities.HR;
using RTSErp.Domain.Entities.Identity;

namespace RTSErp.Infrastructure.Persistence.Seed;

public static class ProductionUserMaintenance
{
    public static async Task ApplyAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        await CorrectMoatazAsync(db, userManager, logger);
        await EnsureAhmedSaiedAsync(db, userManager, configuration, logger);
    }

    private static async Task CorrectMoatazAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        var user = await userManager.FindByEmailAsync("Moataz@rtegy.com");
        if (user is null) return;

        var changed = user.FirstName != "Moataz" || user.LastName != "";
        if (changed)
        {
            user.FirstName = "Moataz";
            user.LastName = "";
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                logger.LogWarning("[Seed] Could not correct Moataz display name: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (user.EmployeeId.HasValue)
        {
            var employee = await db.Employees.FindAsync(user.EmployeeId.Value);
            if (employee is not null && employee.FullName != "Moataz")
            {
                employee.FullName = "Moataz";
                await db.SaveChangesAsync();
                changed = true;
            }
        }

        if (changed)
            logger.LogInformation("[Seed] Corrected Moataz display name.");
    }

    private static async Task EnsureAhmedSaiedAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        const string email = "Ahmed.saied@rtegy.com";
        var user = await userManager.FindByEmailAsync(email);
        var password = configuration["SeedUsers:AhmedSaiedPassword"];

        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("[Seed] Ahmed Saied not created: SeedUsers__AhmedSaiedPassword is not configured.");
                return;
            }

            var employee = new Employee
            {
                FullName = "Ahmed Saied",
                JobTitle = "Sales Manager",
                Department = "Sales",
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.ToUpperInvariant(),
                EmailConfirmed = true,
                FirstName = "Ahmed",
                LastName = "Saied",
                IsActive = true,
                EmployeeId = employee.Id,
                SecurityStamp = Guid.NewGuid().ToString(),
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                db.Employees.Remove(employee);
                await db.SaveChangesAsync();
                logger.LogWarning("[Seed] Could not create Ahmed Saied: {Errors}",
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            employee.UserId = user.Id;
            await db.SaveChangesAsync();
        }

        if (!user.IsActive)
        {
            user.IsActive = true;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, "SalesManager"))
        {
            var roleResult = await userManager.AddToRoleAsync(user, "SalesManager");
            if (!roleResult.Succeeded)
                logger.LogWarning("[Seed] Could not assign SalesManager to Ahmed Saied: {Errors}",
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        logger.LogInformation("[Seed] Ahmed Saied Sales Manager account is ready.");
    }
}
