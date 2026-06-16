using Microsoft.AspNetCore.Identity;

using UserGroupSiteKimiK27Code.Data.Models;
using UserGroupSiteKimiK27Code.Shared;

namespace UserGroupSiteKimiK27Code.Server.Services;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        foreach (var role in new[] { Roles.Admin, Roles.Speaker })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new Role { Name = role, NormalizedName = role.ToUpperInvariant() });
            }
        }

        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@usergroup.local";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User",
                MemberSince = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
            }
        }
    }
}