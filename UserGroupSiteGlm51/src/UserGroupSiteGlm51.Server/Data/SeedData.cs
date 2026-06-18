using Microsoft.AspNetCore.Identity;

using UserGroupSiteGlm51.Data.Models;

namespace UserGroupSiteGlm51.Server.Data;

/// <summary>
/// Seeds initial roles and, in Development mode, a default admin user.
/// </summary>
public static class SeedData
{
    private const string AdminRoleName = "Admin";
    private const string SpeakerRoleName = "Speaker";

    /// <summary>
    /// Seeds the Admin and Speaker roles and optionally a default admin user for development.
    /// Call this after Identity setup during application startup.
    /// </summary>
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        bool isDevelopment)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager, isDevelopment);
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        if (!await roleManager.RoleExistsAsync(AdminRoleName))
        {
            await roleManager.CreateAsync(new Role { Name = AdminRoleName });
        }

        if (!await roleManager.RoleExistsAsync(SpeakerRoleName))
        {
            await roleManager.CreateAsync(new Role { Name = SpeakerRoleName });
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<User> userManager, bool isDevelopment)
    {
        if (!isDevelopment)
        {
            return;
        }

        const string adminEmail = "admin@usergroup.local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser is not null)
        {
            return;
        }

        adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User",
            MemberSince = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, AdminRoleName);
        }
    }
}