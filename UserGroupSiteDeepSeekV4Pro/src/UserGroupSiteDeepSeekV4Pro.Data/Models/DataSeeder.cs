using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace UserGroupSiteDeepSeekV4Pro.Data.Models;

public static class DataSeeder
{
    public const string AdminRoleName = "Admin";
    public const string SpeakerRoleName = "Speaker";
    public const string UserRoleName = "User";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure roles exist
        foreach (var roleName in new[] { AdminRoleName, SpeakerRoleName, UserRoleName })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role { Name = roleName });
            }
        }

        // Ensure database is created and migrated
        await context.Database.MigrateAsync();

        // Optionally create an initial admin user if none exists
        if (!await userManager.Users.AnyAsync())
        {
            var adminUser = new User
            {
                UserName = "admin@usergroup.com",
                Email = "admin@usergroup.com",
                EmailConfirmed = true,
                FirstName = "Site",
                LastName = "Admin",
                MemberSince = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AdminRoleName);
            }
        }
    }
}