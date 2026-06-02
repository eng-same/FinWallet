using FinWallet.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinWallet.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Applying pending database migrations...");
            await context.Database.MigrateAsync();

            logger.LogInformation("Seeding roles...");
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new ApplicationRole(roleName));
                }
            }

            logger.LogInformation("Seeding default Administrator...");
            const string adminEmail = "admin@finwallet.local";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    PhoneNumber = "0910000000",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var createPowerUser = await userManager.CreateAsync(newAdmin, "Admin@123456");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                    logger.LogInformation("Default Administrator seeded successfully.");
                }
                else
                {
                    logger.LogError("Failed to seed administrator. Errors: {Errors}", 
                        string.Join(", ", createPowerUser.Errors.Select(e => e.Description)));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database seeding.");
            throw;
        }
    }
}
